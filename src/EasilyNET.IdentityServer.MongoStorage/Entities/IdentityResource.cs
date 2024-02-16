// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

// ReSharper disable ClassNeverInstantiated.Global

namespace EasilyNET.IdentityServer.MongoStorage.Entities;

/// <summary>
/// IdentityResource
/// </summary>
public class IdentityResource
{
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 名字
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
    /// 是否显示在发现文档中
    /// </summary>
    public bool ShowInDiscoveryDocument { get; set; } = true;

    /// <summary>
    /// UserClaims
    /// </summary>
    public List<IdentityClaim> UserClaims { get; set; } = [];

    /// <summary>
    /// 属性
    /// </summary>
    public IDictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
}