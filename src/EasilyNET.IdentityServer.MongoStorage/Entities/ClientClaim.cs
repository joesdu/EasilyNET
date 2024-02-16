// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace EasilyNET.IdentityServer.MongoStorage.Entities;

/// <summary>
/// ClientClaim
/// </summary>
public class ClientClaim
{
    /// <summary>
    /// Type
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Value
    /// </summary>
    public string Value { get; set; } = string.Empty;
}