using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace EasilyNET.AutoDependencyInjection.Registry;

/// <summary>
///     <para xml:lang="en">
///     Registry for tracking service implementations and named/keyed services.
///     This class is scoped to an <see cref="IServiceCollection" /> instance to avoid global static state.
///     </para>
///     <para xml:lang="zh">
///     用于跟踪服务实现和命名/键控服务的注册表。
///     此类的作用域限定于 <see cref="IServiceCollection" /> 实例，以避免全局静态状态。
///     </para>
/// </summary>
internal sealed class ServiceRegistry
{
    /// <summary>
    ///     <para xml:lang="en">Caches key-type equality validation results to avoid repeated reflection on every named registration.</para>
    ///     <para xml:lang="zh">缓存键类型的相等性校验结果，避免每次命名注册都重复反射。</para>
    /// </summary>
    private static readonly ConcurrentDictionary<Type, bool> KeyTypeValidationCache = new();

    /// <summary>
    ///     <para xml:lang="en">
    ///     Named/Keyed services registry, keyed by (key, service type) to avoid collisions.
    ///     The key object must have proper equality semantics (value types, strings, or reference types
    ///     that correctly override <see cref="object.Equals(object)" /> and <see cref="object.GetHashCode" />).
    ///     </para>
    ///     <para xml:lang="zh">
    ///     命名/键控服务注册表，以 (key, 服务类型) 为键以避免冲突。
    ///     键对象必须具有正确的相等性语义（值类型、字符串或正确重写了
    ///     <see cref="object.Equals(object)" /> 和 <see cref="object.GetHashCode" /> 的引用类型）。
    ///     </para>
    /// </summary>
    private ConcurrentDictionary<(object Key, Type ServiceType), NamedServiceDescriptor> NamedServices { get; } = [];

    /// <summary>
    ///     <para xml:lang="en">Service type to implementation type mapping for parameter override resolution</para>
    ///     <para xml:lang="zh">服务类型到实现类型的映射，用于参数覆盖解析</para>
    /// </summary>
    private ConcurrentDictionary<Type, Type> ServiceImplementations { get; } = [];

    /// <summary>
    ///     <para xml:lang="en">
    ///     Optional predicate applied to each attribute-discovered implementation type during auto-registration.
    ///     When it returns <see langword="false" />, that type is skipped. <see langword="null" /> means register all.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     可选的过滤谓词，自动注册时对每个被特性发现的实现类型应用；返回 <see langword="false" /> 则跳过该类型。
    ///     为 <see langword="null" /> 时注册全部。
    ///     </para>
    /// </summary>
    internal Func<Type, bool>? RegistrationFilter { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Register a named/keyed service</para>
    ///     <para xml:lang="zh">注册命名/键控服务</para>
    /// </summary>
    /// <param name="key">
    ///     <para xml:lang="en">
    ///     The service key. Must be a value type, string, or a reference type that correctly
    ///     overrides <see cref="object.Equals(object)" /> and <see cref="object.GetHashCode" />.
    ///     Using reference types without proper equality implementation will cause lookup failures.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     服务键。必须是值类型、字符串或正确重写了 <see cref="object.Equals(object)" /> 和
    ///     <see cref="object.GetHashCode" /> 的引用类型。使用没有正确相等性实现的引用类型将导致查找失败。
    ///     </para>
    /// </param>
    /// <param name="serviceType">
    ///     <para xml:lang="en">The service type</para>
    ///     <para xml:lang="zh">服务类型</para>
    /// </param>
    /// <param name="implementationType">
    ///     <para xml:lang="en">The implementation type</para>
    ///     <para xml:lang="zh">实现类型</para>
    /// </param>
    /// <param name="lifetime">
    ///     <para xml:lang="en">The service lifetime</para>
    ///     <para xml:lang="zh">服务生命周期</para>
    /// </param>
    internal void RegisterNamedService(object key, Type serviceType, Type implementationType, ServiceLifetime lifetime)
    {
        ValidateKeyEquality(key);
        var descriptor = new NamedServiceDescriptor(serviceType, implementationType, lifetime);
        NamedServices[(key, serviceType)] = descriptor;
    }

    /// <summary>
    ///     <para xml:lang="en">Register a service implementation mapping</para>
    ///     <para xml:lang="zh">注册服务实现映射</para>
    /// </summary>
    /// <param name="serviceType">
    ///     <para xml:lang="en">The service type</para>
    ///     <para xml:lang="zh">服务类型</para>
    /// </param>
    /// <param name="implementationType">
    ///     <para xml:lang="en">The implementation type</para>
    ///     <para xml:lang="zh">实现类型</para>
    /// </param>
    internal void RegisterImplementation(Type serviceType, Type implementationType)
    {
        ServiceImplementations[serviceType] = implementationType;
    }

    /// <summary>
    ///     <para xml:lang="en">Try to get the implementation type for a service type</para>
    ///     <para xml:lang="zh">尝试获取服务类型的实现类型</para>
    /// </summary>
    /// <param name="serviceType">
    ///     <para xml:lang="en">The service type</para>
    ///     <para xml:lang="zh">服务类型</para>
    /// </param>
    /// <param name="implementationType">
    ///     <para xml:lang="en">The implementation type if found</para>
    ///     <para xml:lang="zh">如果找到则为实现类型</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">True if found, false otherwise</para>
    ///     <para xml:lang="zh">如果找到则返回 true，否则返回 false</para>
    /// </returns>
    internal bool TryGetImplementationType(Type serviceType, out Type? implementationType) => ServiceImplementations.TryGetValue(serviceType, out implementationType);

    /// <summary>
    ///     <para xml:lang="en">Try to get a named service descriptor</para>
    ///     <para xml:lang="zh">尝试获取命名服务描述符</para>
    /// </summary>
    /// <param name="key">
    ///     <para xml:lang="en">The service key</para>
    ///     <para xml:lang="zh">服务键</para>
    /// </param>
    /// <param name="serviceType">
    ///     <para xml:lang="en">The service type</para>
    ///     <para xml:lang="zh">服务类型</para>
    /// </param>
    /// <param name="descriptor">
    ///     <para xml:lang="en">The descriptor if found</para>
    ///     <para xml:lang="zh">如果找到则为描述符</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">True if found, false otherwise</para>
    ///     <para xml:lang="zh">如果找到则返回 true，否则返回 false</para>
    /// </returns>
    internal bool TryGetNamedService(object key, Type serviceType, out NamedServiceDescriptor? descriptor) => NamedServices.TryGetValue((key, serviceType), out descriptor);

    /// <summary>
    ///     <para xml:lang="en">Get all registered service types and their implementations (for diagnostics)</para>
    ///     <para xml:lang="zh">获取所有已注册的服务类型及其实现（用于诊断）</para>
    /// </summary>
    internal IReadOnlyDictionary<Type, Type> GetAllImplementations() => ServiceImplementations;

    /// <summary>
    ///     <para xml:lang="en">Get all registered named services (for diagnostics)</para>
    ///     <para xml:lang="zh">获取所有已注册的命名服务（用于诊断）</para>
    /// </summary>
    internal IReadOnlyDictionary<(object Key, Type ServiceType), NamedServiceDescriptor> GetAllNamedServices() => NamedServices;

    /// <summary>
    ///     <para xml:lang="en">
    ///     Validates that the key has proper equality semantics for use in dictionary lookups.
    ///     Value types and strings are always valid. Reference types must override Equals and GetHashCode.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     验证键是否具有适合字典查找的正确相等性语义。
    ///     值类型和字符串始终有效。引用类型必须重写 Equals 和 GetHashCode。
    ///     </para>
    /// </summary>
    /// <param name="key">
    ///     <para xml:lang="en">The key to validate</para>
    ///     <para xml:lang="zh">要验证的键</para>
    /// </param>
    /// <exception cref="ArgumentException">
    ///     <para xml:lang="en">Thrown when the key is a reference type that does not override Equals or GetHashCode</para>
    ///     <para xml:lang="zh">当键是未重写 Equals 或 GetHashCode 的引用类型时抛出</para>
    /// </exception>
    private static void ValidateKeyEquality(object key)
    {
        var keyType = key.GetType();
        // 校验结果按类型缓存，反射只在每个类型首次出现时执行一次。
        if (KeyTypeValidationCache.GetOrAdd(keyType, static t => HasProperEqualitySemantics(t)))
        {
            return;
        }
        throw new ArgumentException($"The key type '{keyType.Name}' does not override Equals and/or GetHashCode. " +
                                    "Service keys must be value types, strings, or reference types with proper equality implementation. " +
                                    "This is required for correct dictionary lookups.",
            nameof(key));
    }

    /// <summary>
    ///     <para xml:lang="en">Determines whether a key type has proper equality semantics for dictionary lookups.</para>
    ///     <para xml:lang="zh">判断键类型是否具备用于字典查找的正确相等性语义。</para>
    /// </summary>
    private static bool HasProperEqualitySemantics(Type keyType)
    {
        // Value types and strings have proper equality semantics by default
        if (keyType.IsValueType || keyType == typeof(string))
        {
            return true;
        }
        // Reference types must override both Equals and GetHashCode (declared in a type other than System.Object)
        var equalsMethod = keyType.GetMethod(nameof(Equals), [typeof(object)]);
        var getHashCodeMethod = keyType.GetMethod(nameof(GetHashCode), Type.EmptyTypes);
        return equalsMethod?.DeclaringType != typeof(object) && getHashCodeMethod?.DeclaringType != typeof(object);
    }
}