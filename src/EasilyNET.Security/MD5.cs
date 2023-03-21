using System.Text;

namespace EasilyNET.Security;

/// MD5本身不属于密码范畴,但是介于大部分人使用他作为一些加密的功能,所以也放进到这里.
/// <summary>
/// 针对MD5一些算法.
/// </summary>
public static class MD5
{
    /// <summary>
    /// 获取16位长度的MD5大写字符串
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string To16MD5(this string value) => value.To32MD5().Substring(8, 16);

    /// <summary>
    /// 获取32位长度的MD5大写字符串
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string To32MD5(this string value)
    {
#if NETSTANDARD
        using var md5 = System.Security.Cryptography.MD5.Create();
        var data = md5.ComputeHash(Encoding.UTF8.GetBytes(value));
#else
        var data = System.Security.Cryptography.MD5.HashData(Encoding.UTF8.GetBytes(value));
#endif
        var builder = new StringBuilder();
        // 循环遍历哈希数据的每一个字节并格式化为十六进制字符串 
        foreach (var t in data)
        {
            _ = builder.Append(t.ToString("X2"));
        }
        return builder.ToString();
    }
}