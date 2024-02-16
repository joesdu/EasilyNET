// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

// ReSharper disable ClassNeverInstantiated.Global

namespace EasilyNET.IdentityServer.MongoStorage.Entities;

/// <summary>
/// PersistedGrant
/// </summary>
public class PersistedGrant
{
    /// <summary>
    /// Key
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Type
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// SubjectID
    /// </summary>
    public string SubjectId { get; set; } = string.Empty;

    /// <summary>
    /// ClientID
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreationTime { get; set; }

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTime? Expiration { get; set; }

    /// <summary>
    /// 日期
    /// </summary>
    public string Data { get; set; } = string.Empty;
}