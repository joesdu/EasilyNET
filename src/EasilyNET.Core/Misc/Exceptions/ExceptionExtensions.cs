// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.Core.Misc.Exceptions;

/// <summary>
///     <para xml:lang="en">Exception extensions</para>
///     <para xml:lang="zh">异常扩展</para>
/// </summary>
public static class ExceptionExtensions
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
    /// <exception cref="ArgumentException">
    ///     <para xml:lang="en">Thrown if the condition is met</para>
    ///     <para xml:lang="zh">如果条件成立则抛出此异常</para>
    /// </exception>
    public static void ThrowIf(Func<bool> express, string? message)
    {
        if (express.Invoke())
        {
            throw new(message);
        }
    }
}