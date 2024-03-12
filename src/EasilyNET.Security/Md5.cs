using System.Security.Cryptography;
using System.Text;

namespace EasilyNET.Security;

internal static class Md5
{
    /// <summary>
    /// 获取16位长度的MD5大写字符串
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    internal static string To16MD5(this string value) => value.To32MD5().Substring(8, 16);

    /// <summary>
    /// 获取32位长度的MD5大写字符串
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    internal static string To32MD5(this string value)
    {
        var data = MD5.HashData(Encoding.UTF8.GetBytes(value));
        var builder = new StringBuilder();
        // 循环遍历哈希数据的每一个字节并格式化为十六进制字符串 
        foreach (var t in data)
        {
            builder.Append(t.ToString("X2"));
        }
        return builder.ToString();
    }
}
