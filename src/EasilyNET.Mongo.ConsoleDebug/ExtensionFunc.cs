namespace EasilyNET.Mongo.ConsoleDebug;

/// <summary>
/// 内部的一些扩展方法
/// </summary>
// ReSharper disable once UnusedType.Global
internal static class ExtensionFunc
{
    /// <summary>
    /// 截断一个字符串,并在末尾添加一个后缀
    /// </summary>
    /// <param name="value">原始字符串</param>
    /// <param name="maxLength">最大长度(添加后缀后的长度)</param>
    /// <param name="suffix">后缀,默认: ...</param>
    /// <returns></returns>
    internal static string Truncate(this string value, int maxLength, string suffix = "...")
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value;
#if NETSTANDARD2_0
        return value.Substring(0, maxLength) + suffix;
#else
        return value[..maxLength] + suffix;
#endif
    }
}