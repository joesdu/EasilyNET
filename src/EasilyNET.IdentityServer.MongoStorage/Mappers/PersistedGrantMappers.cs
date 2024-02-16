using Duende.IdentityServer.Models;
using MapsterMapper;

namespace EasilyNET.IdentityServer.MongoStorage.Mappers;

/// <summary>
/// </summary>
public static class PersistedGrantMappers
{
    static PersistedGrantMappers()
    {
        Mapper = new(new PersistedGrantMapperProfile());
    }

    private static Mapper Mapper { get; }

    /// <summary>
    /// Maps an entity to a model.
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public static PersistedGrant ToModel(this Entities.PersistedGrant token) => Mapper.Map<PersistedGrant>(token);

    /// <summary>
    /// Maps a model to an entity.
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public static Entities.PersistedGrant ToEntity(this PersistedGrant token) => Mapper.Map<Entities.PersistedGrant>(token);

    /// <summary>
    /// update entity
    /// </summary>
    /// <param name="token"></param>
    /// <param name="target"></param>
    public static void UpdateEntity(this PersistedGrant token, Entities.PersistedGrant target) => Mapper.Map(token, target);
}