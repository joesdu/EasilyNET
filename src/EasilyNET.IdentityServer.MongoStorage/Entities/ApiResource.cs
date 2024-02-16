// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

// ReSharper disable ClassNeverInstantiated.Global

namespace EasilyNET.IdentityServer.MongoStorage.Entities;

/// <summary>
/// Api资源
/// </summary>
public class ApiResource
{
    /// <summary>
    /// 启用
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 名称
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
    /// 属性
    /// </summary>
    public IDictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Secrets
    /// </summary>
    public List<ApiSecret> Secrets { get; set; } = [];

    /// <summary>
    /// Scopes
    /// </summary>
    public List<string> Scopes { get; set; } = [];

    /// <summary>
    /// 用户载荷
    /// </summary>
    public List<ApiResourceClaim> UserClaims { get; set; } = [];
}