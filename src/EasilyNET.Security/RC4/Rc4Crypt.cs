﻿using EasilyNET.Core.Misc;
using System.Text;

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
    /// RC4加密
    /// </summary>
    /// <param name="key"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    public static string Encrypt(string data, string key)
    {
        var encrypted = Encrypt(Encoding.UTF8.GetBytes(data), Encoding.UTF8.GetBytes(key));
        return encrypted.ToBase64();
    }

    /// <summary>
    /// RC4解密
    /// </summary>
    /// <param name="key"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    public static string Decrypt(string data, string key)
    {
        var decrypted = Encrypt(data.FromBase64(), Encoding.UTF8.GetBytes(key));
        return Encoding.UTF8.GetString(decrypted);
    }

    private static byte[] EncryptInit(byte[] key)
    {
        var s = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();
        for (int i = 0, j = 0; i < 256; i++)
        {
            j = (j + key[i % key.Length] + s[i]) & 255;
            Swap(s, i, j);
        }
        return s;
    }

    private static byte[] Encrypt(byte[] data, byte[] key)
    {
        var s = EncryptInit(key);
        var i = 0;
        var j = 0;
        return data.Select(b =>
        {
            i = (i + 1) & 255;
            j = (j + s[i]) & 255;
            Swap(s, i, j);
            return (byte)(b ^ s[(s[i] + s[j]) & 255]);
        }).ToArray();
    }

    private static void Swap(IList<byte> s, int i, int j) => (s[i], s[j]) = (s[j], s[i]);
}