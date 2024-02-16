// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace EasilyNET.IdentityServer.MongoStorage.Entities;

/// <summary>
/// UserClaim
/// </summary>
public abstract class UserClaim
{
    /// <summary>
    /// Id
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 类型
    /// </summary>
    public string Type { get; set; } = string.Empty;
}