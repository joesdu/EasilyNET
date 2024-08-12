// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// ReSharper disable UnusedMember.Global

namespace EasilyNET.Core.System;

/// <summary>
/// 一个空结构体
/// </summary>
/// <remarks>
/// 当泛型类型需要一个类型参数但完全不使用时,这可以节省 4 个字节,相对于 System.Object
/// </remarks>
internal readonly struct EmptyStruct
{
    /// <summary>
    /// 获取空结构体的一个实例
    /// </summary>
    internal static EmptyStruct Instance => default;
}