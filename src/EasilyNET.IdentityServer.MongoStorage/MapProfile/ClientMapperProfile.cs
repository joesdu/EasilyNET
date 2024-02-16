using EasilyNET.IdentityServer.MongoStorage.Entities;
using Mapster;
using ClientClaim = Duende.IdentityServer.Models.ClientClaim;
using Secret = Duende.IdentityServer.Models.Secret;

namespace EasilyNET.IdentityServer.MongoStorage.Mappers;

/// <summary>
/// Mapster configuration for Client
/// </summary>
public class ClientMapperProfile : TypeAdapterConfig
{
    /// <summary>
    ///     <see>
    ///         <cref>{ClientMapperProfile}</cref>
    ///     </see>
    /// </summary>
    public ClientMapperProfile()
    {
        // entity to model
        ForType<Client, Duende.IdentityServer.Models.Client>()
            .Map(x => x.Properties, opt => opt.Properties.ToDictionary(item => item.Key, item => item.Value))
            .Map(x => x.AllowedGrantTypes, opt => opt.AllowedGrantTypes.Select(x => x.GrantType))
            .Map(x => x.RedirectUris, opt => opt.RedirectUris.Select(x => x.RedirectUri))
            .Map(x => x.PostLogoutRedirectUris, opt => opt.PostLogoutRedirectUris.Select(x => x.PostLogoutRedirectUri))
            .Map(x => x.AllowedScopes, opt => opt.AllowedScopes.Select(x => x.Scope))
            .Map(x => x.ClientSecrets, opt => opt.ClientSecrets.Select(x => x))
            .Map(x => x.Claims, opt => opt.Claims.Select(x => new ClientClaim(x.Type, x.Value)))
            .Map(x => x.IdentityProviderRestrictions, opt => opt.IdentityProviderRestrictions.Select(x => x.Provider))
            .Map(x => x.AllowedCorsOrigins, opt => opt.AllowedCorsOrigins.Select(x => x.Origin));
        TypeAdapterConfig<ClientSecret, Secret>
            .NewConfig()
            .Map(dest => dest.Type, src => src.Type);

        // model to entity
        ForType<Duende.IdentityServer.Models.Client, Client>()
            .Map(x => x.Properties, opt => opt.Properties.ToList().Select(x => new ClientProperty { Key = x.Key, Value = x.Value }))
            .Map(x => x.AllowedGrantTypes, opt => opt.AllowedGrantTypes.Select(x => new ClientGrantType { GrantType = x }))
            .Map(x => x.RedirectUris, opt => opt.RedirectUris.Select(x => new ClientRedirectUri { RedirectUri = x }))
            .Map(x => x.PostLogoutRedirectUris, opt => opt.PostLogoutRedirectUris.Select(x => new ClientPostLogoutRedirectUri { PostLogoutRedirectUri = x }))
            .Map(x => x.AllowedScopes, opt => opt.AllowedScopes.Select(x => new ClientScope { Scope = x }))
            .Map(x => x.Claims, opt => opt.Claims.Select(x => new Entities.ClientClaim { Type = x.Type, Value = x.Value }))
            .Map(x => x.IdentityProviderRestrictions, opt => opt.IdentityProviderRestrictions.Select(x => new ClientIdPRestriction { Provider = x }))
            .Map(x => x.AllowedCorsOrigins, opt => opt.AllowedCorsOrigins.Select(x => new ClientCorsOrigin { Origin = x }));
        TypeAdapterConfig<Secret, ClientSecret>.NewConfig();
    }
}