using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using EasilyNET.Core.WebSocket;

namespace EasilyNET.Test.Unit.WebSocket;

[TestClass]
public class ManagedWebSocketClientMaxMessageSizeTest
{
    /// <summary>
    /// 找一个本地可用的 TCP 端口（TOCTOU 竞争在本地测试中概率极低，可接受）。
    /// </summary>
    private static int FindFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    /// <summary>
    /// 创建一个绑定到随机端口的 HttpListener，返回其实例及对应的 ws:// URI。
    /// </summary>
    private static (HttpListener Listener, Uri WsUri) CreateServer()
    {
        var port = FindFreePort();
        var httpListener = new HttpListener();
        httpListener.Prefixes.Add($"http://127.0.0.1:{port}/ws/");
        httpListener.Start();
        return (httpListener, new($"ws://127.0.0.1:{port}/ws/"));
    }

    /// <summary>
    /// 接受一个 WebSocket 连接并执行 <paramref name="serverAction" />，之后释放资源。
    /// 服务端产生的任何异常（如客户端已断开）均被静默忽略，以免干扰断言。
    /// </summary>
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
                // Ignore: client may have disconnected due to size limit enforcement
            }
            finally
            {
                wsCtx.WebSocket.Dispose();
            }
        }
        catch
        {
            // Ignore: listener was stopped before or during accept
        }
    }

    /// <summary>
    /// 单帧消息超出 MaxMessageSize 时，应触发 Error 事件并包含 "exceeded maximum allowed size"。
    /// </summary>
    [TestMethod]
    public async Task ReceiveLoop_SingleFrameExceedsMaxMessageSize_ShouldFireError()
    {
        var (httpListener, wsUri) = CreateServer();
        var serverTask = RunServerAsync(httpListener, async ws =>
        {
            // 5 字节单帧，客户端限制为 4
            await ws.SendAsync(new byte[5], WebSocketMessageType.Binary, true, CancellationToken.None).ConfigureAwait(false);
            await Task.Delay(500).ConfigureAwait(false);
        });
        var errorRaised = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var client = new ManagedWebSocketClient(new()
        {
            ServerUri = wsUri,
            AutoReconnect = false,
            MaxMessageSize = 4,
            ReceiveBufferSize = 64
        });
        client.Error += (_, e) => errorRaised.TrySetResult(e.Exception);
        await client.ConnectAsync();
        var ex = await errorRaised.Task.WaitAsync(TimeSpan.FromSeconds(5));
        AssertMaxMessageSizeExceeded(ex);
        httpListener.Stop();
        await serverTask.WaitAsync(TimeSpan.FromSeconds(3));
    }

    /// <summary>
    /// 多帧消息的第一帧超出 MaxMessageSize 时，应立即触发 Error 事件。
    /// </summary>
    [TestMethod]
    public async Task ReceiveLoop_MultiFrameFirstFragmentExceedsMaxMessageSize_ShouldFireError()
    {
        var (httpListener, wsUri) = CreateServer();
        var serverTask = RunServerAsync(httpListener, async ws =>
        {
            // 首帧 5 字节（endOfMessage=false）超出限制 4
            await ws.SendAsync(new byte[5], WebSocketMessageType.Binary, false, CancellationToken.None).ConfigureAwait(false);
            await Task.Delay(500).ConfigureAwait(false);
        });
        var errorRaised = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var client = new ManagedWebSocketClient(new()
        {
            ServerUri = wsUri,
            AutoReconnect = false,
            MaxMessageSize = 4,
            ReceiveBufferSize = 64
        });
        client.Error += (_, e) => errorRaised.TrySetResult(e.Exception);
        await client.ConnectAsync();
        var ex = await errorRaised.Task.WaitAsync(TimeSpan.FromSeconds(5));
        AssertMaxMessageSizeExceeded(ex);
        httpListener.Stop();
        await serverTask.WaitAsync(TimeSpan.FromSeconds(3));
    }

    /// <summary>
    /// 多帧消息各帧单独不超限，但累积后超出 MaxMessageSize 时，应触发 Error 事件。
    /// </summary>
    [TestMethod]
    public async Task ReceiveLoop_MultiFrameAccumulatedExceedsMaxMessageSize_ShouldFireError()
    {
        var (httpListener, wsUri) = CreateServer();
        var serverTask = RunServerAsync(httpListener, async ws =>
        {
            // 两帧各 3 字节：单帧均不超限（3 ≤ 4），但累积 6 > 4
            await ws.SendAsync(new byte[3], WebSocketMessageType.Binary, false, CancellationToken.None).ConfigureAwait(false);
            await ws.SendAsync(new byte[3], WebSocketMessageType.Binary, true, CancellationToken.None).ConfigureAwait(false);
            await Task.Delay(500).ConfigureAwait(false);
        });
        var errorRaised = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var client = new ManagedWebSocketClient(new()
        {
            ServerUri = wsUri,
            AutoReconnect = false,
            MaxMessageSize = 4,
            ReceiveBufferSize = 64
        });
        client.Error += (_, e) => errorRaised.TrySetResult(e.Exception);
        await client.ConnectAsync();
        var ex = await errorRaised.Task.WaitAsync(TimeSpan.FromSeconds(5));
        AssertMaxMessageSizeExceeded(ex);
        httpListener.Stop();
        await serverTask.WaitAsync(TimeSpan.FromSeconds(3));
    }

    /// <summary>
    /// 消息大小恰好等于 MaxMessageSize 时，应正常投递 MessageReceived 事件，不触发错误。
    /// </summary>
    [TestMethod]
    public async Task ReceiveLoop_SingleFrameAtExactMaxMessageSize_ShouldDeliverNormally()
    {
        var (httpListener, wsUri) = CreateServer();
        var payload = new byte[] { 1, 2, 3, 4 };
        var serverTask = RunServerAsync(httpListener, async ws =>
        {
            await ws.SendAsync(payload, WebSocketMessageType.Binary, true, CancellationToken.None).ConfigureAwait(false);
            await Task.Delay(500).ConfigureAwait(false);
        });
        var messageReceived = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        var errorRaised = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var client = new ManagedWebSocketClient(new()
        {
            ServerUri = wsUri,
            AutoReconnect = false,
            MaxMessageSize = 4,
            ReceiveBufferSize = 64,
            // 禁用心跳响应过滤，防止 { 1,2,3,4 } 意外匹配默认 pong 配置
            HeartbeatResponseMessage = ReadOnlyMemory<byte>.Empty
        });
        client.MessageReceived += (_, e) => messageReceived.TrySetResult(e.Data.ToArray());
        client.Error += (_, e) => errorRaised.TrySetResult(e.Exception);
        await client.ConnectAsync();
        var completedTask = await Task.WhenAny(messageReceived.Task, errorRaised.Task, Task.Delay(TimeSpan.FromSeconds(5)));
        Assert.AreSame(messageReceived.Task, completedTask, "Expected MessageReceived event, but got Error or timeout.");
        CollectionAssert.AreEqual(payload, await messageReceived.Task);
        httpListener.Stop();
        await serverTask.WaitAsync(TimeSpan.FromSeconds(3));
    }

    /// <summary>
    /// MaxMessageSize ≤ 0（禁用限制）时，超大消息应正常投递，不触发错误。
    /// ReceiveBufferSize 设为 64 以强制走多次 ReceiveAsync 拼装路径，覆盖多帧路径中的跳过逻辑。
    /// </summary>
    [TestMethod]
    public async Task ReceiveLoop_MaxMessageSizeDisabled_LargeMessageShouldDeliverNormally()
    {
        var (httpListener, wsUri) = CreateServer();
        var payload = new byte[1024];
        new Random(42).NextBytes(payload);
        var serverTask = RunServerAsync(httpListener, async ws =>
        {
            await ws.SendAsync(payload, WebSocketMessageType.Binary, true, CancellationToken.None).ConfigureAwait(false);
            await Task.Delay(500).ConfigureAwait(false);
        });
        var messageReceived = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        var errorRaised = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var client = new ManagedWebSocketClient(new()
        {
            ServerUri = wsUri,
            AutoReconnect = false,
            MaxMessageSize = 0, // 禁用大小限制
            ReceiveBufferSize = 64,
            HeartbeatResponseMessage = ReadOnlyMemory<byte>.Empty
        });
        client.MessageReceived += (_, e) => messageReceived.TrySetResult(e.Data.ToArray());
        client.Error += (_, e) => errorRaised.TrySetResult(e.Exception);
        await client.ConnectAsync();
        var completedTask = await Task.WhenAny(messageReceived.Task, errorRaised.Task, Task.Delay(TimeSpan.FromSeconds(5)));
        Assert.AreSame(messageReceived.Task, completedTask, "Expected MessageReceived event, but got Error or timeout.");
        CollectionAssert.AreEqual(payload, await messageReceived.Task);
        httpListener.Stop();
        await serverTask.WaitAsync(TimeSpan.FromSeconds(3));
    }

    private static void AssertMaxMessageSizeExceeded(Exception ex)
    {
        Assert.IsInstanceOfType<InvalidOperationException>(ex);
        Assert.Contains("exceeded", ex.Message);
        Assert.Contains("MaxMessageSize", ex.Message);
    }
}