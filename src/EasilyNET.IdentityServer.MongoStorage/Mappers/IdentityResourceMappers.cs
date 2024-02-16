using Duende.IdentityServer.Models;
using MapsterMapper;

namespace EasilyNET.IdentityServer.MongoStorage.Mappers;

/// <summary>
/// Extension methods to map to/from entity/model for IdentityResource.
/// </summary>
public static class IdentityResourceMappers
{
    static IdentityResourceMappers()
    {
        Mapper = new(new IdentityResourceMapperProfile());
    }

    private static Mapper Mapper { get; }

    /// <summary>
    /// Maps an entity to a model.
    /// </summary>
    /// <param name="resource"></param>
    /// <returns></returns>
    public static IdentityResource ToModel(this Entities.IdentityResource resource) => Mapper.Map<IdentityResource>(resource);

    /// <summary>
    /// Maps a model to an entity.
    /// </summary>
    /// <param name="resource"></param>
    /// <returns></returns>
    public static Entities.IdentityResource ToEntity(this IdentityResource resource) => Mapper.Map<Entities.IdentityResource>(resource);
}