using System.Runtime.CompilerServices;

namespace EasilyNET.Core.Misc.Exceptions;

/// <summary>
/// 异常处理的一些扩展
/// </summary>
public static class ArgumentExceptionExtensions
{
    /// <summary>
    /// 若是条件成立抛出异常,否则通过
    /// </summary>
    /// <param name="express">条件表达式</param>
    /// <param name="message">信息</param>
    /// <param name="paramName">参数名称</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static void ThrowIf(Func<bool> express, string? message, [CallerArgumentExpression(nameof(express))] string? paramName = null)
    {
        if (express.Invoke()) throw new ArgumentException(message, paramName);
    }
}
