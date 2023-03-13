namespace EasilyNET.RabbitBus.Extensions;

/// <summary>
/// Object扩展,适用于大多数类型的扩展.
/// </summary>
internal static class ObjectExtension
{
    /// <summary>
    /// 验证指定值的断言<paramref name="assertion" />是否为真，如果不为真，抛出指定消息<paramref name="message" />的指定类型
    /// <typeparamref name="TException" />异常
    /// </summary>
    /// <typeparam name="TException">异常类型</typeparam>
    /// <param name="assertion">要验证的断言。</param>
    /// <param name="message">异常消息。</param>
    private static void Require<TException>(bool assertion, string message) where TException : Exception
    {
        if (assertion) return;
        if (string.IsNullOrWhiteSpace(message)) throw new ArgumentNullException(nameof(message));
        throw (TException)Activator.CreateInstance(typeof(TException), message)!;
    }

    /// <summary>
    /// 检查参数不能为空引用，否则抛出<see cref="ArgumentNullException" />异常。
    /// </summary>
    /// <param name="value"></param>
    /// <param name="paramName">参数名称</param>
    /// <exception cref="ArgumentNullException"></exception>
    internal static void NotNull<T>(this T value, string paramName)
    {
        Require<ArgumentNullException>(value is not null, $"参数“{paramName}”不能为空引用。");
    }
}