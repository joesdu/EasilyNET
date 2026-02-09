using EasilyNET.Raft.AspNetCore.Runtime;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EasilyNET.Raft.AspNetCore.Health;

/// <summary>
///     <para xml:lang="en">Liveness health check for raft runtime loop</para>
///     <para xml:lang="zh">Raft 运行时存活探针</para>
/// </summary>
public sealed class RaftLivenessHealthCheck(IRaftRuntime runtime) : IHealthCheck
{
    /// <inheritdoc />
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        => Task.FromResult(runtime.IsInitialized
            ? HealthCheckResult.Healthy("raft liveness ok")
            : HealthCheckResult.Unhealthy("raft runtime not initialized"));
}
