using EasilyNET.IdentityServer.MongoStorage.Entities;
using Mapster;

namespace EasilyNET.IdentityServer.MongoStorage.Mappers;

/// <summary>
/// AutoMapper Config for PersistedGrant
/// Between Model and Entity
/// </summary>
public class PersistedGrantMapperProfile : TypeAdapterConfig
{
    /// <summary>
    ///     <see cref="PersistedGrantMapperProfile" />
    /// </summary>
    public PersistedGrantMapperProfile()
    {
        // entity to model
        TypeAdapterConfig<PersistedGrant, Duende.IdentityServer.Models.PersistedGrant>.NewConfig();

        // model to entity
        TypeAdapterConfig<Duende.IdentityServer.Models.PersistedGrant, PersistedGrant>.NewConfig();
    }
}