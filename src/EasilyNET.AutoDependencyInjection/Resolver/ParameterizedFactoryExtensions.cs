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
        ///     using <see cref="PositionalParameter" /> for each runtime argument to avoid ambiguity when parameter types are identical.
        ///     </para>
        ///     <para xml:lang="zh">
        ///     注册 <c>Func&lt;TParam1, TParam2, TService&gt;</c> 工厂，使用 <see cref="PositionalParameter" /> 按位置传递运行时参数来解析 <typeparamref name="TService" />，
        ///     避免参数类型相同时的匹配歧义。
        ///     </para>
        /// </summary>
        public IServiceCollection AddParameterizedFactory<TParam1, TParam2, TService>() where TService : notnull
        {
            services.AddTransient<Func<TParam1, TParam2, TService>>(sp =>
                (p1, p2) =>
                {
                    using var resolver = sp.CreateResolver();
                    return resolver.Resolve<TService>(new PositionalParameter(0, p1),
                        new PositionalParameter(1, p2));
                });
            return services;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///     Register a <c>Func&lt;TParam1, TParam2, TParam3, TService&gt;</c> factory that resolves <typeparamref name="TService" />
        ///     using <see cref="PositionalParameter" /> for each runtime argument to avoid ambiguity when parameter types are identical.
        ///     </para>
        ///     <para xml:lang="zh">
        ///     注册 <c>Func&lt;TParam1, TParam2, TParam3, TService&gt;</c> 工厂，使用 <see cref="PositionalParameter" /> 按位置传递运行时参数来解析 <typeparamref name="TService" />
        ///     ，
        ///     避免参数类型相同时的匹配歧义。
        ///     </para>
        /// </summary>
        public IServiceCollection AddParameterizedFactory<TParam1, TParam2, TParam3, TService>() where TService : notnull
        {
            services.AddTransient<Func<TParam1, TParam2, TParam3, TService>>(sp =>
                (p1, p2, p3) =>
                {
                    using var resolver = sp.CreateResolver();
                    return resolver.Resolve<TService>(new PositionalParameter(0, p1),
                        new PositionalParameter(1, p2),
                        new PositionalParameter(2, p3));
                });
            return services;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///     Register a <c>Func&lt;TParam1, TParam2, TParam3, TParam4, TService&gt;</c> factory that resolves <typeparamref name="TService" />
        ///     using <see cref="PositionalParameter" /> for each runtime argument to avoid ambiguity when parameter types are identical.
        ///     </para>
        ///     <para xml:lang="zh">
        ///     注册 <c>Func&lt;TParam1, TParam2, TParam3, TParam4, TService&gt;</c> 工厂，使用 <see cref="PositionalParameter" /> 按位置传递运行时参数来解析
        ///     <typeparamref name="TService" />，
        ///     避免参数类型相同时的匹配歧义。
        ///     </para>
        /// </summary>
        public IServiceCollection AddParameterizedFactory<TParam1, TParam2, TParam3, TParam4, TService>() where TService : notnull
        {
            services.AddTransient<Func<TParam1, TParam2, TParam3, TParam4, TService>>(sp =>
                (p1, p2, p3, p4) =>
                {
                    using var resolver = sp.CreateResolver();
                    return resolver.Resolve<TService>(new PositionalParameter(0, p1),
                        new PositionalParameter(1, p2),
                        new PositionalParameter(2, p3),
                        new PositionalParameter(3, p4));
                });
            return services;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///     Register a general-purpose <c>Func&lt;object[], TService&gt;</c> factory for services with 5+ constructor parameters.
        ///     Arguments are matched by position using <see cref="PositionalParameter" />.
        ///     The <paramref name="paramTypes" /> array declares the expected types for runtime validation.
        ///     </para>
        ///     <para xml:lang="zh">
        ///     注册通用的 <c>Func&lt;object[], TService&gt;</c> 工厂，适用于构造函数参数超过 4 个的服务。
        ///     参数按位置使用 <see cref="PositionalParameter" /> 匹配。
        ///     <paramref name="paramTypes" /> 数组声明期望的参数类型，用于运行时校验。
        ///     </para>
        /// </summary>
        /// <example>
        ///     <code>
        /// services.AddParameterizedFactory&lt;MyService&gt;(typeof(string), typeof(int), typeof(bool), typeof(string), typeof(double));
        /// // Then inject Func&lt;object[], MyService&gt; into your class:
        /// public class MyController(Func&lt;object[], MyService&gt; factory)
        /// {
        ///     public void Do() =&gt; factory(["hello", 42, true, "world", 3.14]);
        /// }
        /// </code>
        /// </example>
        public IServiceCollection AddParameterizedFactory<TService>(params Type[] paramTypes) where TService : notnull
        {
            ArgumentNullException.ThrowIfNull(paramTypes);
            // Capture a defensive copy so the caller cannot mutate the array after registration
            var expectedTypes = paramTypes.ToArray();
            services.AddTransient<Func<object[], TService>>(sp =>
                args =>
                {
                    ArgumentNullException.ThrowIfNull(args);
                    if (args.Length != expectedTypes.Length)
                    {
                        throw new ArgumentException($"Expected {expectedTypes.Length} arguments for {typeof(TService).Name}, but got {args.Length}.");
                    }
                    var parameters = new Parameter[args.Length];
                    for (var i = 0; i < args.Length; i++)
                    {
                        if (expectedTypes[i].IsInstanceOfType(args[i]))
                        {
                            parameters[i] = new PositionalParameter(i, args[i]);
                        }
                        else
                        {
                            throw new ArgumentException($"Argument at position {i} for {typeof(TService).Name} expected type '{expectedTypes[i].Name}', but got '{args[i].GetType().Name}'.");
                        }
                    }
                    using var resolver = sp.CreateResolver();
                    return resolver.Resolve<TService>(parameters);
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