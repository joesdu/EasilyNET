using Duende.IdentityServer.Models;
using MapsterMapper;

namespace EasilyNET.IdentityServer.MongoStorage.Mappers;

/// <summary>
/// Extension methods to map to/from entity/model for clients.
/// </summary>
public static class ClientMappers
{
    static ClientMappers()
    {
        Mapper = new(new ClientMapperProfile());
    }

    private static Mapper Mapper { get; }

    /// <summary>
    /// Maps an entity to a model.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <returns></returns>
    public static Client ToModel(this Entities.Client entity) => Mapper.Map<Client>(entity);

    /// <summary>
    /// Maps a model to an entity.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <returns></returns>
    public static Entities.Client ToEntity(this Client model) => Mapper.Map<Entities.Client>(model);
}