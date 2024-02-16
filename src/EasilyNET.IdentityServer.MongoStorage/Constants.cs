// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace EasilyNET.IdentityServer.MongoStorage;

/// <summary>
/// 常量
/// </summary>
internal static class Constants
{
    /// <summary>
    /// 表名
    /// </summary>
    internal static class TableNames
    {
        #region Operational

        /// <summary>
        /// PersistedGrant
        /// </summary>
        internal const string PersistedGrant = "PersistedGrants";

        #endregion Operational

        #region Configuration

        /// <summary>
        /// IdentityResource
        /// </summary>
        internal const string IdentityResource = "IdentityResources";

        /// <summary>
        /// IdentityClaim
        /// </summary>
        internal const string IdentityClaim = "IdentityClaims";

        /// <summary>
        /// ApiResource
        /// </summary>
        internal const string ApiResource = "ApiResources";

        /// <summary>
        /// ApiSecret
        /// </summary>
        internal const string ApiSecret = "ApiSecrets";

        /// <summary>
        /// ApiScope
        /// </summary>
        internal const string ApiScope = "ApiScopes";

        /// <summary>
        /// ApiClaim
        /// </summary>
        internal const string ApiClaim = "ApiClaims";

        /// <summary>
        /// ApiScopeClaim
        /// </summary>
        internal const string ApiScopeClaim = "ApiScopeClaims";

        /// <summary>
        /// Client
        /// </summary>
        internal const string Client = "Clients";

        /// <summary>
        /// ClientGrantType
        /// </summary>
        internal const string ClientGrantType = "ClientGrantTypes";

        /// <summary>
        /// ClientRedirectUri
        /// </summary>
        internal const string ClientRedirectUri = "ClientRedirectUris";

        /// <summary>
        /// ClientPostLogoutRedirectUri
        /// </summary>
        internal const string ClientPostLogoutRedirectUri = "ClientPostLogoutRedirectUris";

        /// <summary>
        /// ClientScopes
        /// </summary>
        internal const string ClientScopes = "ClientScopes";

        /// <summary>
        /// ClientSecret
        /// </summary>
        internal const string ClientSecret = "ClientSecrets";

        /// <summary>
        /// ClientClaim
        /// </summary>
        internal const string ClientClaim = "ClientClaims";

        /// <summary>
        /// ClientIdPRestriction
        /// </summary>
        internal const string ClientIdPRestriction = "ClientIdPRestrictions";

        /// <summary>
        /// ClientCorsOrigin
        /// </summary>
        internal const string ClientCorsOrigin = "ClientCorsOrigins";

        #endregion Configuration
    }
}