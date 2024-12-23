using System.Runtime.CompilerServices;

namespace EasilyNET.Core.Misc.Exceptions;

/// <summary>
///     <para xml:lang="en">Some extensions for exception handling</para>
///     <para xml:lang="zh">异常处理的一些扩展</para>
/// </summary>
public static class ArgumentExceptionExtensions
{
    /// <summary>
    ///     <para xml:lang="en">Throws an exception if the condition is met, otherwise passes</para>
    ///     <para xml:lang="zh">若是条件成立抛出异常,否则通过</para>
    /// </summary>
    /// <param name="express">
    ///     <para xml:lang="en">Condition expression</para>
    ///     <para xml:lang="zh">条件表达式</para>
    /// </param>
    /// <param name="message">
    ///     <para xml:lang="en">Message</para>
    ///     <para xml:lang="zh">信息</para>
    /// </param>
    /// <param name="paramName">
    ///     <para xml:lang="en">Parameter name</para>
    ///     <para xml:lang="zh">参数名称</para>
    /// </param>
    /// <exception cref="ArgumentException">
    ///     <para xml:lang="en">Thrown if the condition is met</para>
    ///     <para xml:lang="zh">如果条件成立则抛出此异常</para>
    /// </exception>
    public static void ThrowIf(Func<bool> express, string? message, [CallerArgumentExpression(nameof(express))] string? paramName = null)
    {
        if (express.Invoke()) throw new ArgumentException(message, paramName);
    }
}