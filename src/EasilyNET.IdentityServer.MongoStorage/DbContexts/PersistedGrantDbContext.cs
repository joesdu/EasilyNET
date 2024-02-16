using EasilyNET.IdentityServer.MongoStorage.Abstraction;
using EasilyNET.IdentityServer.MongoStorage.Configuration;
using EasilyNET.IdentityServer.MongoStorage.Entities;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System.Linq.Expressions;

namespace EasilyNET.IdentityServer.MongoStorage.DbContexts;

/// <summary>
/// PersistedGrantDbContext
/// </summary>
public class PersistedGrantDbContext : MongoDBContextBase, IPersistedGrantDbContext
{
    private readonly IMongoCollection<PersistedGrant> _persistedGrants;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="settings"></param>
    public PersistedGrantDbContext(IOptions<MongoDBConfiguration> settings) : base(settings)
    {
        _persistedGrants = Database.GetCollection<PersistedGrant>(Constants.TableNames.PersistedGrant);
        CreateIndexes();
    }

    /// <inheritdoc />

    public IQueryable<PersistedGrant> PersistedGrants => _persistedGrants.AsQueryable();

    /// <inheritdoc />
    public Task Remove(Expression<Func<PersistedGrant, bool>> filter) => _persistedGrants.DeleteManyAsync(filter);

    /// <inheritdoc />
    public Task RemoveExpired() => Remove(x => x.Expiration < DateTime.UtcNow);

    /// <inheritdoc />
    public Task InsertOrUpdate(Expression<Func<PersistedGrant, bool>> filter, PersistedGrant entity) => _persistedGrants.ReplaceOneAsync(filter, entity, new ReplaceOptions { IsUpsert = true });

    private void CreateIndexes()
    {
        var indexOptions = new CreateIndexOptions { Background = true };
        var builder = Builders<PersistedGrant>.IndexKeys;
        var keyIndexModel = new CreateIndexModel<PersistedGrant>(builder.Ascending(c => c.Key), indexOptions);
        var subIndexModel = new CreateIndexModel<PersistedGrant>(builder.Ascending(c => c.SubjectId), indexOptions);
        var clientIdSubIndexModel = new CreateIndexModel<PersistedGrant>(builder.Combine(builder.Ascending(c => c.ClientId), builder.Ascending(c => c.SubjectId)), indexOptions);
        var clientIdSubTypeIndexModel = new CreateIndexModel<PersistedGrant>(builder.Combine(builder.Ascending(c => c.ClientId),
            builder.Ascending(c => c.SubjectId),
            builder.Ascending(c => c.Type)), indexOptions);
        _persistedGrants.Indexes.CreateOne(keyIndexModel);
        _persistedGrants.Indexes.CreateOne(subIndexModel);
        _persistedGrants.Indexes.CreateOne(clientIdSubIndexModel);
        _persistedGrants.Indexes.CreateOne(clientIdSubTypeIndexModel);
    }
}