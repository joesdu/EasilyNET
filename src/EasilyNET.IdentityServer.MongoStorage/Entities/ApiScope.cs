// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

// ReSharper disable ClassNeverInstantiated.Global

namespace EasilyNET.IdentityServer.MongoStorage.Entities;

/// <summary>
/// ApiScope
/// </summary>
public class ApiScope
{
    /// <summary>
    /// Name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 显示名称
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 是否必须
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// Emphasize
    /// </summary>
    public bool Emphasize { get; set; }

    /// <summary>
    /// ShowInDiscoveryDocument
    /// </summary>
    public bool ShowInDiscoveryDocument { get; set; } = true;

    /// <summary>
    /// UserClaims
    /// </summary>
    public List<ApiScopeClaim> UserClaims { get; set; } = [];
}