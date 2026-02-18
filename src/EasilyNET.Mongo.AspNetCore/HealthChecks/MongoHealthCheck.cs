using EasilyNET.Core.Misc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace EasilyNET.Mongo.AspNetCore.HealthChecks;

/// <summary>
///     <para xml:lang="en">MongoDB health check implementation</para>
///     <para xml:lang="zh">MongoDB 健康检查实现</para>
/// </summary>
/// <param name="client">
///     <see cref="IMongoClient" />
/// </param>
/// <param name="databaseName">
///     <para xml:lang="en">Optional database name to check. If null, uses the admin database.</para>
///     <para xml:lang="zh">可选的数据库名称。如果为 null，则使用 admin 数据库。</para>
/// </param>
internal sealed class MongoHealthCheck(IMongoClient client, string? databaseName = null) : IHealthCheck
{
    private static readonly BsonDocument PingCommand = new("ping", 1);

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var database = client.GetDatabase(databaseName ?? "admin");
            var result = await database.RunCommandAsync<BsonDocument>(PingCommand, cancellationToken: cancellationToken).ConfigureAwait(false);
            return result.Contains("ok") && result["ok"].AsDouble.AreAlmostEqual(1.0)
                       ? HealthCheckResult.Healthy("MongoDB is responding to ping.")
                       : HealthCheckResult.Unhealthy("MongoDB ping returned unexpected result.");
        }
        catch (Exception ex)
        {
            return new(context.Registration.FailureStatus, "MongoDB health check failed.", ex);
        }
    }
}