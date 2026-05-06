using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using EasilyNET.Core.WebSocket;

namespace EasilyNET.Test.Unit.WebSocket;

[TestClass]
public class ManagedWebSocketClientTest
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public async Task ConnectAsync_WhenStateChangedHandlerSynchronouslyDisconnects_ShouldNotDeadlock()
    {
        var connectingHandlerInvoked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var client = new ManagedWebSocketClient(new()
        {
            ServerUri = new("ws://127.0.0.1:65535"),
            AutoReconnect = false,
            ConnectionTimeout = TimeSpan.FromMilliseconds(200)
        });
        client.StateChanged += (_, e) =>
        {
            if (e.CurrentState != WebSocketClientState.Connecting || connectingHandlerInvoked.Task.IsCompleted)
            {
                return;
            }
            connectingHandlerInvoked.TrySetResult();
            client.DisconnectAsync().GetAwaiter().GetResult();
        };
        var connectTask = client.ConnectAsync();
        await connectingHandlerInvoked.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var completedTask = await Task.WhenAny(connectTask, Task.Delay(TimeSpan.FromSeconds(2)));
        Assert.AreSame(connectTask, completedTask, "ConnectAsync deadlocked when StateChanged synchronously called DisconnectAsync.");
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(async () => await connectTask);
        Assert.AreEqual(WebSocketClientState.Disconnected, client.State);
    }

    [TestMethod]
    public async Task ConnectAsync_WhenConfigureWebSocketSynchronouslyDisconnects_ShouldNotDeadlock()
    {
        var configureCallbackInvoked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ManagedWebSocketClient? client = null;
        var options = new WebSocketClientOptions
        {
            ServerUri = new("ws://127.0.0.1:65535"),
            AutoReconnect = false,
            ConnectionTimeout = TimeSpan.FromMilliseconds(200),
            ConfigureWebSocket = _ =>
            {
                configureCallbackInvoked.TrySetResult();
                client!.DisconnectAsync().GetAwaiter().GetResult();
            }
        };
        await using var managedClient = new ManagedWebSocketClient(options);
        client = managedClient;
        var connectTask = client.ConnectAsync();
        await configureCallbackInvoked.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var completedTask = await Task.WhenAny(connectTask, Task.Delay(TimeSpan.FromSeconds(2)));
        Assert.AreSame(connectTask, completedTask, "ConnectAsync deadlocked when ConfigureWebSocket synchronously called DisconnectAsync.");
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(async () => await connectTask);
        Assert.AreEqual(WebSocketClientState.Disconnected, client.State);
    }

    [TestMethod]
    public async Task DisposeAsync_WhenConfigureWebSocketBlocksUnderConnectionLock_ShouldReturnWithinConfiguredBudget()
    {
        var enteredConfigureCallback = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var releaseConfigureCallback = new ManualResetEventSlim(false);
        var options = new WebSocketClientOptions
        {
            ServerUri = new("ws://127.0.0.1:65535"),
            AutoReconnect = false,
            ConnectionTimeout = TimeSpan.FromMilliseconds(200),
            DisposeLockTimeout = TimeSpan.FromMilliseconds(100),
            DisposeLockTimeoutGracePeriod = TimeSpan.FromMilliseconds(200),
            ConfigureWebSocket = _ =>
            {
                enteredConfigureCallback.TrySetResult();
                releaseConfigureCallback.Wait(TimeSpan.FromSeconds(5));
            }
        };
        await using var client = new ManagedWebSocketClient(options);
        var connectTask = Task.Run(async () => await client.ConnectAsync());
        await enteredConfigureCallback.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var stopwatch = Stopwatch.StartNew();
        await client.DisposeAsync();
        stopwatch.Stop();
        Assert.AreEqual(WebSocketClientState.Disposed, client.State);
        Assert.IsTrue(stopwatch.Elapsed < TimeSpan.FromSeconds(2), $"DisposeAsync exceeded expected upper bound. Actual: {stopwatch.Elapsed}.");
        Assert.IsFalse(connectTask.IsCompleted, "ConnectAsync should still be blocked by ConfigureWebSocket until the callback is released.");
        releaseConfigureCallback.Set();
        var completedTask = await Task.WhenAny(connectTask, Task.Delay(TimeSpan.FromSeconds(2)));
        Assert.AreSame(connectTask, completedTask, "ConnectAsync did not complete after the blocking callback was released.");
        await AssertConnectFailedAfterDisposeAsync(connectTask);
    }

    [TestMethod]
    public async Task MessageReceivedAsync_WhenHandlerAwaits_ShouldKeepDataValidUntilHandlerCompletes()
    {
        var (httpListener, wsUri) = CreateServer();
        var payload = new byte[] { 1, 2, 3, 4, 5 };
        var serverTask = RunServerAsync(httpListener, async ws =>
        {
            await ws.SendAsync(payload, WebSocketMessageType.Binary, true, CancellationToken.None).ConfigureAwait(false);
            var ackBuffer = new byte[16];
            var ack = await ws.ReceiveAsync(ackBuffer, CancellationToken.None).ConfigureAwait(false);
            if (ack.MessageType != WebSocketMessageType.Close)
            {
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test completed", CancellationToken.None).ConfigureAwait(false);
            }
        });
        var messageReceived = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var client = new ManagedWebSocketClient(new()
        {
            ServerUri = wsUri,
            AutoReconnect = false,
            HeartbeatResponseMessage = ReadOnlyMemory<byte>.Empty
        });
        client.MessageReceivedAsync += async (_, e) =>
        {
            await Task.Yield();
            var received = e.Data.ToArray();
            await client.SendTextAsync("ack", TestContext.CancellationToken).ConfigureAwait(false);
            messageReceived.TrySetResult(received);
        };
        await client.ConnectAsync(TestContext.CancellationToken);
        var received = await messageReceived.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);
        CollectionAssert.AreEqual(payload, received);
        httpListener.Stop();
        await serverTask.WaitAsync(TimeSpan.FromSeconds(3), TestContext.CancellationToken);
    }

    private static async Task AssertConnectFailedAfterDisposeAsync(Task connectTask)
    {
        try
        {
            await connectTask;
            Assert.Fail("ConnectAsync should not succeed after DisposeAsync interrupted the connection attempt.");
        }
        catch (OperationCanceledException)
        {
            // Expected: the connection attempt observed cancellation after disposal.
        }
        catch (ObjectDisposedException)
        {
            // Also acceptable: the connection attempt resumed after the client had already been disposed.
        }
    }

    private static int FindFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static (HttpListener Listener, Uri WsUri) CreateServer()
    {
        var port = FindFreePort();
        var path = $"ws-{Guid.NewGuid():N}/";
        var httpListener = new HttpListener();
        httpListener.Prefixes.Add($"http://127.0.0.1:{port}/{path}");
        httpListener.Start();
        return (httpListener, new($"ws://127.0.0.1:{port}/{path}"));
    }

    private static async Task RunServerAsync(HttpListener listener, Func<System.Net.WebSockets.WebSocket, Task> serverAction)
    {
        try
        {
            var ctx = await listener.GetContextAsync().ConfigureAwait(false);
            var wsCtx = await ctx.AcceptWebSocketAsync(null).ConfigureAwait(false);
            try
            {
                await serverAction(wsCtx.WebSocket).ConfigureAwait(false);
            }
            catch
            {
                // Ignore: client may disconnect before the server action finishes.
            }
            finally
            {
                wsCtx.WebSocket.Dispose();
            }
        }
        catch
        {
            // Ignore: listener may be stopped before or during accept.
        }
    }
}
