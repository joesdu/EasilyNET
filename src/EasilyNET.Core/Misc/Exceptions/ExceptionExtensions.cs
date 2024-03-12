namespace EasilyNET.Core.Misc.Exceptions;

/// <summary>
/// 异常扩展
/// </summary>
public static class ExceptionExtensions
{
    /// <summary>
    /// 若是条件成立抛出异常,否则通过
    /// </summary>
    /// <param name="express">条件表达式</param>
    /// <param name="message">信息</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static void ThrowIf(Func<bool> express, string? message)
    {
        if (express.Invoke()) throw new(message);
    }
}
