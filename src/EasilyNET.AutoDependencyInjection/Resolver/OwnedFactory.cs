using Microsoft.Extensions.DependencyInjection;

namespace EasilyNET.AutoDependencyInjection;

/// <summary>
///     <para xml:lang="en">
///     Internal factory that MS DI resolves as <c>Owned&lt;T&gt;</c> via open generic registration.
///     Creates a child scope, resolves T within it, and wraps both in an <see cref="Owned{T}" />.
///     </para>
///     <para xml:lang="zh">
///     内部工厂，MS DI 通过开放泛型注册将其解析为 <c>Owned&lt;T&gt;</c>。
///     创建子作用域，在其中解析 T，并将两者包装在 <see cref="Owned{T}" /> 中。
///     </para>
/// </summary>
internal static class OwnedFactory
{
    /// <summary>
    /// Create an <see cref="Owned{T}" /> by resolving T in a new child scope.
    /// </summary>
    internal static Owned<T> Create<T>(IServiceProvider provider) where T : notnull
    {
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        var scope = scopeFactory.CreateScope();
        try
        {
            var service = scope.ServiceProvider.GetRequiredService<T>();
            return new(scope, service);
        }
        catch
        {
            scope.Dispose();
            throw;
        }
    }
}