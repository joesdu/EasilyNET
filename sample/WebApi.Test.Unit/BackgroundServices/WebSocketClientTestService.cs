using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using EasilyNET.Core.WebSocket;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace WebApi.Test.Unit.BackgroundServices;

/// <summary>
/// WebSocket 客户端集成冒烟测试服务。
/// 连接到本应用内置的 <c>/ws/chat</c> 服务端（ChatHandler，会把文本回显为 "Echo: {text}"，连接时发送 "Welcome to the chat!"），
/// 依次运行一组带断言的场景，覆盖：连接/状态机、欢迎消息、文本回显往返、顺序性、高并发、二进制、大消息多帧、
/// 空消息、未连接发送、主动断开/Closed 事件、手动重连恢复。最后输出通过/失败汇总。
/// </summary>
internal sealed class WebSocketClientTestService(ILogger<WebSocketClientTestService> logger, IServer server, IHostApplicationLifetime appLifetime) : BackgroundService
{
    private readonly ConcurrentQueue<(WebSocketClientState Previous, WebSocketClientState Current)> _stateTransitions = new();
    private readonly ConcurrentQueue<string> _receivedTexts = new();
    private ManagedWebSocketClient? _client;
    private int _receivedBinaryCount;
    private int _errorCount;
    private int _clientInitiatedCloses;
    private int _passed;
    private int _failed;

    private static bool TestMode => true;

    private ManagedWebSocketClient CreateClient(Uri serverUri)
    {
        var options = new WebSocketClientOptions
        {
            ServerUri = serverUri,
            AutoReconnect = true,
            ReconnectDelay = TimeSpan.FromSeconds(1),
            // 协议层保活 + 死连接检测
            KeepAliveInterval = TimeSpan.FromSeconds(5),
            KeepAliveTimeout = TimeSpan.FromSeconds(3)
        };
        var client = new ManagedWebSocketClient(options);
        client.MessageReceived += (_, e) =>
        {
            if (e.MessageType == WebSocketMessageType.Text)
            {
                _receivedTexts.Enqueue(Encoding.UTF8.GetString(e.Data.Span));
            }
            else
            {
                Interlocked.Increment(ref _receivedBinaryCount);
            }
        };
        client.StateChanged += (_, e) =>
        {
            _stateTransitions.Enqueue((e.PreviousState, e.CurrentState));
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Client State: {Previous} -> {Current}", e.PreviousState, e.CurrentState);
            }
        };
        client.Error += (_, e) =>
        {
            Interlocked.Increment(ref _errorCount);
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(e.Exception, "Client Error [{Context}]", e.Context);
            }
        };
        client.Reconnecting += (_, e) =>
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning("Client Reconnecting: Attempt {Attempt}", e.AttemptNumber);
            }
        };
        client.Closed += (_, e) =>
        {
            if (e.InitiatedByClient)
            {
                Interlocked.Increment(ref _clientInitiatedCloses);
            }
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Client Closed: {Description} (InitiatedByClient: {ByClient})", e.CloseDescription, e.InitiatedByClient);
            }
        };
        return client;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!TestMode)
        {
            return;
        }
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, appLifetime.ApplicationStopping);
        var token = cts.Token;
        try
        {
            var serverUri = await ResolveServerUriAsync(token).ConfigureAwait(false);
            if (serverUri is null)
            {
                return;
            }
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Connecting to {Uri}...", serverUri);
            }
            var client = _client = CreateClient(serverUri);
            await RunAllTestsAsync(client, token).ConfigureAwait(false);
            LogSummary();
            // 测试结束后保持连接打开以便观察。连接的存活完全由协议层 Ping/Pong 维持
            // （见 KeepAliveInterval/KeepAliveTimeout），这里不再发送任何应用层"心跳"消息。
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("测试完成，连接保持打开（由协议层保活维持），等待应用关闭...");
            }
            await Task.Delay(Timeout.Infinite, token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // 正常关闭
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "WebSocket test service crashed.");
        }
    }

    private async Task RunAllTestsAsync(ManagedWebSocketClient client, CancellationToken token)
    {
        // 1) 连接建立 + 状态机事件
        await RunTestAsync("连接建立与状态转换", async () =>
        {
            await client.ConnectAsync(token).ConfigureAwait(false);
            Expect(client.State is WebSocketClientState.Connected, "连接后状态应为 Connected");
            var sawConnecting = await WaitUntilAsync(() => _stateTransitions.Any(t => t.Current == WebSocketClientState.Connecting), TimeSpan.FromSeconds(2), token).ConfigureAwait(false);
            var sawConnected = await WaitUntilAsync(() => _stateTransitions.Any(t => t.Current == WebSocketClientState.Connected), TimeSpan.FromSeconds(2), token).ConfigureAwait(false);
            Expect(sawConnecting && sawConnected, "应观察到 Connecting → Connected 的 StateChanged 事件");
        }).ConfigureAwait(false);

        // 2) 服务端欢迎消息
        await RunTestAsync("收到服务端欢迎消息", async () =>
        {
            var ok = await WaitUntilAsync(() => _receivedTexts.Any(t => t.Contains("Welcome", StringComparison.Ordinal)), TimeSpan.FromSeconds(3), token).ConfigureAwait(false);
            Expect(ok, "应收到服务端的 Welcome 消息");
        }).ConfigureAwait(false);

        // 3) 文本回显往返
        await RunTestAsync("文本消息回显往返", async () =>
        {
            var msg = $"RT-{Guid.NewGuid():N}-hello";
            await client.SendTextAsync(msg, token).ConfigureAwait(false);
            var ok = await WaitUntilAsync(() => _receivedTexts.Contains($"Echo: {msg}"), TimeSpan.FromSeconds(3), token).ConfigureAwait(false);
            Expect(ok, $"应收到回显 'Echo: {msg}'");
        }).ConfigureAwait(false);

        // 4) 顺序消息按发送顺序回显
        await RunTestAsync("顺序消息按序回显", async () =>
        {
            const int n = 20;
            var tag = $"SEQ-{Guid.NewGuid():N}";
            var prefix = $"Echo: {tag}-";
            for (var i = 0; i < n; i++)
            {
                await client.SendTextAsync($"{tag}-{i}", token).ConfigureAwait(false);
            }
            var ok = await WaitUntilAsync(() => _receivedTexts.Count(t => t.StartsWith(prefix, StringComparison.Ordinal)) >= n, TimeSpan.FromSeconds(5), token).ConfigureAwait(false);
            Expect(ok, $"应收到 {n} 条回显，实际 {_receivedTexts.Count(t => t.StartsWith(prefix, StringComparison.Ordinal))} 条");
            var indices = _receivedTexts.Where(t => t.StartsWith(prefix, StringComparison.Ordinal))
                                        .Select(t => int.Parse(t.AsSpan(prefix.Length)))
                                        .ToList();
            Expect(indices.SequenceEqual(Enumerable.Range(0, n)), "回显顺序应与发送顺序一致");
        }).ConfigureAwait(false);

        // 5) 高并发发送（无消息丢失）
        await RunTestAsync("高并发发送无丢失", async () =>
        {
            const int n = 50;
            var tag = $"CC-{Guid.NewGuid():N}";
            var prefix = $"Echo: {tag}-";
            await Task.WhenAll(Enumerable.Range(0, n).Select(i => client.SendTextAsync($"{tag}-{i}", token))).ConfigureAwait(false);
            var ok = await WaitUntilAsync(() => _receivedTexts.Count(t => t.StartsWith(prefix, StringComparison.Ordinal)) >= n, TimeSpan.FromSeconds(8), token).ConfigureAwait(false);
            Expect(ok, $"并发发送的 {n} 条都应收到回显，实际 {_receivedTexts.Count(t => t.StartsWith(prefix, StringComparison.Ordinal))} 条");
        }).ConfigureAwait(false);

        // 6) 二进制发送（byte[] 与 ReadOnlyMemory 两个重载）
        await RunTestAsync("二进制消息发送", async () =>
        {
            await client.SendBinaryAsync(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }, token).ConfigureAwait(false);
            await client.SendBinaryAsync(new byte[] { 1, 2, 3, 4, 5 }.AsMemory(), token).ConfigureAwait(false);
            await Task.Delay(200, token).ConfigureAwait(false);
            // 服务端只记录不回显，验证连接保持正常即可
            Expect(client.State is WebSocketClientState.Connected, "二进制发送后连接应保持 Connected");
        }).ConfigureAwait(false);

        // 7) 空文本消息（边界）
        await RunTestAsync("空文本消息收发", async () =>
        {
            await client.SendTextAsync(string.Empty, token).ConfigureAwait(false);
            var ok = await WaitUntilAsync(() => _receivedTexts.Contains("Echo: "), TimeSpan.FromSeconds(3), token).ConfigureAwait(false);
            Expect(ok, "空消息应收到 'Echo: ' 回显");
        }).ConfigureAwait(false);

        // 8) 大消息多帧收发（~200KB，强制走多帧拼装路径）
        await RunTestAsync("大消息多帧收发", async () =>
        {
            var tag = $"BIG-{Guid.NewGuid():N}";
            var big = tag + new string('x', 200 * 1024);
            var expected = $"Echo: {big}";
            await client.SendTextAsync(big, token).ConfigureAwait(false);
            var ok = await WaitUntilAsync(() => _receivedTexts.Any(t => t.Length == expected.Length && string.Equals(t, expected, StringComparison.Ordinal)), TimeSpan.FromSeconds(10), token).ConfigureAwait(false);
            Expect(ok, "应完整收到 ~200KB 大消息的回显");
        }).ConfigureAwait(false);

        // 9) 主动断开：触发 Closed(InitiatedByClient=true) 且断开后发送抛异常
        await RunTestAsync("主动断开与断开后发送", async () =>
        {
            var before = Volatile.Read(ref _clientInitiatedCloses);
            await client.DisconnectAsync(CancellationToken.None).ConfigureAwait(false);
            Expect(client.State is not WebSocketClientState.Connected, "断开后状态不应为 Connected");
            var closed = await WaitUntilAsync(() => Volatile.Read(ref _clientInitiatedCloses) > before, TimeSpan.FromSeconds(2), token).ConfigureAwait(false);
            Expect(closed, "主动断开应触发 InitiatedByClient=true 的 Closed 事件");
            var threw = false;
            try
            {
                await client.SendTextAsync("should-fail", token).ConfigureAwait(false);
            }
            catch (InvalidOperationException)
            {
                threw = true;
            }
            Expect(threw, "未连接时 SendTextAsync 应抛 InvalidOperationException");
        }).ConfigureAwait(false);

        // 10) 手动重连后恢复收发
        await RunTestAsync("手动重连后恢复收发", async () =>
        {
            await client.ConnectAsync(token).ConfigureAwait(false);
            Expect(client.State is WebSocketClientState.Connected, "重连后应为 Connected");
            var tag = $"RC-{Guid.NewGuid():N}";
            await client.SendTextAsync(tag, token).ConfigureAwait(false);
            var ok = await WaitUntilAsync(() => _receivedTexts.Contains($"Echo: {tag}"), TimeSpan.FromSeconds(3), token).ConfigureAwait(false);
            Expect(ok, "重连后应能继续收发");
        }).ConfigureAwait(false);

        // 11) 快速 连接/断开 循环（无死锁、无资源泄漏崩溃）
        await RunTestAsync("快速连接断开循环", async () =>
        {
            for (var i = 0; i < 3; i++)
            {
                await client.DisconnectAsync(CancellationToken.None).ConfigureAwait(false);
                await client.ConnectAsync(token).ConfigureAwait(false);
            }
            Expect(client.State is WebSocketClientState.Connected, "多轮连接/断开后应稳定在 Connected");
            var tag = $"CY-{Guid.NewGuid():N}";
            await client.SendTextAsync(tag, token).ConfigureAwait(false);
            var ok = await WaitUntilAsync(() => _receivedTexts.Contains($"Echo: {tag}"), TimeSpan.FromSeconds(3), token).ConfigureAwait(false);
            Expect(ok, "循环结束后应能正常收发");
        }).ConfigureAwait(false);
    }

    private async Task RunTestAsync(string name, Func<Task> body)
    {
        try
        {
            await body().ConfigureAwait(false);
            Interlocked.Increment(ref _passed);
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("[PASS] {Name}", name);
            }
        }
        catch (OperationCanceledException)
        {
            // 关闭中断：向上传播以结束测试序列
            throw;
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _failed);
            logger.LogError(ex, "[FAIL] {Name}", name);
        }
    }

    private void LogSummary()
    {
        var passed = Volatile.Read(ref _passed);
        var failed = Volatile.Read(ref _failed);
        logger.LogInformation(
            "WebSocket 测试完成: 通过 {Passed}, 失败 {Failed} | 收到二进制 {Binary} 条, Error 事件 {Errors} 次, 主动关闭 {Closes} 次",
            passed, failed, Volatile.Read(ref _receivedBinaryCount), Volatile.Read(ref _errorCount), Volatile.Read(ref _clientInitiatedCloses));
        if (failed > 0)
        {
            logger.LogWarning("存在 {Failed} 个失败用例，请检查上方 [FAIL] 日志。", failed);
        }
    }

    private async Task<Uri?> ResolveServerUriAsync(CancellationToken token)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Waiting for server to start...");
        }
        // 延迟启动，避免服务端还未准备好就连接造成异常
        await Task.Delay(3000, token).ConfigureAwait(false);
        var addresses = server.Features.Get<IServerAddressesFeature>()?.Addresses;
        if (addresses is null || addresses.Count == 0)
        {
            logger.LogError("No server addresses found.");
            return null;
        }
        // 处理通配符地址
        var address = addresses.First().Replace("://*", "://localhost").Replace("://+", "://localhost");
        var uriBuilder = new UriBuilder(address)
        {
            Path = "ws/chat"
        };
        uriBuilder.Scheme = uriBuilder.Scheme == "https" ? "wss" : "ws";
        return uriBuilder.Uri;
    }

    private static void Expect(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"断言失败: {message}");
        }
    }

    /// <summary>
    /// 轮询等待 <paramref name="condition" /> 为 true。返回 true 表示条件满足；返回 false 表示在 <paramref name="timeout" /> 内未满足（超时）。
    /// 外部 <paramref name="token" /> 取消时抛出 <see cref="OperationCanceledException" />。
    /// </summary>
    private static async Task<bool> WaitUntilAsync(Func<bool> condition, TimeSpan timeout, CancellationToken token)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeoutCts.CancelAfter(timeout);
        try
        {
            while (!condition())
            {
                await Task.Delay(50, timeoutCts.Token).ConfigureAwait(false);
            }
            return true;
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            return false; // 仅超时，非外部取消
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("WebSocketClientTestService is stopping...");
        }
        await base.StopAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_client is not null)
            {
                await _client.DisposeAsync().ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error disposing client during stop");
        }
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("WebSocketClientTestService stopped.");
        }
    }
}
