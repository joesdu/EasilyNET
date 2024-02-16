// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Duende.IdentityServer;
using Duende.IdentityServer.Models;

// ReSharper disable ClassNeverInstantiated.Global

namespace EasilyNET.IdentityServer.MongoStorage.Entities;

/// <summary>
/// Client
/// </summary>
public class Client
{
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 客户端ID
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// ProtocolType, 默认为OpenIdConnect
    /// </summary>
    public string ProtocolType { get; set; } = IdentityServerConstants.ProtocolTypes.OpenIdConnect;

    /// <summary>
    /// ClientSecret
    /// </summary>
    // ReSharper disable once CollectionNeverUpdated.Global
    public List<ClientSecret> ClientSecrets { get; set; } = [];

    /// <summary>
    /// 是否必须ClientSecret
    /// </summary>
    public bool RequireClientSecret { get; set; } = true;

    /// <summary>
    /// 客户端名称
    /// </summary>
    public string ClientName { get; set; } = string.Empty;

    /// <summary>
    /// 描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// ClientUri
    /// </summary>
    public string ClientUri { get; set; } = string.Empty;

    /// <summary>
    /// Logo链接
    /// </summary>
    public string LogoUri { get; set; } = string.Empty;

    /// <summary>
    /// 是否必须Consent
    /// </summary>
    public bool RequireConsent { get; set; } = true;

    /// <summary>
    /// 允许记住许可
    /// </summary>
    public bool AllowRememberConsent { get; set; } = true;

    /// <summary>
    /// 是否总是在Token中包含用户声明
    /// </summary>
    public bool AlwaysIncludeUserClaimsInIdToken { get; set; }

    /// <summary>
    /// AllowedGrantTypes
    /// </summary>
    public List<ClientGrantType> AllowedGrantTypes { get; set; } = [];

    /// <summary>
    /// RequirePkce
    /// </summary>
    public bool RequirePkce { get; set; }

    /// <summary>
    /// AllowPlainTextPkce
    /// </summary>
    public bool AllowPlainTextPkce { get; set; }

    /// <summary>
    /// AllowAccessTokensViaBrowser
    /// </summary>
    public bool AllowAccessTokensViaBrowser { get; set; }

    /// <summary>
    /// RedirectUris
    /// </summary>

    public List<ClientRedirectUri> RedirectUris { get; set; } = [];

    /// <summary>
    /// PostLogoutRedirectUris
    /// </summary>
    public List<ClientPostLogoutRedirectUri> PostLogoutRedirectUris { get; set; } = [];

    /// <summary>
    /// FrontChannelLogoutUri
    /// </summary>
    public string FrontChannelLogoutUri { get; set; } = string.Empty;

    /// <summary>
    /// FrontChannelLogoutSessionRequired
    /// </summary>
    public bool FrontChannelLogoutSessionRequired { get; set; } = true;

    /// <summary>
    /// BackChannelLogoutUri
    /// </summary>
    public string BackChannelLogoutUri { get; set; } = string.Empty;

    /// <summary>
    /// BackChannelLogoutSessionRequired
    /// </summary>

    public bool BackChannelLogoutSessionRequired { get; set; } = true;

    /// <summary>
    /// AllowOfflineAccess
    /// </summary>

    public bool AllowOfflineAccess { get; set; }

    /// <summary>
    /// AllowedScopes
    /// </summary>

    public List<ClientScope> AllowedScopes { get; set; } = [];

    /// <summary>
    /// IdentityTokenLifetime
    /// </summary>
    public int IdentityTokenLifetime { get; set; } = 300;

    /// <summary>
    /// AccessTokenLifetime
    /// </summary>
    public int AccessTokenLifetime { get; set; } = 3600;

    /// <summary>
    /// AuthorizationCodeLifetime
    /// </summary>
    public int AuthorizationCodeLifetime { get; set; } = 300;

    /// <summary>
    /// ConsentLifetime
    /// </summary>
    public int? ConsentLifetime { get; set; } = null;

    /// <summary>
    /// AbsoluteRefreshTokenLifetime
    /// </summary>
    public int AbsoluteRefreshTokenLifetime { get; set; } = 2592000;

    /// <summary>
    /// SlidingRefreshTokenLifetime
    /// </summary>
    public int SlidingRefreshTokenLifetime { get; set; } = 1296000;

    /// <summary>
    /// RefreshTokenUsage
    /// </summary>
    public int RefreshTokenUsage { get; set; } = (int)TokenUsage.OneTimeOnly;

    /// <summary>
    /// UpdateAccessTokenClaimsOnRefresh
    /// </summary>
    public bool UpdateAccessTokenClaimsOnRefresh { get; set; }

    /// <summary>
    /// RefreshTokenExpiration
    /// </summary>
    public int RefreshTokenExpiration { get; set; } = (int)TokenExpiration.Absolute;

    /// <summary>
    /// AccessTokenType
    /// </summary>
    public int AccessTokenType { get; set; } = 0; // AccessTokenType.Jwt;

    /// <summary>
    /// EnableLocalLogin
    /// </summary>
    public bool EnableLocalLogin { get; set; } = true;

    /// <summary>
    /// IdentityProviderRestrictions
    /// </summary>
    public List<ClientIdPRestriction> IdentityProviderRestrictions { get; set; } = [];

    /// <summary>
    /// IncludeJwtId
    /// </summary>
    public bool IncludeJwtId { get; set; }

    /// <summary>
    /// Claims
    /// </summary>
    public List<ClientClaim> Claims { get; set; } = [];

    /// <summary>
    /// AlwaysSendClientClaims
    /// </summary>
    public bool AlwaysSendClientClaims { get; set; }

    /// <summary>
    /// ClientClaimsPrefix
    /// </summary>
    public string ClientClaimsPrefix { get; set; } = "client_";

    /// <summary>
    /// PairWiseSubjectSalt
    /// </summary>
    public string PairWiseSubjectSalt { get; set; } = string.Empty;

    /// <summary>
    /// AllowedCorsOrigins
    /// </summary>
    public List<ClientCorsOrigin> AllowedCorsOrigins { get; set; } = [];

    /// <summary>
    /// Properties
    /// </summary>
    public List<ClientProperty> Properties { get; set; } = [];

    /// <summary>
    /// UserSsoLifetime
    /// </summary>
    public int? UserSsoLifetime { get; set; }

    /// <summary>
    /// UserCodeType
    /// </summary>
    public string UserCodeType { get; set; } = string.Empty;

    /// <summary>
    /// DeviceCodeLifetime
    /// </summary>
    public int DeviceCodeLifetime { get; set; }
}