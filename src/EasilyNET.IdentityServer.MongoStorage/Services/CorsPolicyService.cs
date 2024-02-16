using Duende.IdentityServer.Services;
using EasilyNET.IdentityServer.MongoStorage.Abstraction;
using Microsoft.Extensions.Logging;

namespace EasilyNET.IdentityServer.MongoStorage.Services;

/// <inheritdoc />
public class CorsPolicyService(IConfigurationDbContext context, ILogger<CorsPolicyService> logger) : ICorsPolicyService
{
    private readonly IConfigurationDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

    /// <inheritdoc />
    public Task<bool> IsOriginAllowedAsync(string origin)
    {
        // If we use SelectMany directly, we got a NotSupportedException inside MongoDB driver.
        // Details: 
        // System.NotSupportedException: Unable to determine the serialization information for the collection 
        // selector in the tree: aggregate([]).SelectMany(x => x.AllowedCorsOrigins.Select(y => y.Origin))
        var origins = _context.Clients.Select(x => x.AllowedCorsOrigins.Select(y => y.Origin)).ToList();

        // As a workaround, we use SelectMany in memory.
        var distinctOrigins = origins.SelectMany(o => o).Where(_ => true).Distinct();
        var isAllowed = distinctOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase);
        logger.LogDebug("Origin {origin} is allowed: {originAllowed}", origin, isAllowed);
        return Task.FromResult(isAllowed);
    }
}