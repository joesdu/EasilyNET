using System.Text;

namespace EasilyNET.ExpressMapper;

/// <summary>
/// Provides helper methods for name manipulation.
/// 提供用于名称操作的辅助方法。
/// </summary>
public static class NameHelper
{
    /// <summary>
    /// Converts the first letter of the given string to uppercase.
    /// 将给定字符串的首字母转换为大写。
    /// </summary>
    /// <param name="name">
    /// The string to be converted.
    /// 要转换的字符串。
    /// </param>
    /// <returns>
    /// The string with the first letter in uppercase.
    /// 首字母大写的字符串。
    /// </returns>
    public static string ToUpperFirstLatter(string name)
    {
        var builder = new StringBuilder(name.Length);
        builder.Append(char.ToUpper(name[0]));
        builder.Append(name.AsSpan(1));
        return builder.ToString();
    }
}