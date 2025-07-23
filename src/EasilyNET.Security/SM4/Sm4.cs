// ReSharper disable SuggestBaseTypeForParameter

namespace EasilyNET.Security;

/// <summary>
/// SM4
/// </summary>
internal sealed class Sm4
{
    /// <summary>
    ///     <para xml:lang="en">Fixed parameter CK</para>
    ///     <para xml:lang="zh">固定参数CK</para>
    /// </summary>
    private readonly uint[] CK =
    [
        0x00070e15, 0x1c232a31, 0x383f464d, 0x545b6269,
        0x70777e85, 0x8c939aa1, 0xa8afb6bd, 0xc4cbd2d9,
        0xe0e7eef5, 0xfc030a11, 0x181f262d, 0x343b4249,
        0x50575e65, 0x6c737a81, 0x888f969d, 0xa4abb2b9,
        0xc0c7ced5, 0xdce3eaf1, 0xf8ff060d, 0x141b2229,
        0x30373e45, 0x4c535a61, 0x686f767d, 0x848b9299,
        0xa0a7aeb5, 0xbcc3cad1, 0xd8dfe6ed, 0xf4fb0209,
        0x10171e25, 0x2c333a41, 0x484f565d, 0x646b7279
    ];

    /// <summary>
    ///     <para xml:lang="en">System parameter FK</para>
    ///     <para xml:lang="zh">系统参数FK</para>
    /// </summary>
    private readonly uint[] FK = [0xa3b1bac6, 0x56aa3350, 0x677d9197, 0xb27022dc];

    /// <summary>
    ///     <para xml:lang="en">S-Box</para>
    ///     <para xml:lang="zh">S盒</para>
    /// </summary>
    private readonly byte[] SBoxTable =
    [
        /*      0     1     2     3     4     5     6     7     8     9     a     b     c     d     e     f*/
        /*0*/ 0xd6, 0x90, 0xe9, 0xfe, 0xcc, 0xe1, 0x3d, 0xb7, 0x16, 0xb6, 0x14, 0xc2, 0x28, 0xfb, 0x2c, 0x05,
        /*1*/ 0x2b, 0x67, 0x9a, 0x76, 0x2a, 0xbe, 0x04, 0xc3, 0xaa, 0x44, 0x13, 0x26, 0x49, 0x86, 0x06, 0x99,
        /*2*/ 0x9c, 0x42, 0x50, 0xf4, 0x91, 0xef, 0x98, 0x7a, 0x33, 0x54, 0x0b, 0x43, 0xed, 0xcf, 0xac, 0x62,
        /*3*/ 0xe4, 0xb3, 0x1c, 0xa9, 0xc9, 0x08, 0xe8, 0x95, 0x80, 0xdf, 0x94, 0xfa, 0x75, 0x8f, 0x3f, 0xa6,
        /*4*/ 0x47, 0x07, 0xa7, 0xfc, 0xf3, 0x73, 0x17, 0xba, 0x83, 0x59, 0x3c, 0x19, 0xe6, 0x85, 0x4f, 0xa8,
        /*5*/ 0x68, 0x6b, 0x81, 0xb2, 0x71, 0x64, 0xda, 0x8b, 0xf8, 0xeb, 0x0f, 0x4b, 0x70, 0x56, 0x9d, 0x35,
        /*6*/ 0x1e, 0x24, 0x0e, 0x5e, 0x63, 0x58, 0xd1, 0xa2, 0x25, 0x22, 0x7c, 0x3b, 0x01, 0x21, 0x78, 0x87,
        /*7*/ 0xd4, 0x00, 0x46, 0x57, 0x9f, 0xd3, 0x27, 0x52, 0x4c, 0x36, 0x02, 0xe7, 0xa0, 0xc4, 0xc8, 0x9e,
        /*8*/ 0xea, 0xbf, 0x8a, 0xd2, 0x40, 0xc7, 0x38, 0xb5, 0xa3, 0xf7, 0xf2, 0xce, 0xf9, 0x61, 0x15, 0xa1,
        /*9*/ 0xe0, 0xae, 0x5d, 0xa4, 0x9b, 0x34, 0x1a, 0x55, 0xad, 0x93, 0x32, 0x30, 0xf5, 0x8c, 0xb1, 0xe3,
        /*a*/ 0x1d, 0xf6, 0xe2, 0x2e, 0x82, 0x66, 0xca, 0x60, 0xc0, 0x29, 0x23, 0xab, 0x0d, 0x53, 0x4e, 0x6f,
        /*b*/ 0xd5, 0xdb, 0x37, 0x45, 0xde, 0xfd, 0x8e, 0x2f, 0x03, 0xff, 0x6a, 0x72, 0x6d, 0x6c, 0x5b, 0x51,
        /*c*/ 0x8d, 0x1b, 0xaf, 0x92, 0xbb, 0xdd, 0xbc, 0x7f, 0x11, 0xd9, 0x5c, 0x41, 0x1f, 0x10, 0x5a, 0xd8,
        /*d*/ 0x0a, 0xc1, 0x31, 0x88, 0xa5, 0xcd, 0x7b, 0xbd, 0x2d, 0x74, 0xd0, 0x12, 0xb8, 0xe5, 0xb4, 0xb0,
        /*e*/ 0x89, 0x69, 0x97, 0x4a, 0x0c, 0x96, 0x77, 0x7e, 0x65, 0xb9, 0xf1, 0x09, 0xc5, 0x6e, 0xc6, 0x84,
        /*f*/ 0x18, 0xf0, 0x7d, 0xec, 0x3a, 0xdc, 0x4d, 0x20, 0x79, 0xee, 0x5f, 0x3e, 0xd7, 0xcb, 0x39, 0x48
    ];

    /// <summary>
    ///     <para xml:lang="en">Encrypt nonlinear τ function B=τ(A)</para>
    ///     <para xml:lang="zh">加密 非线性τ函数B=τ(A)</para>
    /// </summary>
    /// <param name="b">
    ///     <para xml:lang="en">Byte array</para>
    ///     <para xml:lang="zh">字节数组</para>
    /// </param>
    /// <param name="i">
    ///     <para xml:lang="en">Index</para>
    ///     <para xml:lang="zh">索引</para>
    /// </param>
    private static long GetULongByBe(ReadOnlySpan<byte> b, int i) => ((long)(b[i] & 0xff) << 24) | (uint)((b[i + 1] & 0xff) << 16) | (uint)((b[i + 2] & 0xff) << 8) | b[i + 3] & 0xff & 0xffffffffL;

    /// <summary>
    ///     <para xml:lang="en">Decrypt nonlinear τ function B=τ(A)</para>
    ///     <para xml:lang="zh">解密 非线性τ函数B=τ(A)</para>
    /// </summary>
    /// <param name="n">
    ///     <para xml:lang="en">Long value</para>
    ///     <para xml:lang="zh">长整型值</para>
    /// </param>
    /// <param name="b">
    ///     <para xml:lang="en">Byte array</para>
    ///     <para xml:lang="zh">字节数组</para>
    /// </param>
    /// <param name="i">
    ///     <para xml:lang="en">Index</para>
    ///     <para xml:lang="zh">索引</para>
    /// </param>
    private static void PutULongToBe(long n, Span<byte> b, int i)
    {
        b[i] = (byte)(int)(0xFF & (n >> 24));
        b[i + 1] = (byte)(int)(0xFF & (n >> 16));
        b[i + 2] = (byte)(int)(0xFF & (n >> 8));
        b[i + 3] = (byte)(int)(0xFF & n);
    }

    private static long SHL(long x, int n) => (x & 0xFFFFFFFF) << n;

    /// <summary>
    ///     <para xml:lang="en">Circular shift, 32-bit x circularly left shift n bits</para>
    ///     <para xml:lang="zh">循环移位,为32位的x循环左移n位</para>
    /// </summary>
    /// <param name="x">
    ///     <para xml:lang="en">Long value</para>
    ///     <para xml:lang="zh">长整型值</para>
    /// </param>
    /// <param name="n">
    ///     <para xml:lang="en">Number of bits</para>
    ///     <para xml:lang="zh">位数</para>
    /// </param>
    private static long RotL(long x, int n) => SHL(x, n) | (x >> 32 - n);

    /// <summary>
    ///     <para xml:lang="en">Reverse the key</para>
    ///     <para xml:lang="zh">将密钥逆序</para>
    /// </summary>
    /// <param name="sk">
    ///     <para xml:lang="en">Key array</para>
    ///     <para xml:lang="zh">密钥数组</para>
    /// </param>
    /// <param name="i">
    ///     <para xml:lang="en">Index</para>
    ///     <para xml:lang="zh">索引</para>
    /// </param>
    private static void Swap(long[] sk, int i) => (sk[i], sk[31 - i]) = (sk[31 - i], sk[i]);

    /// <summary>
    ///     <para xml:lang="en">SM4 S-Box value</para>
    ///     <para xml:lang="zh">Sm4的S盒取值</para>
    /// </summary>
    /// <param name="inch">
    ///     <para xml:lang="en">Input byte</para>
    ///     <para xml:lang="zh">输入字节</para>
    /// </param>
    private byte SBox(byte inch) => SBoxTable[inch & 0xFF];

    /// <summary>
    ///     <para xml:lang="en">Linear transformation L</para>
    ///     <para xml:lang="zh">线性变换 L</para>
    /// </summary>
    /// <param name="ka">
    ///     <para xml:lang="en">Long value</para>
    ///     <para xml:lang="zh">长整型值</para>
    /// </param>
    private long Lt(long ka)
    {
        Span<byte> a = stackalloc byte[4];
        Span<byte> b = stackalloc byte[4];
        PutULongToBe(ka, a, 0);
        b[0] = SBox(a[0]);
        b[1] = SBox(a[1]);
        b[2] = SBox(a[2]);
        b[3] = SBox(a[3]);
        var bb = GetULongByBe(b, 0);
        return bb ^ RotL(bb, 2) ^ RotL(bb, 10) ^ RotL(bb, 18) ^ RotL(bb, 24);
    }

    /// <summary>
    ///     <para xml:lang="en">Round function F</para>
    ///     <para xml:lang="zh">轮函数 F</para>
    /// </summary>
    /// <param name="x0">
    ///     <para xml:lang="en">Long value x0</para>
    ///     <para xml:lang="zh">长整型值 x0</para>
    /// </param>
    /// <param name="x1">
    ///     <para xml:lang="en">Long value x1</para>
    ///     <para xml:lang="zh">长整型值 x1</para>
    /// </param>
    /// <param name="x2">
    ///     <para xml:lang="en">Long value x2</para>
    ///     <para xml:lang="zh">长整型值 x2</para>
    /// </param>
    /// <param name="x3">
    ///     <para xml:lang="en">Long value x3</para>
    ///     <para xml:lang="zh">长整型值 x3</para>
    /// </param>
    /// <param name="rk">
    ///     <para xml:lang="en">Round key</para>
    ///     <para xml:lang="zh">轮密钥</para>
    /// </param>
    private long F(long x0, long x1, long x2, long x3, long rk) => x0 ^ Lt(x1 ^ x2 ^ x3 ^ rk);

    /// <summary>
    ///     <para xml:lang="en">Round key rk</para>
    ///     <para xml:lang="zh">轮密钥rk</para>
    /// </summary>
    /// <param name="ka">
    ///     <para xml:lang="en">Long value</para>
    ///     <para xml:lang="zh">长整型值</para>
    /// </param>
    private long CalcRK(long ka)
    {
        Span<byte> a = stackalloc byte[4];
        Span<byte> b = stackalloc byte[4];
        PutULongToBe(ka, a, 0);
        b[0] = SBox(a[0]);
        b[1] = SBox(a[1]);
        b[2] = SBox(a[2]);
        b[3] = SBox(a[3]);
        var bb = GetULongByBe(b, 0);
        return bb ^ RotL(bb, 13) ^ RotL(bb, 23);
    }

    /// <summary>
    ///     <para xml:lang="en">Set encryption key</para>
    ///     <para xml:lang="zh">加密密钥</para>
    /// </summary>
    /// <param name="SK">
    ///     <para xml:lang="en">Key array</para>
    ///     <para xml:lang="zh">密钥数组</para>
    /// </param>
    /// <param name="key">
    ///     <para xml:lang="en">Key in byte array</para>
    ///     <para xml:lang="zh">字节数组格式的密钥</para>
    /// </param>
    private void SetKey(long[] SK, ReadOnlySpan<byte> key)
    {
        Span<long> MK = stackalloc long[4];
        Span<long> k = stackalloc long[36];
        var i = 0;
        MK[0] = GetULongByBe(key, 0);
        MK[1] = GetULongByBe(key, 4);
        MK[2] = GetULongByBe(key, 8);
        MK[3] = GetULongByBe(key, 12);
        k[0] = MK[0] ^ FK[0];
        k[1] = MK[1] ^ FK[1];
        k[2] = MK[2] ^ FK[2];
        k[3] = MK[3] ^ FK[3];
        for (; i < 32; i++)
        {
            k[i + 4] = k[i] ^ CalcRK(k[i + 1] ^ k[i + 2] ^ k[i + 3] ^ CK[i]);
            SK[i] = k[i + 4];
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Decryption function</para>
    ///     <para xml:lang="zh">解密函数</para>
    /// </summary>
    /// <param name="sk">
    ///     <para xml:lang="en">Round key</para>
    ///     <para xml:lang="zh">轮密钥</para>
    /// </param>
    /// <param name="input">
    ///     <para xml:lang="en">Ciphertext input block</para>
    ///     <para xml:lang="zh">输入分组的密文</para>
    /// </param>
    /// <param name="output">
    ///     <para xml:lang="en">Plaintext output block</para>
    ///     <para xml:lang="zh">输出的对应的分组明文</para>
    /// </param>
    private void OneRound(long[] sk, ReadOnlySpan<byte> input, Span<byte> output)
    {
        var i = 0;
        Span<long> ul_buf = stackalloc long[36];
        ul_buf[0] = GetULongByBe(input, 0);
        ul_buf[1] = GetULongByBe(input, 4);
        ul_buf[2] = GetULongByBe(input, 8);
        ul_buf[3] = GetULongByBe(input, 12);
        while (i < 32)
        {
            ul_buf[i + 4] = F(ul_buf[i], ul_buf[i + 1], ul_buf[i + 2], ul_buf[i + 3], sk[i]);
            i++;
        }
        PutULongToBe(ul_buf[35], output, 0);
        PutULongToBe(ul_buf[34], output, 4);
        PutULongToBe(ul_buf[33], output, 8);
        PutULongToBe(ul_buf[32], output, 12);
    }

    /// <summary>
    ///     <para xml:lang="en">Pad the hexadecimal string with 0 characters, return a hexadecimal string without 0x</para>
    ///     <para xml:lang="zh">补足 16 进制字符串的 0 字符，返回不带 0x 的16进制字符串</para>
    /// </summary>
    /// <param name="input">
    ///     <para xml:lang="en">Input byte array</para>
    ///     <para xml:lang="zh">输入字节数组</para>
    /// </param>
    /// <param name="mode">
    ///     <para xml:lang="en">1 for encryption, 0 for decryption</para>
    ///     <para xml:lang="zh">1表示加密，0表示解密</para>
    /// </param>
    private static byte[] Padding(ReadOnlySpan<byte> input, ESm4Model mode)
    {
        byte[] ret;
        if (mode is ESm4Model.Encrypt)
        {
            var p = 16 - input.Length % 16;
            ret = new byte[input.Length + p];
            input.CopyTo(ret);
            for (var i = 0; i < p; i++)
            {
                ret[input.Length + i] = (byte)p;
            }
        }
        else
        {
            int p = input[^1];
            ret = new byte[input.Length - p];
            input[..^p].CopyTo(ret);
        }
        return ret;
    }

    /// <summary>
    ///     <para xml:lang="en">Set encryption key</para>
    ///     <para xml:lang="zh">设置加密的key</para>
    /// </summary>
    /// <param name="ctx">
    ///     <para xml:lang="en">SM4 context</para>
    ///     <para xml:lang="zh">SM4上下文</para>
    /// </param>
    /// <param name="key">
    ///     <para xml:lang="en">Key in byte array</para>
    ///     <para xml:lang="zh">字节数组格式的密钥</para>
    /// </param>
    internal void SetKeyEnc(Sm4Context ctx, ReadOnlySpan<byte> key)
    {
        ctx.Mode = ESm4Model.Encrypt;
        SetKey(ctx.Key, key);
    }

    /// <summary>
    ///     <para xml:lang="en">Set decryption key</para>
    ///     <para xml:lang="zh">设置解密的key</para>
    /// </summary>
    /// <param name="ctx">
    ///     <para xml:lang="en">SM4 context</para>
    ///     <para xml:lang="zh">SM4上下文</para>
    /// </param>
    /// <param name="key">
    ///     <para xml:lang="en">Key in byte array</para>
    ///     <para xml:lang="zh">字节数组格式的密钥</para>
    /// </param>
    internal void SetKeyDec(Sm4Context ctx, ReadOnlySpan<byte> key)
    {
        ctx.Mode = ESm4Model.Decrypt;
        SetKey(ctx.Key, key);
        for (var i = 0; i < 16; i++)
        {
            Swap(ctx.Key, i);
        }
    }

    internal byte[] ECB(Sm4Context ctx, ReadOnlySpan<byte> input)
    {
        if (ctx is { IsPadding: true, Mode: ESm4Model.Encrypt })
        {
            input = Padding(input, ESm4Model.Encrypt);
        }
        var length = input.Length;
        var bins = new byte[length];
        input.CopyTo(bins);
        var bous = new byte[length];
        Span<byte> inBytes = stackalloc byte[16];
        Span<byte> outBytes = stackalloc byte[16];
        for (var i = 0; length > 0; length -= 16, i++)
        {
            input.Slice(i * 16, length > 16 ? 16 : length).CopyTo(inBytes);
            OneRound(ctx.Key, inBytes, outBytes);
            outBytes.CopyTo(bous.AsSpan(i * 16));
        }
        if (ctx is { IsPadding: true, Mode: ESm4Model.Decrypt })
        {
            bous = Padding(bous, ESm4Model.Decrypt);
        }
        return bous;
    }

    internal byte[] CBC(Sm4Context ctx, ReadOnlySpan<byte> iv, ReadOnlySpan<byte> input)
    {
        if (ctx is { IsPadding: true, Mode: ESm4Model.Encrypt })
        {
            input = Padding(input, ESm4Model.Encrypt);
        }
        var length = input.Length;
        var bins = new byte[length];
        input.CopyTo(bins);
        var bousList = new List<byte>();
        Span<byte> ivBytes = stackalloc byte[16];
        iv.CopyTo(ivBytes);
        Span<byte> inBytes = stackalloc byte[16];
        Span<byte> outBytes = stackalloc byte[16];
        Span<byte> out1 = stackalloc byte[16];
        int i;
        if (ctx.Mode is ESm4Model.Encrypt)
        {
            for (var j = 0; length > 0; length -= 16, j++)
            {
                input.Slice(j * 16, length > 16 ? 16 : length).CopyTo(inBytes);
                for (i = 0; i < 16; i++)
                {
                    outBytes[i] = (byte)(inBytes[i] ^ ivBytes[i]);
                }
                OneRound(ctx.Key, outBytes, out1);
                out1.CopyTo(ivBytes);
                for (var k = 0; k < 16; k++)
                {
                    bousList.Add(out1[k]);
                }
            }
        }
        else
        {
            Span<byte> temp = stackalloc byte[16];
            for (var j = 0; length > 0; length -= 16, j++)
            {
                input.Slice(j * 16, length > 16 ? 16 : length).CopyTo(inBytes);
                inBytes.CopyTo(temp);
                OneRound(ctx.Key, inBytes, outBytes);
                for (i = 0; i < 16; i++)
                {
                    out1[i] = (byte)(outBytes[i] ^ ivBytes[i]);
                }
                ivBytes.Clear(); // Clear the previous IV
                temp.CopyTo(ivBytes); // Update IV with the current block's ciphertext
                for (var k = 0; k < 16; k++)
                {
                    bousList.Add(out1[k]);
                }
            }
        }
        return ctx is { IsPadding: true, Mode: ESm4Model.Decrypt } ? Padding(bousList.ToArray(), ESm4Model.Decrypt) : [.. bousList];
    }
}