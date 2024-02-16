// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace EasilyNET.IdentityServer.MongoStorage.Options;

/// <summary>
/// Token cleanup options
/// </summary>
public class TokenCleanupOptions
{
    /// <summary>
    /// 间隔
    /// </summary>
    public int Interval { get; set; } = 60;

    /// <summary>
    /// 启用?
    /// </summary>
    public bool Enable { get; set; }
}