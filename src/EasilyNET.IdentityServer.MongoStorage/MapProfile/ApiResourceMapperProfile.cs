using EasilyNET.IdentityServer.MongoStorage.Entities;
using Mapster;
using Secret = Duende.IdentityServer.Models.Secret;

namespace EasilyNET.IdentityServer.MongoStorage.MapProfile;

/// <summary>
/// Mapster configuration for API resource
/// Between model and entity
/// </summary>
public class ApiResourceMapperProfile : TypeAdapterConfig
{
    /// <summary>
    ///     <see cref="ApiResourceMapperProfile" />
    /// </summary>
    public ApiResourceMapperProfile()
    {
        TypeAdapterConfig<UserClaim, string>.NewConfig().Map(dest => dest, src => src.Type);

        // entity to model
        ForType<ApiResource, Duende.IdentityServer.Models.ApiResource>()
            .Map(x => x.Properties, opt => opt.Properties.ToDictionary(item => item.Key, item => item.Value))
            .Map(x => x.ApiSecrets, opt => opt.Secrets.Select(x => x))
            .Map(x => x.Scopes, opt => opt.Scopes.Select(x => x))
            .Map(x => x.UserClaims, opts => opts.UserClaims.Select(x => x.Type));
        TypeAdapterConfig<ApiSecret, Secret>.NewConfig();
        ForType<ApiScope, Duende.IdentityServer.Models.ApiScope>().Map(x => x.UserClaims, opt => opt.UserClaims);

        // model to entity
        ForType<Duende.IdentityServer.Models.ApiResource, ApiResource>()
            .Map(x => x.Properties, opt => opt.Properties.ToDictionary(item => item.Key, item => item.Value))
            .Map(x => x.Secrets, opts => opts.ApiSecrets.Select(x => x))
            .Map(x => x.Scopes, opts => opts.Scopes.Select(x => x))
            .Map(x => x.UserClaims, opts => opts.UserClaims.Select(x => new ApiResourceClaim { Type = x }));
        TypeAdapterConfig<Secret, ApiSecret>.NewConfig();
        ForType<Duende.IdentityServer.Models.ApiScope, ApiScope>()
            .Map(x => x.UserClaims, opts => opts.UserClaims.Select(x => new ApiScopeClaim { Type = x }));
    }
}