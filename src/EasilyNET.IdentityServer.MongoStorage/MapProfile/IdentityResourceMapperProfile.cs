using EasilyNET.IdentityServer.MongoStorage.Entities;
using Mapster;

namespace EasilyNET.IdentityServer.MongoStorage.Mappers;

/// <summary>
/// Mapster configuration for identity resource
/// Between model and entity
/// </summary>
public class IdentityResourceMapperProfile : TypeAdapterConfig
{
    /// <summary>
    ///     <see cref="IdentityResourceMapperProfile" />
    /// </summary>
    public IdentityResourceMapperProfile()
    {
        TypeAdapterConfig<UserClaim, string>.NewConfig().Map(dest => dest, src => src.Type);

        // entity to model
        ForType<IdentityResource, Duende.IdentityServer.Models.IdentityResource>()
            .Map(x => x.Properties, opt => opt.Properties.ToDictionary(item => item.Key, item => item.Value))
            .Map(x => x.UserClaims, opt => opt.UserClaims);

        // model to entity
        ForType<Duende.IdentityServer.Models.IdentityResource, IdentityResource>()
            .Map(x => x.Properties, opt => opt.Properties.ToDictionary(item => item.Key, item => item.Value))
            .Map(x => x.UserClaims, opts => opts.UserClaims.Select(x => new IdentityClaim { Type = x }));
    }
}