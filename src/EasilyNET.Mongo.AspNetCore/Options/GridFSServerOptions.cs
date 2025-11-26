using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

// ReSharper disable CollectionNeverUpdated.Global

namespace EasilyNET.Mongo.AspNetCore.Options;

/// <summary>
///     <para xml:lang="en">GridFS controller configuration options</para>
///     <para xml:lang="zh">GridFS控制器配置选项</para>
/// </summary>
public class GridFSServerOptions
{
    /// <summary>
    ///     <para xml:lang="en">Authorization data to apply to the controller</para>
    ///     <para xml:lang="zh">应用到控制器的授权数据</para>
    /// </summary>
    public List<IAuthorizeData> AuthorizeData { get; } = [];

    /// <summary>
    ///     <para xml:lang="en">Filters to apply to the controller</para>
    ///     <para xml:lang="zh">应用到控制器的过滤器</para>
    /// </summary>
    public List<IFilterMetadata> Filters { get; } = [];

    /// <summary>
    ///     <para xml:lang="en">Whether to enable the controller, default: <see langword="true" /></para>
    ///     <para xml:lang="zh">是否启用控制器,默认: <see langword="true" /></para>
    /// </summary>
    public bool EnableController { get; set; } = true;
}