using Microsoft.Extensions.DependencyInjection;

namespace EasilyNET.AutoDependencyInjection;

/// <summary>
///     <para xml:lang="en">
///     Provides controlled lifetime management for a resolved service, similar to Autofac's <c>Owned&lt;T&gt;</c>.
///     The service is resolved within an isolated <see cref="IServiceScope" />; disposing this instance
///     releases the scope and all scoped dependencies created within it.
///     </para>
///     <para xml:lang="zh">
///     提供对已解析服务的受控生命周期管理，类似于 Autofac 的 <c>Owned&lt;T&gt;</c>。
///     服务在独立的 <see cref="IServiceScope" /> 中解析；释放此实例时会释放该作用域及其中创建的所有 Scoped 依赖。
///     </para>
/// </summary>
/// <typeparam name="T">
///     <para xml:lang="en">The service type</para>
///     <para xml:lang="zh">服务类型</para>
/// </typeparam>
public sealed class Owned<T> : IDisposable
{
    private readonly IServiceScope _scope;
    private bool _disposed;

    internal Owned(IServiceScope scope, T value)
    {
        _scope = scope;
        Value = value;
    }

    /// <summary>
    ///     <para xml:lang="en">The resolved service instance</para>
    ///     <para xml:lang="zh">已解析的服务实例</para>
    /// </summary>
    public T Value { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _scope.Dispose();
    }
}