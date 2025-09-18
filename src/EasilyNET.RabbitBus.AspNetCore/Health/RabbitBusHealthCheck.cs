using EasilyNET.RabbitBus.AspNetCore.Manager;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EasilyNET.RabbitBus.AspNetCore.Health;

/// <summary>
/// 简单的RabbitMQ健康检查：检查连接与通道是否打开
/// </summary>
internal sealed class RabbitBusHealthCheck(PersistentConnection connection) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var channel = await connection.GetChannelAsync(cancellationToken).ConfigureAwait(false);
            return channel is { IsOpen: true }
                       ? HealthCheckResult.Healthy("RabbitMQ channel is open")
                       : HealthCheckResult.Degraded("RabbitMQ channel not open");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("RabbitMQ channel retrieval failed", ex);
        }
    }
}

internal static class RabbitBusHealthCheckExtensions
{
    public static IHealthChecksBuilder AddRabbitBusHealthCheck(this IHealthChecksBuilder builder, string name = "rabbitbus") => builder.AddCheck<RabbitBusHealthCheck>(name);
}