using EasilyNET.AutoDependencyInjection.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace EasilyNET.AutoDependencyInjection;

/// <summary>
///     <para xml:lang="en">
///     Default implementation of <see cref="IIndex{TKey,TService}" /> that resolves keyed services
///     from the underlying <see cref="IServiceProvider" />.
///     </para>
///     <para xml:lang="zh">
///     <see cref="IIndex{TKey, TService}" /> 的默认实现，从底层 <see cref="IServiceProvider" /> 解析 keyed 服务。
///     </para>
/// </summary>
internal sealed class KeyedServiceIndex<TKey, TService>(IServiceProvider provider) : IIndex<TKey, TService>
    where TKey : notnull
    where TService : notnull
{
    /// <inheritdoc />
    public TService this[TKey key] => provider.GetRequiredKeyedService<TService>(key);

    /// <inheritdoc />
    public bool TryGet(TKey key, out TService? service)
    {
        service = provider.GetKeyedService<TService>(key);
        return service is not null;
    }
}