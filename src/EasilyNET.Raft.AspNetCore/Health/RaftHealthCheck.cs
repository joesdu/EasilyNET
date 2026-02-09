using EasilyNET.Raft.AspNetCore.Runtime;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EasilyNET.Raft.AspNetCore.Health;

/// <summary>
///     <para xml:lang="en">Health check for raft runtime</para>
///     <para xml:lang="zh">Raft 运行时健康检查</para>
/// </summary>
public sealed class RaftHealthCheck(IRaftRuntime runtime) : IHealthCheck
{
    /// <inheritdoc />
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var state = runtime.GetState();
        var data = new Dictionary<string, object>
        {
            ["nodeId"] = state.NodeId,
            ["role"] = state.Role.ToString(),
            ["term"] = state.CurrentTerm,
            ["leaderId"] = state.LeaderId ?? string.Empty,
            ["commitIndex"] = state.CommitIndex,
            ["lastApplied"] = state.LastApplied
        };
        return Task.FromResult(HealthCheckResult.Healthy("raft runtime is running", data));
    }
}