using EasilyNET.AutoDependencyInjection;

// ReSharper disable UnusedMethodReturnValue.Global
// ReSharper disable UnusedMember.Global

#pragma warning disable IDE0130 // 命名空间与文件夹结构不匹配

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
///     <para xml:lang="en">
///     Extension methods for registering parameterized factory delegates (Func&lt;X, T&gt;, Func&lt;X, Y, T&gt;, etc.)
///     inspired by Autofac's implicit relationship types.
///     </para>
///     <para xml:lang="zh">
///     注册参数化工厂委托（Func&lt;X, T&gt;、Func&lt;X, Y, T&gt; 等）的扩展方法，
///     灵感来自 Autofac 的隐式关系类型。
///     </para>
/// </summary>
public static class ParameterizedFactoryExtensions
{
    /// <param name="services">
    ///     <para xml:lang="en">The service collection</para>
    ///     <para xml:lang="zh">服务集合</para>
    /// </param>
    extension(IServiceCollection services)
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///     Register a <c>Func&lt;TParam, TService&gt;</c> factory that resolves <typeparamref name="TService" />
        ///     using a <see cref="TypedParameter" /> for the runtime argument.
        ///     </para>
        ///     <para xml:lang="zh">
        ///     注册 <c>Func&lt;TParam, TService&gt;</c> 工厂，使用 <see cref="TypedParameter" /> 传递运行时参数来解析 <typeparamref name="TService" />。
        ///     </para>
        /// </summary>
        /// <example>
        ///     <code>
        /// services.AddParameterizedFactory&lt;string, IFooService&gt;();
        /// // Then inject Func&lt;string, IFooService&gt; into your class:
        /// public class MyController(Func&lt;string, IFooService&gt; fooFactory)
        /// {
        ///     public void Do() =&gt; fooFactory("Rose").SayHello();
        /// }
        /// </code>
        /// </example>
        public IServiceCollection AddParameterizedFactory<TParam, TService>() where TService : notnull
        {
            services.AddTransient<Func<TParam, TService>>(sp =>
                param =>
                {
                    using var resolver = sp.CreateResolver();
                    return resolver.Resolve<TService>(new TypedParameter(typeof(TParam), param));
                });
            return services;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///     Register a <c>Func&lt;TParam1, TParam2, TService&gt;</c> factory that resolves <typeparamref name="TService" />
        ///     using <see cref="TypedParameter" /> for each runtime argument.
        ///     </para>
        ///     <para xml:lang="zh">
        ///     注册 <c>Func&lt;TParam1, TParam2, TService&gt;</c> 工厂，使用 <see cref="TypedParameter" /> 传递运行时参数来解析 <typeparamref name="TService" />。
        ///     </para>
        /// </summary>
        public IServiceCollection AddParameterizedFactory<TParam1, TParam2, TService>() where TService : notnull
        {
            services.AddTransient<Func<TParam1, TParam2, TService>>(sp =>
                (p1, p2) =>
                {
                    using var resolver = sp.CreateResolver();
                    return resolver.Resolve<TService>(new TypedParameter(typeof(TParam1), p1),
                        new TypedParameter(typeof(TParam2), p2));
                });
            return services;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///     Register a <c>Func&lt;TParam1, TParam2, TParam3, TService&gt;</c> factory that resolves <typeparamref name="TService" />
        ///     using <see cref="TypedParameter" /> for each runtime argument.
        ///     </para>
        ///     <para xml:lang="zh">
        ///     注册 <c>Func&lt;TParam1, TParam2, TParam3, TService&gt;</c> 工厂，使用 <see cref="TypedParameter" /> 传递运行时参数来解析 <typeparamref name="TService" />。
        ///     </para>
        /// </summary>
        public IServiceCollection AddParameterizedFactory<TParam1, TParam2, TParam3, TService>() where TService : notnull
        {
            services.AddTransient<Func<TParam1, TParam2, TParam3, TService>>(sp =>
                (p1, p2, p3) =>
                {
                    using var resolver = sp.CreateResolver();
                    return resolver.Resolve<TService>(new TypedParameter(typeof(TParam1), p1),
                        new TypedParameter(typeof(TParam2), p2),
                        new TypedParameter(typeof(TParam3), p3));
                });
            return services;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///     Register a <c>Func&lt;Owned&lt;TService&gt;&gt;</c> factory that creates lifetime-controlled instances.
        ///     Each call to the factory creates a new scope; disposing the <see cref="Owned{T}" /> releases it.
        ///     </para>
        ///     <para xml:lang="zh">
        ///     注册 <c>Func&lt;Owned&lt;TService&gt;&gt;</c> 工厂，创建生命周期受控的实例。
        ///     每次调用工厂都会创建新的作用域；释放 <see cref="Owned{T}" /> 时释放该作用域。
        ///     </para>
        /// </summary>
        public IServiceCollection AddOwnedFactory<TService>() where TService : notnull
        {
            services.AddTransient<Func<Owned<TService>>>(sp =>
                () => OwnedFactory.Create<TService>(sp));
            return services;
        }
    }
}