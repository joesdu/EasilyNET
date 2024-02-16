// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace EasilyNET.IdentityServer.MongoStorage.Entities;

/// <summary>
/// ClientRedirectUri
/// </summary>
public class ClientRedirectUri
{
    /// <summary>
    /// RedirectUri
    /// </summary>
    public string RedirectUri { get; set; } = string.Empty;
}