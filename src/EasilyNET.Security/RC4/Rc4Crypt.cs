// ReSharper disable SuggestBaseTypeForParameter
// ReSharper disable ParameterTypeCanBeEnumerable.Local
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.Security;

/// 参考: https://www.cnblogs.com/billyme/p/14772881.html
/// <summary>
/// RC4 加密解密
/// </summary>
public static class Rc4Crypt
{
    /// <summary>
    /// RC4解密
    /// </summary>
    /// <param name="data">待解密数据</param>
    /// <param name="key">密钥</param>
    /// <returns></returns>
    public static byte[] Decrypt(ReadOnlySpan<byte> data, ReadOnlySpan<byte> key) => Encrypt(data, key);

    /// <summary>
    /// RC4加密
    /// </summary>
    /// <param name="data">待加密数据</param>
    /// <param name="key">密钥</param>
    /// <returns></returns>
    public static byte[] Encrypt(ReadOnlySpan<byte> data, ReadOnlySpan<byte> key)
    {
        Span<byte> s = stackalloc byte[256];
        EncryptInit(key, s);
        var i = 0;
        var j = 0;
        var result = new byte[data.Length];
        for (var k = 0; k < data.Length; k++)
        {
            i = (i + 1) & 255;
            j = (j + s[i]) & 255;
            Swap(s, i, j);
            result[k] = (byte)(data[k] ^ s[(s[i] + s[j]) & 255]);
        }
        return result;
    }

    private static void EncryptInit(ReadOnlySpan<byte> key, Span<byte> s)
    {
        for (var i = 0; i < 256; i++)
        {
            s[i] = (byte)i;
        }
        for (int i = 0, j = 0; i < 256; i++)
        {
            j = (j + key[i % key.Length] + s[i]) & 255;
            Swap(s, i, j);
        }
    }

    private static void Swap(Span<byte> s, int i, int j) => (s[i], s[j]) = (s[j], s[i]);
}