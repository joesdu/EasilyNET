using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using EasilyNET.IdentityServer.MongoStorage.Abstraction;
using EasilyNET.IdentityServer.MongoStorage.Mappers;
using Microsoft.Extensions.Logging;

namespace EasilyNET.IdentityServer.MongoStorage.Stores;

/// <inheritdoc />
public class PersistedGrantStore(IPersistedGrantDbContext context, ILogger<PersistedGrantStore> logger) : IPersistedGrantStore
{
    /// <inheritdoc />
    public async Task StoreAsync(PersistedGrant token)
    {
        try
        {
            logger.LogDebug("Try to save or update {persistedGrantKey} in database", token.Key);
            await context.InsertOrUpdate(t => t.Key == token.Key, token.ToEntity());
            logger.LogDebug("{persistedGrantKey} stored in database", token.Key);
        }
        catch (Exception ex)
        {
            logger.LogError(0, ex, "Exception storing persisted grant");
            throw;
        }
    }

    /// <inheritdoc />
    public Task<PersistedGrant?> GetAsync(string key)
    {
        var persistedGrant = context.PersistedGrants.FirstOrDefault(x => x.Key == key);
        var model = persistedGrant?.ToModel();
        logger.LogDebug("{persistedGrantKey} found in database: {persistedGrantKeyFound}", key, model is not null);
        return Task.FromResult(model);
    }

    /// <inheritdoc />
    public Task<IEnumerable<PersistedGrant>> GetAllAsync(PersistedGrantFilter filter)
    {
        Validate(filter);
        var persistedGrants = context.PersistedGrants.Where(x => (string.IsNullOrWhiteSpace(filter.SubjectId) || x.SubjectId == filter.SubjectId) &&
                                                                 (string.IsNullOrWhiteSpace(filter.ClientId) || x.ClientId == filter.ClientId) &&
                                                                 (string.IsNullOrWhiteSpace(filter.Type) || x.Type == filter.Type)).ToList();
        var model = persistedGrants.Select(x => x.ToModel());
        logger.LogDebug("{count} persisted grants found for filter: {filter}", persistedGrants.Count, filter);
        return Task.FromResult(model);
    }

    /// <inheritdoc />
    public Task RemoveAsync(string key)
    {
        logger.LogDebug("removing {persistedGrantKey} persisted grant from database", key);
        context.Remove(x => x.Key == key);
        return Task.FromResult(0);
    }

    /// <inheritdoc />
    public Task RemoveAllAsync(PersistedGrantFilter filter)
    {
        Validate(filter);
        logger.LogDebug("removing persisted grants from database for filter: {filter}", filter);
        context.Remove(x => (string.IsNullOrWhiteSpace(filter.SubjectId) || x.SubjectId == filter.SubjectId) &&
                            (string.IsNullOrWhiteSpace(filter.ClientId) || x.ClientId == filter.ClientId) &&
                            (string.IsNullOrWhiteSpace(filter.Type) || x.Type == filter.Type));
        return Task.FromResult(0);
    }

    private static void Validate(PersistedGrantFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter, nameof(filter));
        if (string.IsNullOrWhiteSpace(filter.ClientId) && string.IsNullOrWhiteSpace(filter.SubjectId) && string.IsNullOrWhiteSpace(filter.Type))
        {
            throw new ArgumentException("No filter values set.", nameof(filter));
        }
    }
}