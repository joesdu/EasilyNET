using EasilyNET.AutoDependencyInjection.Contexts;

namespace EasilyNET.AutoDependencyInjection.Abstractions;

/// <summary>
///     <para xml:lang="en">App module interface</para>
///     <para xml:lang="zh">App模块接口</para>
/// </summary>
public interface IAppModule : IApplicationInitialization
{
    /// <summary>
    ///     <para xml:lang="en">Configure services</para>
    ///     <para xml:lang="zh">配置服务</para>
    /// </summary>
    /// <param name="context">
    ///     <para xml:lang="en">Configuration services context</para>
    ///     <para xml:lang="zh">配置服务上下文</para>
    /// </param>
    Task ConfigureServices(ConfigureServicesContext context);

    /// <summary>
    ///     <para xml:lang="en">Service dependency collection</para>
    ///     <para xml:lang="zh">服务依赖集合</para>
    /// </summary>
    /// <param name="moduleType">
    ///     <para xml:lang="en">Module type</para>
    ///     <para xml:lang="zh">模块类型</para>
    /// </param>
    IEnumerable<Type> GetDependedTypes(Type? moduleType = null);

    /// <summary>
    ///     <para xml:lang="en">Get whether to enable, obtained from the configuration</para>
    ///     <para xml:lang="zh">获取是否启用,从配置中获取</para>
    /// </summary>
    /// <param name="context">
    ///     <para xml:lang="en">Configuration services context</para>
    ///     <para xml:lang="zh">配置服务上下文</para>
    /// </param>
    bool GetEnable(ConfigureServicesContext context);
}