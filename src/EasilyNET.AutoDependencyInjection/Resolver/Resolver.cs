using System.Collections.Concurrent;
using System.Reflection;
using EasilyNET.AutoDependencyInjection.Abstractions;
using EasilyNET.AutoDependencyInjection.Core.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace EasilyNET.AutoDependencyInjection;

/// <summary>
///     <para xml:lang="en">Creates a new resolver instance</para>
///     <para xml:lang="zh">创建新的解析器实例</para>
/// </summary>
/// <param name="provider">
///     <para xml:lang="en">The service provider</para>
///     <para xml:lang="zh">服务提供者</para>
/// </param>
/// <param name="registry">
///     <para xml:lang="en">The service registry (optional, will try to resolve from provider if not provided)</para>
///     <para xml:lang="zh">服务注册表（可选，如果未提供将尝试从提供者解析）</para>
/// </param>
/// <param name="scope">
///     <para xml:lang="en">The service scope (optional)</para>
///     <para xml:lang="zh">服务作用域（可选）</para>
/// </param>
internal sealed class Resolver(IServiceProvider provider, ServiceRegistry? registry = null, IServiceScope? scope = null) : IResolver
{
    // 缓存构造函数及其参数信息，避免重复反射
    private static readonly ConcurrentDictionary<Type, ConstructorCache> CtorCache = [];
    private readonly IServiceProviderIsService? _isService = provider.GetService<IServiceProviderIsService>();
    private readonly ServiceRegistry? _registry = registry ?? provider.GetService<ServiceRegistry>();

    /// <inheritdoc />
    public void Dispose()
    {
        scope?.Dispose();
    }

    /// <inheritdoc />
    public T Resolve<T>(params Parameter[] parameters) => (T)Resolve(typeof(T), parameters);

    /// <inheritdoc />
    public object Resolve(Type serviceType, params Parameter[]? parameters)
    {
        if (parameters is null || parameters.Length == 0)
        {
            return provider.GetService(serviceType) ?? throw new InvalidOperationException($"Unable to resolve service of type '{serviceType.Name}'.");
        }
        // 使用参数覆盖创建实例：必须找到实现类型
        var implType = GetImplementationType(serviceType) ?? throw new InvalidOperationException($"Unable to determine implementation type for '{serviceType.Name}'. Register via {nameof(DependencyInjectionAttribute)} or manual registration.");
        var cache = GetOrCreateCtorCache(implType);
        var ctor = SelectBestConstructor(cache, parameters, implType);
        var args = BuildArguments(cache.GetParameterInfos(ctor), parameters);
        return ctor.Invoke(args);
    }

    /// <inheritdoc />
    public T ResolveNamed<T>(string name, params Parameter[]? parameters) => ResolveKeyed<T>(name, parameters);

    /// <inheritdoc />
    public T ResolveKeyed<T>(object key, params Parameter[]? parameters)
    {
        // 无参数时优先使用内置 keyed service
        if (parameters is null || parameters.Length == 0)
        {
            // 使用 GetKeyedService 避免异常驱动的流程控制
            var keyedService = provider.GetKeyedService<T>(key);
            if (keyedService is not null)
            {
                return keyedService;
            }
        }
        // 从 ServiceRegistry 获取（如果可用）
        if (_registry is null || !_registry.TryGetNamedService(key, typeof(T), out var descriptor) || descriptor is null)
        {
            throw new InvalidOperationException($"No keyed service registered for key '{key}' and type '{typeof(T).Name}'.");
        }
        var cache = GetOrCreateCtorCache(descriptor.ImplementationType);
        var ctor = SelectBestConstructor(cache, parameters ?? [], descriptor.ImplementationType);
        var args = BuildArguments(cache.GetParameterInfos(ctor), parameters ?? []);
        return (T)ctor.Invoke(args);
    }

    /// <summary>
    /// 获取或创建构造函数缓存
    /// </summary>
    private static ConstructorCache GetOrCreateCtorCache(Type implType)
    {
        return CtorCache.GetOrAdd(implType, static t =>
        {
            var ctors = t.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            return ctors.Length == 0 ? throw new InvalidOperationException($"No public constructor found for type {t.Name}.") : new(ctors);
        });
    }

    /// <summary>
    /// 选择最佳构造函数（所有参数都可满足的优先）
    /// </summary>
    private ConstructorInfo SelectBestConstructor(ConstructorCache cache, Parameter[] parameters, Type implType)
    {
        foreach (var ctor in cache.Constructors)
        {
            var paramInfos = cache.GetParameterInfos(ctor);
            if (CanSatisfyAllParameters(paramInfos, parameters))
            {
                return ctor;
            }
        }
        // 没有完全匹配的构造函数时，提供详细错误信息
        var ctorSignatures = string.Join("; ", cache.Constructors.Select(c =>
            $"({string.Join(", ", c.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"))})"));
        throw new InvalidOperationException($"No suitable constructor found for type '{implType.Name}'. " +
                                            $"Available constructors: {ctorSignatures}. " +
                                            $"Provided parameters: [{string.Join(", ", parameters.Select(p => p.GetType().Name))}].");
    }

    /// <summary>
    /// 检查是否所有参数都可以被满足
    /// </summary>
    private bool CanSatisfyAllParameters(CachedParameterInfo[] paramInfos, Parameter[] parameters)
    {
        var hasUnsatisfiedParameter = false;
        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var p in paramInfos)
        {
            var canSupply = parameters.Any(prm => prm.CanSupplyValue(p.ParameterType, p.Name, p.Position));
            if (canSupply)
            {
                continue;
            }
            // Use IServiceProviderIsService for O(1) check instead of GetService which may instantiate
            var isRegistered = _isService?.IsService(p.ParameterType) ?? provider.GetService(p.ParameterType) is not null;
            if (isRegistered)
            {
                continue;
            }
            hasUnsatisfiedParameter = true;
            break;
        }
        return !hasUnsatisfiedParameter;
    }

    /// <summary>
    /// 构建构造函数参数数组
    /// </summary>
    private object?[] BuildArguments(CachedParameterInfo[] paramInfos, Parameter[] parameters)
    {
        var args = new object?[paramInfos.Length];
        for (var i = 0; i < paramInfos.Length; i++)
        {
            var p = paramInfos[i];
            object? value = null;
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var prm in parameters)
            {
                if (!prm.CanSupplyValue(p.ParameterType, p.Name, p.Position))
                {
                    continue;
                }
                value = prm.GetValue(provider, p.ParameterType, p.Name, p.Position);
                break;
            }
            // 如果用户没有提供参数覆盖，则从服务容器解析
            value ??= p.ServiceKey is not null
                          ? provider.GetRequiredKeyedService(p.ParameterType, p.ServiceKey)
                          : provider.GetRequiredService(p.ParameterType);
            args[i] = value;
        }
        return args;
    }

    private Type? GetImplementationType(Type serviceType)
    {
        if (serviceType is { IsAbstract: false, IsInterface: false })
        {
            return serviceType;
        }
        // 优先使用 ServiceRegistry
        return _registry is not null && _registry.TryGetImplementationType(serviceType, out var impl) ? impl : null;
    }

    /// <summary>
    /// 缓存参数信息，避免重复调用 GetParameters() 和 GetCustomAttributes()
    /// </summary>
    private sealed record CachedParameterInfo(int Position, Type ParameterType, string? Name, object? ServiceKey);

    /// <summary>
    /// 构造函数缓存，按参数数量降序排列
    /// </summary>
    private sealed class ConstructorCache(ConstructorInfo[] ctors)
    {
        private readonly ConcurrentDictionary<ConstructorInfo, CachedParameterInfo[]> _parameterCache = new();

        public ConstructorInfo[] Constructors { get; } = [.. ctors.OrderByDescending(c => c.GetParameters().Length)];

        public CachedParameterInfo[] GetParameterInfos(ConstructorInfo ctor)
        {
            return _parameterCache.GetOrAdd(ctor, static c =>
            {
                var ps = c.GetParameters();
                var result = new CachedParameterInfo[ps.Length];
                for (var i = 0; i < ps.Length; i++)
                {
                    var p = ps[i];
                    object? serviceKey = null;
                    var keyedAttr = p.GetCustomAttribute<FromKeyedServicesAttribute>();
                    if (keyedAttr is not null)
                    {
                        serviceKey = keyedAttr.Key;
                    }
                    result[i] = new(i, p.ParameterType, p.Name, serviceKey);
                }
                return result;
            });
        }
    }
}