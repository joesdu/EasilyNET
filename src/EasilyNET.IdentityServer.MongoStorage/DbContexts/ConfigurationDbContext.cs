using EasilyNET.IdentityServer.MongoStorage.Abstraction;
using EasilyNET.IdentityServer.MongoStorage.Configuration;
using EasilyNET.IdentityServer.MongoStorage.Entities;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace EasilyNET.IdentityServer.MongoStorage.DbContexts;

/// <summary>
/// ConfigurationDbContext
/// </summary>
public class ConfigurationDbContext : MongoDBContextBase, IConfigurationDbContext
{
    private readonly IMongoCollection<ApiResource> _apiResources;
    private readonly IMongoCollection<ApiScope> _apiScopes;
    private readonly IMongoCollection<Client> _clients;
    private readonly IMongoCollection<IdentityResource> _identityResources;

    /// <inheritdoc />
    public ConfigurationDbContext(IOptions<MongoDBConfiguration> settings) : base(settings)
    {
        _clients = Database.GetCollection<Client>(Constants.TableNames.Client);
        _identityResources = Database.GetCollection<IdentityResource>(Constants.TableNames.IdentityResource);
        _apiResources = Database.GetCollection<ApiResource>(Constants.TableNames.ApiResource);
        _apiScopes = Database.GetCollection<ApiScope>(Constants.TableNames.ApiScope);
        CreateClientsIndexes();
        CreateIdentityResourcesIndexes();
        CreateApiResourcesIndexes();
        CreateApiScopesIndexes();
    }

    /// <inheritdoc />
    public IQueryable<Client> Clients => _clients.AsQueryable();

    /// <inheritdoc />
    public IQueryable<IdentityResource> IdentityResources => _identityResources.AsQueryable();

    /// <inheritdoc />
    public IQueryable<ApiResource> ApiResources => _apiResources.AsQueryable();

    /// <inheritdoc />
    public IQueryable<ApiScope> ApiScopes => _apiScopes.AsQueryable();

    /// <inheritdoc />
    public async Task AddClient(Client entity) => await _clients.InsertOneAsync(entity);

    /// <inheritdoc />
    public async Task AddIdentityResource(IdentityResource entity) => await _identityResources.InsertOneAsync(entity);

    /// <inheritdoc />
    public async Task AddApiResource(ApiResource entity) => await _apiResources.InsertOneAsync(entity);

    /// <inheritdoc />
    public async Task AddApiScope(ApiScope entity) => await _apiScopes.InsertOneAsync(entity);

    private void CreateClientsIndexes()
    {
        CreateIndexOptions indexOptions = new() { Background = true };
        var builder = Builders<Client>.IndexKeys;
        CreateIndexModel<Client> clientIdIndexModel = new(builder.Ascending(c => c.ClientId), indexOptions);
        _clients.Indexes.CreateOne(clientIdIndexModel);
    }

    private void CreateIdentityResourcesIndexes()
    {
        CreateIndexOptions indexOptions = new() { Background = true };
        var builder = Builders<IdentityResource>.IndexKeys;
        CreateIndexModel<IdentityResource> nameIndexModel = new(builder.Ascending(c => c.Name), indexOptions);
        _identityResources.Indexes.CreateOne(nameIndexModel);
    }

    private void CreateApiResourcesIndexes()
    {
        CreateIndexOptions indexOptions = new() { Background = true };
        var builder = Builders<ApiResource>.IndexKeys;
        CreateIndexModel<ApiResource> nameIndexModel = new(builder.Ascending(c => c.Name), indexOptions);
        CreateIndexModel<ApiResource> scopesIndexModel = new(builder.Ascending(c => c.Scopes), indexOptions);
        _apiResources.Indexes.CreateOne(nameIndexModel);
        _apiResources.Indexes.CreateOne(scopesIndexModel);
    }

    private void CreateApiScopesIndexes()
    {
        CreateIndexOptions indexOptions = new() { Background = true };
        var builder = Builders<ApiScope>.IndexKeys;
        CreateIndexModel<ApiScope> nameIndexModel = new(builder.Ascending(c => c.Name), indexOptions);
        _apiScopes.Indexes.CreateOne(nameIndexModel);
    }
}