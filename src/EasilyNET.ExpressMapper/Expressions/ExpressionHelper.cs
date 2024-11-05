using System.Linq.Expressions;

namespace EasilyNET.ExpressMapper.Expressions;

/// <summary>
/// 表达式助手类，提供辅助方法来处理表达式。
/// Static helper class for handling expressions.
/// </summary>
public static class ExpressionHelper
{
    /// <summary>
    /// 返回指定类型的默认值表达式。
    /// Returns an expression representing the default value of the specified type.
    /// </summary>
    /// <param name="type">要获取默认值的类型。The type to get the default value for.</param>
    /// <returns>表示默认值的表达式。An expression representing the default value.</returns>
    public static Expression DefaultValue(Type type) => Expression.Constant(default, type);
}