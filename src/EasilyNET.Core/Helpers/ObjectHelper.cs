using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace EasilyNET.Core.Helpers;

/// <summary>
/// 对象
/// </summary>
public static class ObjectHelper
{
    /// <summary>
    /// 对象属性缓存
    /// </summary>
    private static readonly ConcurrentDictionary<string, Lazy<PropertyInfo?>> CachedObjectProperties = new();

    /// <summary>
    /// 设置属性值
    /// </summary>
    /// <typeparam name="TObject">动态对象</typeparam>
    /// <typeparam name="TValue">动态值</typeparam>
    /// <param name="obj">要设置对象</param>
    /// <param name="propertySelector">属性条件</param>
    /// <param name="valueFactory">值工厂</param>
    public static void TrySetProperty<TObject, TValue>(TObject obj, Expression<Func<TObject, TValue>> propertySelector, Func<TValue> valueFactory)
    {
        TrySetProperty(obj, propertySelector, _ => valueFactory());
    }

    /// <summary>
    /// 设置属性值
    /// </summary>
    /// <typeparam name="TObject">动态对象</typeparam>
    /// <typeparam name="TValue">动态值</typeparam>
    /// <param name="obj">要设置对象</param>
    /// <param name="propertySelector">属性条件</param>
    /// <param name="valueFactory">值工厂，返回已设置好值</param>
    public static void TrySetProperty<TObject, TValue>(TObject obj, Expression<Func<TObject, TValue>> propertySelector, Func<TObject, TValue> valueFactory)
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
        property.Value?.SetValue(obj, valueFactory(obj));
    }
}
