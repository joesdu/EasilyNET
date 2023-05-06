using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;

namespace EasilyNET.IdentityServer.MongoStorage;

internal class RepositoryClientStore : IClientStore
{
    private readonly IRepository Repository;

    public RepositoryClientStore(IRepository repository)
    {
        Repository = repository;
    }

    public Task<Client?> FindClientByIdAsync(string clientId) => Task.FromResult(Repository.Single<Client>(c => c.ClientId == clientId))!;
}