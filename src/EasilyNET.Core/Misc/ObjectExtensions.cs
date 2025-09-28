using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.Core.Misc;

/// <summary>
///     <para xml:lang="en">Object extensions, applicable to most types</para>
///     <para xml:lang="zh">Object 扩展，适用于大多数类型的扩展</para>
/// </summary>
public static class ObjectExtensions
{
    /// <summary>
    ///     <para xml:lang="en">
    ///     Uses a ConcurrentDictionary to cache object property information. For the same object and property, reflection is only needed
    ///     once. Subsequent operations can directly retrieve property information from the cache, improving performance when handling a large number of
    ///     objects.
    ///     </para>
    ///     <para xml:lang="zh">使用了一个 ConcurrentDictionary 来缓存对象的属性信息。对于同一个对象和属性，只需要进行一次反射操作，之后的操作可以直接从缓存中获取属性信息，这在处理大量对象时可以提高性能。</para>
    /// </summary>
    private static readonly ConcurrentDictionary<string, Lazy<PropertyInfo?>> CachedObjectProperties = new();

    /// <summary>
    ///     <para xml:lang="en">
    ///     Validates whether the specified assertion <paramref name="assertion" /> is true. If not, throws an exception of the specified
    ///     type <typeparamref name="TException" /> with the specified message <paramref name="message" />.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     验证指定值的断言 <paramref name="assertion" /> 是否为真，如果不为真，抛出指定消息 <paramref name="message" /> 的指定类型 <typeparamref name="TException" />
    ///     异常。
    ///     </para>
    /// </summary>
    /// <typeparam name="TException">
    ///     <para xml:lang="en">The type of exception to throw</para>
    ///     <para xml:lang="zh">异常类型</para>
    /// </typeparam>
    /// <param name="assertion">
    ///     <para xml:lang="en">The assertion to validate</para>
    ///     <para xml:lang="zh">要验证的断言</para>
    /// </param>
    /// <param name="message">
    ///     <para xml:lang="en">The exception message</para>
    ///     <para xml:lang="zh">异常消息</para>
    /// </param>
    public static void Require<TException>(bool assertion, string message) where TException : Exception
    {
        if (assertion)
        {
            return;
        }
        ArgumentException.ThrowIfNullOrEmpty(message);
        throw (TException)Activator.CreateInstance(typeof(TException), message)!;
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///     Checks if the string is not null or empty, otherwise throws an <see cref="ArgumentNullException" /> or
    ///     <see cref="ArgumentException" />.
    ///     </para>
    ///     <para xml:lang="zh">检查字符串不能为空引用或空字符串，否则抛出 <see cref="ArgumentNullException" /> 异常或 <see cref="ArgumentException" /> 异常。</para>
    /// </summary>
    /// <param name="value">
    ///     <para xml:lang="en">The string value</para>
    ///     <para xml:lang="zh">字符串值</para>
    /// </param>
    /// <param name="paramName">
    ///     <para xml:lang="en">The parameter name</para>
    ///     <para xml:lang="zh">参数名称</para>
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     <para xml:lang="en">Thrown when the string is null</para>
    ///     <para xml:lang="zh">当字符串为空时抛出</para>
    /// </exception>
    /// <exception cref="ArgumentException">
    ///     <para xml:lang="en">Thrown when the string is empty</para>
    ///     <para xml:lang="zh">当字符串为空时抛出</para>
    /// </exception>
    public static void NotNullOrEmpty(this string value, string paramName) => Require<ArgumentException>(!string.IsNullOrEmpty(value), $"参数“{paramName}”不能为空引用或空字符串。");

    /// <summary>
    ///     <para xml:lang="en">Checks if the Guid value is not Guid.Empty, otherwise throws an <see cref="ArgumentException" />.</para>
    ///     <para xml:lang="zh">检查 Guid 值不能为 Guid.Empty，否则抛出 <see cref="ArgumentException" /> 异常。</para>
    /// </summary>
    /// <param name="value">
    ///     <para xml:lang="en">The Guid value</para>
    ///     <para xml:lang="zh">Guid 值</para>
    /// </param>
    /// <param name="paramName">
    ///     <para xml:lang="en">The parameter name</para>
    ///     <para xml:lang="zh">参数名称</para>
    /// </param>
    /// <exception cref="ArgumentException">
    ///     <para xml:lang="en">Thrown when the Guid value is Guid.Empty</para>
    ///     <para xml:lang="zh">当 Guid 值为 Guid.Empty 时抛出</para>
    /// </exception>
    public static void NotEmpty(this Guid value, string paramName) => Require<ArgumentException>(value != Guid.Empty, $"参数“{paramName}”的值不能为 Guid.Empty");

    /// <summary>
    ///     <para xml:lang="en">
    ///     Checks if the collection is not null or empty, otherwise throws an <see cref="ArgumentNullException" /> or
    ///     <see cref="ArgumentException" />.
    ///     </para>
    ///     <para xml:lang="zh">检查集合不能为空引用或空集合，否则抛出 <see cref="ArgumentNullException" /> 异常或 <see cref="ArgumentException" /> 异常。</para>
    /// </summary>
    /// <typeparam name="T">
    ///     <para xml:lang="en">The type of the collection items</para>
    ///     <para xml:lang="zh">集合项的类型</para>
    /// </typeparam>
    /// <param name="collection">
    ///     <para xml:lang="en">The collection</para>
    ///     <para xml:lang="zh">集合</para>
    /// </param>
    /// <param name="paramName">
    ///     <para xml:lang="en">The parameter name</para>
    ///     <para xml:lang="zh">参数名称</para>
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     <para xml:lang="en">Thrown when the collection is null</para>
    ///     <para xml:lang="zh">当集合为空时抛出</para>
    /// </exception>
    /// <exception cref="ArgumentException">
    ///     <para xml:lang="en">Thrown when the collection is empty</para>
    ///     <para xml:lang="zh">当集合为空时抛出</para>
    /// </exception>
    public static void NotNullOrEmpty<T>(this IEnumerable<T> collection, string paramName) => Require<ArgumentException>(collection.Any(), $"参数“{paramName}”不能为空引用或空集合。");

    /// <summary>
    ///     <para xml:lang="en">Checks if the specified attribute is present</para>
    ///     <para xml:lang="zh">检查指定的特性是否存在</para>
    /// </summary>
    /// <typeparam name="T">
    ///     <para xml:lang="en">The type of the attribute to check</para>
    ///     <para xml:lang="zh">要检查的特性的类型</para>
    /// </typeparam>
    /// <param name="memberInfo">
    ///     <para xml:lang="en">The member info</para>
    ///     <para xml:lang="zh">成员信息</para>
    /// </param>
    /// <param name="inherit">
    ///     <para xml:lang="en">Whether to check inherited attributes</para>
    ///     <para xml:lang="zh">是否检查继承的特性</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">True if the attribute is present, otherwise false</para>
    ///     <para xml:lang="zh">如果存在则返回 true，否则返回 false</para>
    /// </returns>
    public static bool HasAttribute<T>(this MemberInfo memberInfo, bool inherit = true) where T : Attribute => memberInfo.IsDefined(typeof(T), inherit);

    /// <param name="value">
    ///     <para xml:lang="en">The value to validate</para>
    ///     <para xml:lang="zh">要验证的值</para>
    /// </param>
    /// <typeparam name="T">
    ///     <para xml:lang="en">The type of the value to validate</para>
    ///     <para xml:lang="zh">要验证的值的类型</para>
    /// </typeparam>
    extension<T>(T value)
    {
        /// <summary>
        ///     <para xml:lang="en">Validates whether the specified assertion expression is true. If not, throws an <see cref="Exception" />.</para>
        ///     <para xml:lang="zh">验证指定值的断言表达式是否为真，如果不为真，抛出 <see cref="Exception" /> 异常。</para>
        /// </summary>
        /// <param name="assertionFunc">
        ///     <para xml:lang="en">The assertion expression to validate</para>
        ///     <para xml:lang="zh">要验证的断言表达式</para>
        /// </param>
        /// <param name="message">
        ///     <para xml:lang="en">The exception message</para>
        ///     <para xml:lang="zh">异常消息</para>
        /// </param>
        public void Required(Func<T, bool> assertionFunc, string message)
        {
            ArgumentNullException.ThrowIfNull(assertionFunc);
            Require<Exception>(assertionFunc(value), message);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///     Validates whether the specified assertion expression is true. If not, throws an exception of the specified type
        ///     <typeparamref name="TException" />.
        ///     </para>
        ///     <para xml:lang="zh">验证指定值的断言表达式是否为真，如果不为真，抛出指定类型 <typeparamref name="TException" /> 的异常。</para>
        /// </summary>
        /// <typeparam name="TException">
        ///     <para xml:lang="en">The type of exception to throw</para>
        ///     <para xml:lang="zh">异常类型</para>
        /// </typeparam>
        /// <param name="assertionFunc">
        ///     <para xml:lang="en">The assertion expression to validate</para>
        ///     <para xml:lang="zh">要验证的断言表达式</para>
        /// </param>
        /// <param name="message">
        ///     <para xml:lang="en">The exception message</para>
        ///     <para xml:lang="zh">异常消息</para>
        /// </param>
        public void Required<TException>(Func<T, bool> assertionFunc, string message) where TException : Exception
        {
            ArgumentNullException.ThrowIfNull(assertionFunc);
            Require<TException>(assertionFunc(value), message);
        }

        /// <summary>
        ///     <para xml:lang="en">Checks if the parameter is not null, otherwise throws an <see cref="ArgumentNullException" />.</para>
        ///     <para xml:lang="zh">检查参数不能为空引用，否则抛出 <see cref="ArgumentNullException" /> 异常。</para>
        /// </summary>
        /// <param name="paramName">
        ///     <para xml:lang="en">The parameter name</para>
        ///     <para xml:lang="zh">参数名称</para>
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <para xml:lang="en">Thrown when the parameter is null</para>
        ///     <para xml:lang="zh">当参数为空时抛出</para>
        /// </exception>
        public void NotNull(string paramName) => Require<ArgumentNullException>(value is not null, $"参数“{paramName}”不能为空引用。");

        /// <summary>
        ///     <para xml:lang="en">
        ///     Dynamically sets the property value of an object at runtime. This method is thread-safe and useful for writing general data
        ///     mapping functions that need to dynamically set object properties based on different inputs.
        ///     </para>
        ///     <para xml:lang="zh">在运行时动态地设置对象的属性值。这个方法在多线程环境中也是安全的，在编写需要根据不同输入动态设置对象属性的通用数据映射函数时非常有用。</para>
        /// </summary>
        /// <typeparam name="TValue">
        ///     <para xml:lang="en">The type of the value</para>
        ///     <para xml:lang="zh">值的类型</para>
        /// </typeparam>
        /// <param name="propertySelector">
        ///     <para xml:lang="en">The property selector</para>
        ///     <para xml:lang="zh">属性选择器</para>
        /// </param>
        /// <param name="valueFactory">
        ///     <para xml:lang="en">
        ///     The value factory to compute the property value when needed. Useful for handling property values that are computationally
        ///     expensive or dependent on other factors.
        ///     </para>
        ///     <para xml:lang="zh">在需要的时候计算属性值的值工厂。处理计算成本较高或依赖于其他因素的属性值时非常有用。</para>
        /// </param>
        [RequiresUnreferencedCode("This method uses reflection and may not be compatible with AOT.")]
        public bool TrySetProperty<TValue>(Expression<Func<T, TValue>> propertySelector, Func<TValue> valueFactory)
        {
            return TrySetProperty(value, propertySelector, _ => valueFactory());
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///     Dynamically sets the property value of an object at runtime. This method is thread-safe and useful for writing general data
        ///     mapping functions that need to dynamically set object properties based on different inputs.
        ///     </para>
        ///     <para xml:lang="zh">在运行时动态地设置对象的属性值。这个方法在多线程环境中也是安全的，在编写需要根据不同输入动态设置对象属性的通用数据映射函数时非常有用。</para>
        /// </summary>
        /// <typeparam name="TValue">
        ///     <para xml:lang="en">The type of the value</para>
        ///     <para xml:lang="zh">值的类型</para>
        /// </typeparam>
        /// <param name="propertySelector">
        ///     <para xml:lang="en">The property selector</para>
        ///     <para xml:lang="zh">属性选择器</para>
        /// </param>
        /// <param name="valueFactory">
        ///     <para xml:lang="en">
        ///     The value factory to compute the property value when needed. Useful for handling property values that are computationally
        ///     expensive or dependent on other factors.
        ///     </para>
        ///     <para xml:lang="zh">在需要的时候计算属性值的值工厂。处理计算成本较高或依赖于其他因素的属性值时非常有用。</para>
        /// </param>
        [RequiresUnreferencedCode("This method uses reflection and may not be compatible with AOT.")]
        public bool TrySetProperty<TValue>(Expression<Func<T, TValue>> propertySelector, Func<T, TValue> valueFactory)
        {
            var cacheKey = $"{value?.GetType().FullName}_{propertySelector}";
            var property = CachedObjectProperties.GetOrAdd(cacheKey, _ => new(() =>
            {
                if (propertySelector.Body is not MemberExpression memberExpression)
                {
                    return null;
                }
                //if (propertySelector.Body is not { NodeType: ExpressionType.MemberAccess  } var memberExpression) return default;
                //根据成员表达式，获取对应属性，并且有set访问属性
                var propertyInfo = value?.GetType().GetProperties().FirstOrDefault(o => o.Name == memberExpression.Member.Name && o.GetSetMethod(true) is not null);
                return propertyInfo;
            }));
            if (property.Value == null)
            {
                return false;
            }
            try
            {
                property.Value.SetValue(value, valueFactory(value));
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}