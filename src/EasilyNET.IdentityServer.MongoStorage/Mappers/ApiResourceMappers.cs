using Duende.IdentityServer.Models;
using EasilyNET.IdentityServer.MongoStorage.MapProfile;
using MapsterMapper;

namespace EasilyNET.IdentityServer.MongoStorage.Mappers;

/// <summary>
/// ApiResourceMappers
/// </summary>
public static class ApiResourceMappers
{
    static ApiResourceMappers()
    {
        Mapper = new(new ApiResourceMapperProfile());
    }

    private static Mapper Mapper { get; }

    /// <summary>
    /// Maps an entity to a model.
    /// </summary>
    /// <param name="resource"></param>
    /// <returns></returns>
    public static ApiResource ToModel(this Entities.ApiResource resource) => Mapper.Map<ApiResource>(resource);

    /// <summary>
    /// Map a model to an entity.
    /// </summary>
    /// <param name="resource"></param>
    /// <returns></returns>
    public static Entities.ApiResource ToEntity(this ApiResource resource) => Mapper.Map<Entities.ApiResource>(resource);
}