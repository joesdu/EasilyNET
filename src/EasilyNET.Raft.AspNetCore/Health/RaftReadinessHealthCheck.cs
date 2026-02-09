using EasilyNET.Raft.AspNetCore.Runtime;
using EasilyNET.Raft.Core.Models;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EasilyNET.Raft.AspNetCore.Health;

/// <summary>
///     <para xml:lang="en">Readiness health check for raft serviceability</para>
///     <para xml:lang="zh">Raft 就绪探针</para>
/// </summary>
public sealed class RaftReadinessHealthCheck(IRaftRuntime runtime) : IHealthCheck
{
    /// <inheritdoc />
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (!runtime.IsInitialized)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("raft runtime not initialized"));
        }
        var state = runtime.GetState();
        var ready = state.Role == RaftRole.Leader || !string.IsNullOrWhiteSpace(state.LeaderId);
        return Task.FromResult(ready
                                   ? HealthCheckResult.Healthy("raft readiness ok")
                                   : HealthCheckResult.Degraded("raft has no known leader"));
    }
}