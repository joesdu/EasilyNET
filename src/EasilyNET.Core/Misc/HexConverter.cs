using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace EasilyNET.Core.Misc;

/// <summary>
///     <para xml:lang="en">Hex converter</para>
///     <para xml:lang="zh">十六进制转换器</para>
/// </summary>
public static class HexConverter
{
    // Lookup table for hex values. 0xFF means invalid.
    private static readonly byte[] CharToHexLookup = new byte[128];

    static HexConverter()
    {
        // Initialize lookup table
        CharToHexLookup.AsSpan().Fill(0xFF);
        for (var i = '0'; i <= '9'; i++)
            CharToHexLookup[i] = (byte)(i - '0');
        for (var i = 'a'; i <= 'f'; i++)
            CharToHexLookup[i] = (byte)((i - 'a') + 10);
        for (var i = 'A'; i <= 'F'; i++)
            CharToHexLookup[i] = (byte)((i - 'A') + 10);
    }

    /// <summary>
    ///     <para xml:lang="en">Converts the span of hexadecimal characters to a span of bytes.</para>
    ///     <para xml:lang="zh">将十六进制字符的跨度转换为字节的跨度。</para>
    /// </summary>
    /// <param name="chars">
    ///     <para xml:lang="en">The span containing the hexadecimal characters to convert.</para>
    ///     <para xml:lang="zh">包含要转换的十六进制字符的跨度。</para>
    /// </param>
    /// <param name="bytes">
    ///     <para xml:lang="en">The span to write the converted bytes to.</para>
    ///     <para xml:lang="zh">要写入转换字节的跨度。</para>
    /// </param>
    /// <param name="charsProcessed">
    ///     <para xml:lang="en">When this method returns, contains the number of characters that were processed.</para>
    ///     <para xml:lang="zh">当此方法返回时，包含已处理的字符数。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">true if the conversion was successful; otherwise, false.</para>
    ///     <para xml:lang="zh">如果转换成功，则为 true；否则为 false。</para>
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryFromHexString(ReadOnlySpan<char> chars, Span<byte> bytes, out int charsProcessed)
    {
        charsProcessed = 0;
        if (chars.Length == 0)
        {
            return true;
        }
        if (chars.Length % 2 != 0 || bytes.Length < chars.Length / 2)
        {
            return false;
        }
        ref var charRef = ref MemoryMarshal.GetReference(chars);
        ref var byteRef = ref MemoryMarshal.GetReference(bytes);
        ref var lookupRef = ref MemoryMarshal.GetReference(CharToHexLookup.AsSpan());
        var length = chars.Length;
        var i = 0;
        var j = 0;

        // Unroll loop for better performance on small to medium strings
        while (i < length)
        {
            var c1 = Unsafe.Add(ref charRef, i);
            var c2 = Unsafe.Add(ref charRef, i + 1);

            // Check bounds for lookup table (char is ushort, so it can be > 127)
            // Standard ASCII hex chars are within 0-127
            if (c1 > 127 || c2 > 127)
            {
                return false;
            }
            var val1 = Unsafe.Add(ref lookupRef, c1);
            var val2 = Unsafe.Add(ref lookupRef, c2);
            if (val1 == 0xFF || val2 == 0xFF)
            {
                return false;
            }
            Unsafe.Add(ref byteRef, j) = (byte)((val1 << 4) | val2);
            i += 2;
            j++;
        }
        charsProcessed = length;
        return true;
    }
}