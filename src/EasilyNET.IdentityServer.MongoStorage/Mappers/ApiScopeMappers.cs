using Duende.IdentityServer.Models;
using MapsterMapper;

namespace EasilyNET.IdentityServer.MongoStorage.Mappers;

/// <summary>
/// </summary>
public static class ApiScopeMappers
{
    static ApiScopeMappers()
    {
        Mapper = new(new ApiScopeMapperProfile());
    }

    private static Mapper Mapper { get; }

    /// <summary>
    /// an entity to a model
    /// </summary>
    /// <param name="scope"></param>
    /// <returns></returns>
    public static ApiScope ToModel(this Entities.ApiScope scope) => Mapper.Map<ApiScope>(scope);

    /// <summary>
    /// a model to an entity
    /// </summary>
    /// <param name="scope"></param>
    /// <returns></returns>
    public static Entities.ApiScope ToEntity(this ApiScope scope) => Mapper.Map<Entities.ApiScope>(scope);
}