using EasilyNET.IdentityServer.MongoStorage.Entities;
using Mapster;

namespace EasilyNET.IdentityServer.MongoStorage.Mappers;

/// <summary>
/// Mapster configuration for API Scope
/// </summary>
public class ApiScopeMapperProfile : TypeAdapterConfig
{
    /// <summary>
    ///     <see cref="ApiScopeMapperProfile" />
    /// </summary>
    public ApiScopeMapperProfile()
    {
        TypeAdapterConfig<UserClaim, string>.NewConfig().Map(dest => dest, src => src.Type);

        // entity to model
        ForType<ApiScope, Duende.IdentityServer.Models.ApiScope>().Map(x => x.UserClaims, opt => opt.UserClaims);

        // model to entity
        ForType<Duende.IdentityServer.Models.ApiScope, ApiScope>().Map(x => x.UserClaims, opts => opts.UserClaims.Select(x => new ApiScopeClaim { Type = x }));
    }
}