using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using EasilyNET.IdentityServer.MongoStorage.Abstraction;
using EasilyNET.IdentityServer.MongoStorage.Mappers;
using Microsoft.Extensions.Logging;

namespace EasilyNET.IdentityServer.MongoStorage.Stores;

/// <inheritdoc />
public class ClientStore(IConfigurationDbContext context, ILogger<ClientStore> logger) : IClientStore
{
    private readonly IConfigurationDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

    /// <inheritdoc />
    public Task<Client?> FindClientByIdAsync(string clientId)
    {
        var client = _context.Clients.FirstOrDefault(x => x.ClientId == clientId);
        var model = client?.ToModel();
        logger.LogDebug("{clientId} found in database: {clientIdFound}", clientId, model is not null);
        return Task.FromResult(model);
    }
}