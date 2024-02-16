// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Duende.IdentityServer;

namespace EasilyNET.IdentityServer.MongoStorage.Entities;

/// <summary>
/// Secret
/// </summary>
public abstract class Secret
{
    /// <summary>
    /// 描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Value
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTime? Expiration { get; set; }

    /// <summary>
    /// 类型
    /// </summary>
    public string Type { get; set; } = IdentityServerConstants.SecretTypes.SharedSecret;
}