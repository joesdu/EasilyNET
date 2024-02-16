using EasilyNET.IdentityServer.MongoStorage.Abstraction;
using Microsoft.Extensions.Logging;

// ReSharper disable ClassWithVirtualMembersNeverInherited.Global

namespace EasilyNET.IdentityServer.MongoStorage;

/// <summary>
/// TokenCleanup
/// </summary>
public class TokenCleanup(IPersistedGrantDbContext persistedGrantDbContext, ILogger<TokenCleanup> logger)
{
    private readonly ILogger<TokenCleanup> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Method to clear expired persisted grants.
    /// </summary>
    /// <returns></returns>
    public async Task RemoveExpiredGrantsAsync()
    {
        try
        {
            _logger.LogTrace("Querying for expired grants to remove");
            await RemoveGrantsAsync();
            // TODO: await RemoveDeviceCodesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("Exception removing expired grants: {exception}", ex.Message);
        }
    }

    /// <summary>
    /// Removes the stale persisted grants.
    /// </summary>
    /// <returns></returns>
    protected virtual async Task RemoveGrantsAsync()
    {
        var expired = persistedGrantDbContext.PersistedGrants.Where(x => x.Expiration < DateTime.UtcNow).ToArray();
        var found = expired.Length;
        _logger.LogDebug("Removing {grantCount} grants", found);
        if (found > 0)
        {
            await persistedGrantDbContext.RemoveExpired();
        }
    }
}