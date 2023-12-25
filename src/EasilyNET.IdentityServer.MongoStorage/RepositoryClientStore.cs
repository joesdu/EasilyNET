using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;

namespace EasilyNET.IdentityServer.MongoStorage;

internal sealed class RepositoryClientStore(IRepository repository) : IClientStore
{
    public Task<Client?> FindClientByIdAsync(string clientId) => Task.FromResult(repository.Single<Client>(c => c.ClientId == clientId))!;
}