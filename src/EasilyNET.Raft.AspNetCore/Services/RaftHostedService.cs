using EasilyNET.Raft.AspNetCore.Runtime;
using EasilyNET.Raft.Core.Messages;
using EasilyNET.Raft.Core.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace EasilyNET.Raft.AspNetCore.Services;

/// <summary>
///     <para xml:lang="en">Hosted service for driving raft runtime timers and loop</para>
///     <para xml:lang="zh">驱动 Raft 运行时循环和计时器的后台服务</para>
/// </summary>
public sealed class RaftHostedService(IRaftRuntime runtime, IOptions<RaftOptions> options) : BackgroundService
{
    private readonly Random _random = new();

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await runtime.InitializeAsync(stoppingToken).ConfigureAwait(false);

        var electionTask = Task.Run(() => ElectionLoopAsync(stoppingToken), stoppingToken);
        var heartbeatTask = Task.Run(() => HeartbeatLoopAsync(stoppingToken), stoppingToken);

        await Task.WhenAll(electionTask, heartbeatTask).ConfigureAwait(false);
    }

    private async Task ElectionLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var timeout = _random.Next(options.Value.ElectionTimeoutMinMs, options.Value.ElectionTimeoutMaxMs + 1);
            await Task.Delay(timeout, cancellationToken).ConfigureAwait(false);
            await runtime.HandleAsync(new ElectionTimeoutElapsed
            {
                SourceNodeId = options.Value.NodeId,
                Term = runtime.GetState().CurrentTerm
            }, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task HeartbeatLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(options.Value.HeartbeatIntervalMs, cancellationToken).ConfigureAwait(false);
            await runtime.HandleAsync(new HeartbeatTimeoutElapsed
            {
                SourceNodeId = options.Value.NodeId,
                Term = runtime.GetState().CurrentTerm
            }, cancellationToken).ConfigureAwait(false);
        }
    }
}
