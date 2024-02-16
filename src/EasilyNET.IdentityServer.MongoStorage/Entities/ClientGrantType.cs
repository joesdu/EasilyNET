// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace EasilyNET.IdentityServer.MongoStorage.Entities;

/// <summary>
/// ClientGrantType
/// </summary>
public class ClientGrantType
{
    /// <summary>
    /// GrantType
    /// </summary>
    public string GrantType { get; set; } = string.Empty;
}