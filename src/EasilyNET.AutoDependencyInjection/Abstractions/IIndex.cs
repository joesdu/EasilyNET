namespace EasilyNET.AutoDependencyInjection.Abstractions;

/// <summary>
///     <para xml:lang="en">
///     Provides dictionary-style lookup for keyed services, similar to Autofac's <c>IIndex&lt;TKey, TService&gt;</c>.
///     Allows resolving a specific keyed service by its key at runtime.
///     </para>
///     <para xml:lang="zh">
///     提供按键查找服务的字典式接口，类似于 Autofac 的 <c>IIndex&lt;TKey, TService&gt;</c>。
///     允许在运行时通过键解析特定的 keyed 服务。
///     </para>
/// </summary>
/// <typeparam name="TKey">
///     <para xml:lang="en">The type of the service key</para>
///     <para xml:lang="zh">服务键的类型</para>
/// </typeparam>
/// <typeparam name="TService">
///     <para xml:lang="en">The service type</para>
///     <para xml:lang="zh">服务类型</para>
/// </typeparam>
public interface IIndex<in TKey, TService>
    where TKey : notnull
    where TService : notnull
{
    /// <summary>
    ///     <para xml:lang="en">Resolve a keyed service by its key</para>
    ///     <para xml:lang="zh">通过键解析 keyed 服务</para>
    /// </summary>
    /// <param name="key">
    ///     <para xml:lang="en">The service key</para>
    ///     <para xml:lang="zh">服务键</para>
    /// </param>
    TService this[TKey key] { get; }

    /// <summary>
    ///     <para xml:lang="en">Try to resolve a keyed service by its key</para>
    ///     <para xml:lang="zh">尝试通过键解析 keyed 服务</para>
    /// </summary>
    /// <param name="key">
    ///     <para xml:lang="en">The service key</para>
    ///     <para xml:lang="zh">服务键</para>
    /// </param>
    /// <param name="service">
    ///     <para xml:lang="en">The resolved service, or default if not found</para>
    ///     <para xml:lang="zh">已解析的服务，如果未找到则为默认值</para>
    /// </param>
    bool TryGet(TKey key, out TService? service);
}