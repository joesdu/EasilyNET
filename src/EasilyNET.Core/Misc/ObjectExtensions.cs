using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.Core.Misc;

/// <summary>
/// Object扩展,适用于大多数类型的扩展.
/// </summary>
public static class ObjectExtensions
{
    /// <summary>
    /// 使用了一个ConcurrentDictionary来缓存对象的属性信息.对于同一个对象和属性,只需要进行一次反射操作,之后的操作可以直接从缓存中获取属性信息,这在处理大量对象时可以提高性能
    /// </summary>
    private static readonly ConcurrentDictionary<string, Lazy<PropertyInfo?>> CachedObjectProperties = new();

    /// <summary>
    /// 验证指定值的断言<paramref name="assertion" />是否为真，如果不为真，抛出指定消息<paramref name="message" />的指定类型<typeparamref name="TException" />异常
    /// </summary>
    /// <typeparam name="TException">异常类型</typeparam>
    /// <param name="assertion">要验证的断言。</param>
    /// <param name="message">异常消息。</param>
    public static void Require<TException>(bool assertion, string message) where TException : Exception
    {
        if (assertion) return;
        ArgumentException.ThrowIfNullOrEmpty(message, nameof(message));
        throw (TException)Activator.CreateInstance(typeof(TException), message)!;
    }

    /// <summary>
    /// 验证指定值的断言表达式是否为真，不为值抛出<see cref="Exception" />异常
    /// </summary>
    /// <param name="value"></param>
    /// <param name="assertionFunc">要验证的断言表达式</param>
    /// <param name="message">异常消息</param>
    public static void Required<T>(this T value, Func<T, bool> assertionFunc, string message)
    {
        ArgumentNullException.ThrowIfNull(assertionFunc, nameof(assertionFunc));
        Require<Exception>(assertionFunc(value), message);
    }

    /// <summary>
    /// 验证指定值的断言表达式是否为真，不为真抛出<typeparamref name="TException" />异常
    /// </summary>
    /// <typeparam name="T">要判断的值的类型</typeparam>
    /// <typeparam name="TException">抛出的异常类型</typeparam>
    /// <param name="value">要判断的值</param>
    /// <param name="assertionFunc">要验证的断言表达式</param>
    /// <param name="message">异常消息</param>
    public static void Required<T, TException>(this T value, Func<T, bool> assertionFunc, string message) where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(assertionFunc, nameof(assertionFunc));
        Require<TException>(assertionFunc(value), message);
    }

    /// <summary>
    /// 检查参数不能为空引用，否则抛出<see cref="ArgumentNullException" />异常。
    /// </summary>
    /// <param name="value"></param>
    /// <param name="paramName">参数名称</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static void NotNull<T>(this T value, string paramName) => Require<ArgumentNullException>(value is not null, $"参数“{paramName}”不能为空引用。");

    /// <summary>
    /// 检查字符串不能为空引用或空字符串，否则抛出<see cref="ArgumentNullException" />异常或<see cref="ArgumentException" />异常。
    /// </summary>
    /// <param name="value"></param>
    /// <param name="paramName">参数名称。</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public static void NotNullOrEmpty(this string value, string paramName) => Require<ArgumentException>(!string.IsNullOrEmpty(value), $"参数“{paramName}”不能为空引用或空字符串。");

    /// <summary>
    /// 检查Guid值不能为Guid.Empty，否则抛出<see cref="ArgumentException" />异常。
    /// </summary>
    /// <param name="value"></param>
    /// <param name="paramName">参数名称。</param>
    /// <exception cref="ArgumentException"></exception>
    public static void NotEmpty(this Guid value, string paramName) => Require<ArgumentException>(value != Guid.Empty, $"参数“{paramName}”的值不能为Guid.Empty");

    /// <summary>
    /// 检查集合不能为空引用或空集合，否则抛出<see cref="ArgumentNullException" />异常或<see cref="ArgumentException" />异常。
    /// </summary>
    /// <typeparam name="T">集合项的类型。</typeparam>
    /// <param name="collection"></param>
    /// <param name="paramName">参数名称。</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public static void NotNullOrEmpty<T>(this IEnumerable<T> collection, string paramName) => Require<ArgumentException>(collection.Any(), $"参数“{paramName}”不能为空引用或空集合。");

    /// <summary>
    /// 判断特性相应是否存在
    /// </summary>
    /// <typeparam name="T">动态类型要判断的特性</typeparam>
    /// <param name="memberInfo"></param>
    /// <param name="inherit"></param>
    /// <returns>如果存在还在返回true，否则返回false</returns>
    public static bool HasAttribute<T>(this MemberInfo memberInfo, bool inherit = true) where T : Attribute => memberInfo.IsDefined(typeof(T), inherit);

    /// <summary>
    /// 类型转换
    /// </summary>
    /// <param name="value">要转换的值</param>
    /// <param name="type">类型</param>
    /// <returns></returns>
    public static object? ChangeType(this object? value, Type type)
    {
        if (value is null or DBNull)
        {
            return null;
        }
        //如果是Nullable类型
        if (type.IsNullable())
        {
            type = type.GetUnNullableType();
        }
        if (type != typeof(Guid)) return Convert.ChangeType(value, type);
        var success = Guid.TryParse(value.ToString(), out var newGuid);
        return success ? newGuid : Convert.ChangeType(value, type);
    }

    /// <summary>
    /// 转换类型
    /// </summary>
    /// <param name="value">值</param>
    /// <typeparam name="T">要转成的类型</typeparam>
    /// <returns></returns>
    public static T? ChangeType<T>(this object? value) => (T?)ChangeType(value, typeof(T));

    /// <summary>
    /// 当需要在运行时动态地设置对象的属性值时,这个方法在多线程环境中也是安全的,在编写需要根据不同输入动态设置对象属性的通用数据映射函数时非常有用
    /// </summary>
    /// <typeparam name="TObject">动态对象</typeparam>
    /// <typeparam name="TValue">动态值</typeparam>
    /// <param name="obj">要设置对象</param>
    /// <param name="propertySelector">属性条件</param>
    /// <param name="valueFactory">在需要的时候才计算属性的值,处理计算成本较高或依赖于其他因素的属性值时非常有用</param>
    public static bool TrySetProperty<TObject, TValue>(this TObject obj, Expression<Func<TObject, TValue>> propertySelector, Func<TValue> valueFactory)
    {
        return TrySetProperty(obj, propertySelector, _ => valueFactory());
    }

    /// <summary>
    /// 当需要在运行时动态地设置对象的属性值时,这个方法在多线程环境中也是安全的,在编写需要根据不同输入动态设置对象属性的通用数据映射函数时非常有用
    /// </summary>
    /// <typeparam name="TObject">动态对象</typeparam>
    /// <typeparam name="TValue">动态值</typeparam>
    /// <param name="obj">要设置对象</param>
    /// <param name="propertySelector">属性条件</param>
    /// <param name="valueFactory">在需要的时候才计算属性的值,处理计算成本较高或依赖于其他因素的属性值时非常有用</param>
    public static bool TrySetProperty<TObject, TValue>(this TObject obj, Expression<Func<TObject, TValue>> propertySelector, Func<TObject, TValue> valueFactory)
    {
        var cacheKey = $"{obj?.GetType().FullName}_{propertySelector}";
        var property = CachedObjectProperties.GetOrAdd(cacheKey, _ => new(() =>
        {
            if (propertySelector.Body is not MemberExpression memberExpression)
            {
                return default;
            }
            //if (propertySelector.Body is not { NodeType: ExpressionType.MemberAccess  } var memberExpression) return default;
            //根据成员表达式，获取对应属性，并且有set访问属性
            var propertyInfo = obj?.GetType().GetProperties().FirstOrDefault(o => o.Name == memberExpression.Member.Name && o.GetSetMethod(true) is not null);
            return propertyInfo;
        }));
        if (property.Value == null)
        {
            return false;
        }
        try
        {
            property.Value.SetValue(obj, valueFactory(obj));
            return true;
        }
        catch
        {
            return false;
        }
    }
}