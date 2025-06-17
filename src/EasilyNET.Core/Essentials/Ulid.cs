using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;

#pragma warning disable IDE0046

// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace EasilyNET.Core.Essentials;

/// <summary>
///     <para xml:lang="en">Represents a Universally Unique Lexicographically Sortable Identifier (ULID).</para>
///     <para xml:lang="zh">表示一个通用唯一且可字典序排序的标识符（ULID，通用唯一有序标识符）。</para>
///     <para>Spec: https://github.com/ulid/spec</para>
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 16)]
public readonly struct Ulid : IEquatable<Ulid>, ISpanFormattable, ISpanParsable<Ulid>, IUtf8SpanFormattable
{
    // https://en.wikipedia.org/wiki/Base32
    private static readonly char[] Base32Text = "0123456789ABCDEFGHJKMNPQRSTVWXYZ".ToCharArray();
    private static readonly byte[] Base32Bytes = Encoding.UTF8.GetBytes(Base32Text);

    private static readonly byte[] CharToBase32 =
    [
        255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
        255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
        255, 255, 255, 255, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 255, 255, 255, 255, 255, 255, 255, 10, 11, 12, 13, 14, 15,
        16, 17, 255, 18, 19, 255, 20, 21, 255, 22, 23, 24, 25, 26, 255, 27, 28, 29, 30, 31, 255, 255, 255, 255, 255,
        255, 10, 11, 12, 13, 14, 15, 16, 17, 255, 18, 19, 255, 20, 21, 255, 22, 23, 24, 25, 26, 255, 27, 28, 29, 30, 31
    ];

    private static readonly DateTimeOffset UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    ///     <para xml:lang="en">MinValue</para>
    ///     <para xml:lang="zh">最小值</para>
    /// </summary>
    public static readonly Ulid MinValue = new(UnixEpoch.ToUnixTimeMilliseconds(), "\0\0\0\0\0\0\0\0\0\0"u8);

    /// <summary>
    ///     <para xml:lang="en">MaxValue</para>
    ///     <para xml:lang="zh">最大值</para>
    /// </summary>
    public static readonly Ulid MaxValue = new(DateTimeOffset.MaxValue.ToUnixTimeMilliseconds(), [255, 255, 255, 255, 255, 255, 255, 255, 255, 255]);

    /// <summary>
    /// Represents an empty universally unique lexicographically sortable identifier (ULID).
    /// </summary>
    /// <remarks>
    /// This field is a read-only instance of <see cref="Ulid" /> where all bytes are set to zero. It
    /// can be used as a default or uninitialized value for ULID comparisons or assignments.
    /// </remarks>
    public static readonly Ulid Empty = new();

    // Core

    // Timestamp(48bits)
    [FieldOffset(0)]
    private readonly byte timestamp0;

    [FieldOffset(1)]
    private readonly byte timestamp1;

    [FieldOffset(2)]
    private readonly byte timestamp2;

    [FieldOffset(3)]
    private readonly byte timestamp3;

    [FieldOffset(4)]
    private readonly byte timestamp4;

    [FieldOffset(5)]
    private readonly byte timestamp5;

    // Randomness(80bits)
    [FieldOffset(6)]
    private readonly byte randomness0;

    [FieldOffset(7)]
    private readonly byte randomness1;

    [FieldOffset(8)]
    private readonly byte randomness2;

    [FieldOffset(9)]
    private readonly byte randomness3;

    [FieldOffset(10)]
    private readonly byte randomness4;

    [FieldOffset(11)]
    private readonly byte randomness5;

    [FieldOffset(12)]
    private readonly byte randomness6;

    [FieldOffset(13)]
    private readonly byte randomness7;

    [FieldOffset(14)]
    private readonly byte randomness8;

    [FieldOffset(15)]
    private readonly byte randomness9;

    /// <summary>
    ///     <para xml:lang="en">Randomness bytes of the ULID.</para>
    ///     <para xml:lang="zh">ULID 的随机部分字节数组。</para>
    /// </summary>
    [IgnoreDataMember]
    public byte[] Random =>
    [
        randomness0,
        randomness1,
        randomness2,
        randomness3,
        randomness4,
        randomness5,
        randomness6,
        randomness7,
        randomness8,
        randomness9
    ];

    /// <summary>
    ///     <para xml:lang="en">Timestamp of the ULID in milliseconds since Unix epoch (1970-01-01T00:00:00Z).</para>
    ///     <para xml:lang="zh">ULID 的时间戳（自 Unix 纪元以来的毫秒数）。</para>
    /// </summary>
    [IgnoreDataMember]
    public DateTimeOffset Time
    {
        get
        {
            if (BitConverter.IsLittleEndian)
            {
                // |A|B|C|D|E|F|... -> |F|E|D|C|B|A|0|0|

                // Lower |A|B|C|D| -> |D|C|B|A|
                // Upper |E|F| -> |F|E|
                // Time  |F|E| + |0|0|D|C|B|A|
                var lower = Unsafe.ReadUnaligned<uint>(ref Unsafe.AsRef(in timestamp0));
                var upper = Unsafe.ReadUnaligned<ushort>(ref Unsafe.AsRef(in timestamp4));
                var time = BinaryPrimitives.ReverseEndianness(upper) + ((long)BinaryPrimitives.ReverseEndianness(lower) << 16);
                return DateTimeOffset.FromUnixTimeMilliseconds(time);
            }
            else
            {
                // |A|B|C|D|E|F|... -> |0|0|A|B|C|D|E|F|

                // Upper |A|B|C|D|
                // Lower |E|F|
                // Time  |A|B|C|C|0|0| + |E|F|
                var upper = Unsafe.ReadUnaligned<uint>(ref Unsafe.AsRef(in timestamp0));
                var lower = Unsafe.ReadUnaligned<ushort>(ref Unsafe.AsRef(in timestamp4));
                var time = ((long)upper << 16) + lower;
                return DateTimeOffset.FromUnixTimeMilliseconds(time);
            }
        }
    }

    internal Ulid(long timestampMilliseconds, XorShift64 random) : this()
    {
        unsafe
        {
            ref var firstByte = ref Unsafe.AsRef<byte>(Unsafe.AsPointer(ref timestampMilliseconds));
            if (BitConverter.IsLittleEndian)
            {
                // Get memory in stack and copy to ulid(Little->Big reverse order).
                timestamp0 = Unsafe.Add(ref firstByte, 5);
                timestamp1 = Unsafe.Add(ref firstByte, 4);
                timestamp2 = Unsafe.Add(ref firstByte, 3);
                timestamp3 = Unsafe.Add(ref firstByte, 2);
                timestamp4 = Unsafe.Add(ref firstByte, 1);
                timestamp5 = Unsafe.Add(ref firstByte, 0);
            }
            else
            {
                timestamp0 = Unsafe.Add(ref firstByte, 2);
                timestamp1 = Unsafe.Add(ref firstByte, 3);
                timestamp2 = Unsafe.Add(ref firstByte, 4);
                timestamp3 = Unsafe.Add(ref firstByte, 5);
                timestamp4 = Unsafe.Add(ref firstByte, 6);
                timestamp5 = Unsafe.Add(ref firstByte, 7);
            }
        }

        // Get first byte of randomness from Ulid Struct.
        Unsafe.WriteUnaligned(ref randomness0, random.Next()); // randomness0~7(but use 0~1 only)
        Unsafe.WriteUnaligned(ref randomness2, random.Next()); // randomness2~9
    }

    internal Ulid(long timestampMilliseconds, ReadOnlySpan<byte> randomness) : this()
    {
        unsafe
        {
            ref var firstByte = ref Unsafe.AsRef<byte>(Unsafe.AsPointer(ref timestampMilliseconds));
            if (BitConverter.IsLittleEndian)
            {
                // Get memory in stack and copy to ulid(Little->Big reverse order).
                timestamp0 = Unsafe.Add(ref firstByte, 5);
                timestamp1 = Unsafe.Add(ref firstByte, 4);
                timestamp2 = Unsafe.Add(ref firstByte, 3);
                timestamp3 = Unsafe.Add(ref firstByte, 2);
                timestamp4 = Unsafe.Add(ref firstByte, 1);
                timestamp5 = Unsafe.Add(ref firstByte, 0);
            }
            else
            {
                timestamp0 = Unsafe.Add(ref firstByte, 2);
                timestamp1 = Unsafe.Add(ref firstByte, 3);
                timestamp2 = Unsafe.Add(ref firstByte, 4);
                timestamp3 = Unsafe.Add(ref firstByte, 5);
                timestamp4 = Unsafe.Add(ref firstByte, 6);
                timestamp5 = Unsafe.Add(ref firstByte, 7);
            }
        }
        ref var src = ref MemoryMarshal.GetReference(randomness); // length = 10
        randomness0 = randomness[0];
        randomness1 = randomness[1];
        Unsafe.WriteUnaligned(ref randomness2, Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref src, 2))); // randomness2~randomness9
    }

    /// <summary>
    ///     <para xml:lang="en">Initializes a new instance of the <see cref="Ulid" /> struct using the specified 16-byte span.</para>
    ///     <para xml:lang="zh">使用指定 16 字节数组初始化 ULID。前 8 字节为时间戳，后 8 字节为随机数。</para>
    /// </summary>
    /// <param name="bytes">
    ///     <para xml:lang="en">
    ///     A read-only span of 16 bytes representing the ULID value. The first 8 bytes represent the timestamp, and the remaining 8
    ///     bytes represent the randomness.
    ///     </para>
    ///     <para xml:lang="zh">16 字节的只读字节数组，前 8 字节为时间戳，后 8 字节为随机数。</para>
    /// </param>
    /// <exception cref="ArgumentException">
    ///     <para xml:lang="en">Thrown if the length of <paramref name="bytes" /> is not 16.</para>
    ///     <para xml:lang="zh">当 <paramref name="bytes" /> 长度不是 16 时抛出。</para>
    /// </exception>
    public Ulid(ReadOnlySpan<byte> bytes) : this()
    {
        if (bytes.Length != 16)
        {
            throw new ArgumentException("invalid bytes length, length:" + bytes.Length);
        }
        this = MemoryMarshal.Read<Ulid>(bytes);
    }

    internal Ulid(ReadOnlySpan<char> base32)
    {
        // unroll-code is based on NUlid.
        randomness9 = (byte)((CharToBase32[base32[24]] << 5) | CharToBase32[base32[25]]); // eliminate bounds-check of span
        timestamp0 = (byte)((CharToBase32[base32[0]] << 5) | CharToBase32[base32[1]]);
        timestamp1 = (byte)((CharToBase32[base32[2]] << 3) | (CharToBase32[base32[3]] >> 2));
        timestamp2 = (byte)((CharToBase32[base32[3]] << 6) | (CharToBase32[base32[4]] << 1) | (CharToBase32[base32[5]] >> 4));
        timestamp3 = (byte)((CharToBase32[base32[5]] << 4) | (CharToBase32[base32[6]] >> 1));
        timestamp4 = (byte)((CharToBase32[base32[6]] << 7) | (CharToBase32[base32[7]] << 2) | (CharToBase32[base32[8]] >> 3));
        timestamp5 = (byte)((CharToBase32[base32[8]] << 5) | CharToBase32[base32[9]]);
        randomness0 = (byte)((CharToBase32[base32[10]] << 3) | (CharToBase32[base32[11]] >> 2));
        randomness1 = (byte)((CharToBase32[base32[11]] << 6) | (CharToBase32[base32[12]] << 1) | (CharToBase32[base32[13]] >> 4));
        randomness2 = (byte)((CharToBase32[base32[13]] << 4) | (CharToBase32[base32[14]] >> 1));
        randomness3 = (byte)((CharToBase32[base32[14]] << 7) | (CharToBase32[base32[15]] << 2) | (CharToBase32[base32[16]] >> 3));
        randomness4 = (byte)((CharToBase32[base32[16]] << 5) | CharToBase32[base32[17]]);
        randomness5 = (byte)((CharToBase32[base32[18]] << 3) | (CharToBase32[base32[19]] >> 2));
        randomness6 = (byte)((CharToBase32[base32[19]] << 6) | (CharToBase32[base32[20]] << 1) | (CharToBase32[base32[21]] >> 4));
        randomness7 = (byte)((CharToBase32[base32[21]] << 4) | (CharToBase32[base32[22]] >> 1));
        randomness8 = (byte)((CharToBase32[base32[22]] << 7) | (CharToBase32[base32[23]] << 2) | (CharToBase32[base32[24]] >> 3));
    }

    /// <summary>
    ///     <para xml:lang="en">Initializes a new instance of the <see cref="Ulid" /> struct using the specified <see cref="Guid" />.</para>
    ///     <para xml:lang="zh">使用指定 Guid 初始化 ULID。</para>
    /// </summary>
    /// <param name="guid">
    ///     <para xml:lang="en">The <see cref="Guid" /> to convert into a <see cref="Ulid" />.</para>
    ///     <para xml:lang="zh">要转换为 ULID 的 Guid。</para>
    /// </param>
    // HACK: We assume the layout of a Guid is the following:
    // Int32, Int16, Int16, Int8, Int8, Int8, Int8, Int8, Int8, Int8, Int8
    // source: https://github.com/dotnet/runtime/blob/4f9ae42d861fcb4be2fcd5d3d55d5f227d30e723/src/libraries/System.Private.CoreLib/src/System/Guid.cs
    public Ulid(Guid guid)
    {
        if (IsVector128Supported && BitConverter.IsLittleEndian)
        {
            var vector = Unsafe.As<Guid, Vector128<byte>>(ref guid);
            var shuffled = Shuffle(vector, Vector128.Create((byte)3, 2, 1, 0, 5, 4, 7, 6, 8, 9, 10, 11, 12, 13, 14, 15));
            this = Unsafe.As<Vector128<byte>, Ulid>(ref shuffled);
            return;
        }
        Span<byte> buf = stackalloc byte[16];
        if (BitConverter.IsLittleEndian)
        {
            // |A|B|C|D|E|F|G|H|I|J|K|L|M|N|O|P|
            // |D|C|B|A|...
            //      ...|F|E|H|G|...
            //              ...|I|J|K|L|M|N|O|P|
            ref var ptr = ref Unsafe.As<Guid, uint>(ref guid);
            var lower = BinaryPrimitives.ReverseEndianness(ptr);
            MemoryMarshal.Write(buf, in lower);
            ptr = ref Unsafe.Add(ref ptr, 1);
            var upper = ((ptr & 0x00_FF_00_FF) << 8) | ((ptr & 0xFF_00_FF_00) >> 8);
            MemoryMarshal.Write(buf[4..], in upper);
            ref var upperBytes = ref Unsafe.As<uint, ulong>(ref Unsafe.Add(ref ptr, 1));
            MemoryMarshal.Write(buf[8..], in upperBytes);
        }
        else
        {
            MemoryMarshal.Write(buf, in guid);
        }
        this = MemoryMarshal.Read<Ulid>(buf);
    }

    // Factory

    /// <summary>
    ///     <para xml:lang="en">Creates a new ULID with the current timestamp and random bytes.</para>
    ///     <para xml:lang="zh">生成当前时间戳和随机数的 ULID。</para>
    /// </summary>
    /// <returns>
    ///     <para xml:lang="en">A new <see cref="Ulid" /> instance.</para>
    ///     <para xml:lang="zh">新的 <see cref="Ulid" /> 实例。</para>
    /// </returns>
    public static Ulid NewUlid() => new(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), RandomProvider.GetXorShift64());

    /// <summary>
    ///     <para xml:lang="en">Creates a new <see cref="Ulid" /> instance using the specified timestamp and a randomly generated identifier.</para>
    ///     <para xml:lang="zh">使用指定时间戳生成 ULID。</para>
    /// </summary>
    /// <param name="timestamp">
    ///     <para xml:lang="en">The timestamp to use for the <see cref="Ulid" />. This value represents the number of milliseconds since the Unix epoch.</para>
    ///     <para xml:lang="zh">ULID 的时间戳，表示自 Unix 纪元以来的毫秒数。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">A new <see cref="Ulid" /> instance that incorporates the specified timestamp and a random component.</para>
    ///     <para xml:lang="zh">包含指定时间戳和随机部分的新 <see cref="Ulid" /> 实例。</para>
    /// </returns>
    public static Ulid NewUlid(DateTimeOffset timestamp) => new(timestamp.ToUnixTimeMilliseconds(), RandomProvider.GetXorShift64());

    /// <summary>
    ///     <para xml:lang="en">Creates a new <see cref="Ulid" /> instance using the specified timestamp and randomness.</para>
    ///     <para xml:lang="zh">使用指定时间戳和随机字节生成 ULID。</para>
    /// </summary>
    /// <param name="timestamp">
    ///     <para xml:lang="en">The timestamp to use for the ULID, represented as a <see cref="DateTimeOffset" />.</para>
    ///     <para xml:lang="zh">ULID 的时间戳，<see cref="DateTimeOffset" /> 类型。</para>
    /// </param>
    /// <param name="randomness">
    ///     <para xml:lang="en">A 10-byte span representing the randomness component of the ULID. The length of the span must be exactly 10 bytes.</para>
    ///     <para xml:lang="zh">10 字节的随机部分，长度必须为 10。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">A new <see cref="Ulid" /> instance constructed with the specified timestamp and randomness.</para>
    ///     <para xml:lang="zh">包含指定时间戳和随机部分的新 <see cref="Ulid" /> 实例。</para>
    /// </returns>
    /// <exception cref="ArgumentException">
    ///     <para xml:lang="en">Thrown if the <paramref name="randomness" /> span does not have a length of 10 bytes.</para>
    ///     <para xml:lang="zh">当 <paramref name="randomness" /> 长度不是 10 时抛出。</para>
    /// </exception>
    public static Ulid NewUlid(DateTimeOffset timestamp, ReadOnlySpan<byte> randomness) => randomness.Length != 10 ? throw new ArgumentException("invalid randomness length, length:" + randomness.Length) : new(timestamp.ToUnixTimeMilliseconds(), randomness);

    /// <summary>
    ///     <para xml:lang="en">
    ///     Parses the specified Base32-encoded string representation of a ULID and returns the corresponding <see cref="Ulid" />
    ///     instance.
    ///     </para>
    ///     <para xml:lang="zh">解析 Base32 编码字符串为 ULID。</para>
    /// </summary>
    /// <param name="base32">
    ///     <para xml:lang="en">The Base32-encoded string to parse. Must be a valid ULID representation.</para>
    ///     <para xml:lang="zh">要解析的 Base32 编码字符串，必须是有效的 ULID 表示。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">An <see cref="Ulid" /> instance that represents the parsed ULID.</para>
    ///     <para xml:lang="zh">解析得到的 <see cref="Ulid" /> 实例。</para>
    /// </returns>
    public static Ulid Parse(string base32) => Parse(base32.AsSpan());

    /// <summary>
    ///     <para xml:lang="en">Parses a 26-character Base32-encoded string representation of a ULID into an <see cref="Ulid" /> instance.</para>
    ///     <para xml:lang="zh">解析 26 字符 Base32 编码字符串为 ULID。</para>
    /// </summary>
    /// <param name="base32">
    ///     <para xml:lang="en">A read-only span of characters containing the 26-character Base32-encoded ULID string.</para>
    ///     <para xml:lang="zh">26 字符的 Base32 编码字符串。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">An <see cref="Ulid" /> instance that represents the parsed ULID.</para>
    ///     <para xml:lang="zh">解析得到的 <see cref="Ulid" /> 实例。</para>
    /// </returns>
    /// <exception cref="ArgumentException">
    ///     <para xml:lang="en">Thrown if <paramref name="base32" /> does not have a length of 26 characters.</para>
    ///     <para xml:lang="zh">当 <paramref name="base32" /> 长度不是 26 时抛出。</para>
    /// </exception>
    public static Ulid Parse(ReadOnlySpan<char> base32) => base32.Length != 26 ? throw new ArgumentException("invalid base32 length, length:" + base32.Length) : new(base32);

    /// <summary>
    ///     <para xml:lang="en">Parses a ULID from a Base32-encoded byte span.</para>
    ///     <para xml:lang="zh">从 Base32 字节数组解析 ULID。</para>
    /// </summary>
    /// <param name="base32">
    ///     <para xml:lang="en">The Base32-encoded byte span representing the ULID to parse.</para>
    ///     <para xml:lang="zh">Base32 编码的字节数组。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The parsed <see cref="Ulid" /> instance.</para>
    ///     <para xml:lang="zh">解析得到的 <see cref="Ulid" /> 实例。</para>
    /// </returns>
    /// <exception cref="ArgumentException">
    ///     <para xml:lang="en">Thrown if the <paramref name="base32" /> span has an invalid length or cannot be parsed as a valid ULID.</para>
    ///     <para xml:lang="zh">当 <paramref name="base32" /> 长度无效或无法解析为有效 ULID 时抛出。</para>
    /// </exception>
    public static Ulid Parse(ReadOnlySpan<byte> base32) => !TryParse(base32, out var ulid) ? throw new ArgumentException("invalid base32 length, length:" + base32.Length) : ulid;

    /// <summary>
    ///     <para xml:lang="en">Attempts to parse the specified Base32-encoded string into a <see cref="Ulid" />.</para>
    ///     <para xml:lang="zh">尝试解析 Base32 编码字符串为 ULID。</para>
    /// </summary>
    /// <param name="base32">
    ///     <para xml:lang="en">The Base32-encoded string to parse. This value can be null.</para>
    ///     <para xml:lang="zh">要解析的 Base32 编码字符串，可以为 null。</para>
    /// </param>
    /// <param name="ulid">
    ///     <para xml:lang="en">
    ///     When this method returns, contains the parsed <see cref="Ulid" /> if the parsing succeeded, or the default value of
    ///     <see cref="Ulid" /> if the parsing failed.
    ///     </para>
    ///     <para xml:lang="zh">方法返回时，若解析成功则为解析得到的 <see cref="Ulid" />，否则为默认值。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en"><see langword="true" /> if the parsing was successful; otherwise, <see langword="false" />.</para>
    ///     <para xml:lang="zh">如果解析成功返回 true，否则返回 false。</para>
    /// </returns>
    public static bool TryParse(string? base32, out Ulid ulid) => TryParse(base32.AsSpan(), out ulid);

    /// <summary>
    ///     <para xml:lang="en">Attempts to parse a Base32-encoded string representation of a ULID.</para>
    ///     <para xml:lang="zh">尝试解析 Base32 编码字符串为 ULID。</para>
    /// </summary>
    /// <param name="base32">
    ///     <para xml:lang="en">
    ///     The Base32-encoded string to parse, represented as a <see cref="ReadOnlySpan{T}" /> of characters. Must be exactly 26
    ///     characters long.
    ///     </para>
    ///     <para xml:lang="zh">要解析的 Base32 编码字符串，<see cref="ReadOnlySpan{T}" /> 类型，长度必须为 26。</para>
    /// </param>
    /// <param name="ulid">
    ///     <para xml:lang="en">
    ///     When this method returns, contains the parsed <see cref="Ulid" /> if the parsing succeeded, or the default value of
    ///     <see cref="Ulid" /> if the parsing failed.
    ///     </para>
    ///     <para xml:lang="zh">方法返回时，若解析成功则为解析得到的 <see cref="Ulid" />，否则为默认值。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en"><see langword="true" /> if the parsing was successful; otherwise, <see langword="false" />.</para>
    ///     <para xml:lang="zh">如果解析成功返回 true，否则返回 false。</para>
    /// </returns>
    public static bool TryParse(ReadOnlySpan<char> base32, out Ulid ulid)
    {
        if (base32.Length != 26)
        {
            ulid = default;
            return false;
        }
        try
        {
            ulid = new(base32);
            return true;
        }
        catch
        {
            ulid = default;
            return false;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Attempts to parse a Base32-encoded <see cref="Ulid" /> from the specified byte span.</para>
    ///     <para xml:lang="zh">尝试从 Base32 字节数组解析 ULID。</para>
    /// </summary>
    /// <param name="base32">
    ///     <para xml:lang="en">A read-only span of bytes representing the Base32-encoded <see cref="Ulid" />. Must be exactly 26 bytes in length.</para>
    ///     <para xml:lang="zh">Base32 编码的只读字节数组，长度必须为 26。</para>
    /// </param>
    /// <param name="ulid">
    ///     <para xml:lang="en">
    ///     When this method returns, contains the parsed <see cref="Ulid" /> if the operation succeeded, or the default value of
    ///     <see cref="Ulid" /> if the operation failed.
    ///     </para>
    ///     <para xml:lang="zh">方法返回时，若解析成功则为解析得到的 <see cref="Ulid" />，否则为默认值。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en"><see langword="true" /> if the parsing operation succeeded; otherwise, <see langword="false" />.</para>
    ///     <para xml:lang="zh">如果解析成功返回 true，否则返回 false。</para>
    /// </returns>
    public static bool TryParse(ReadOnlySpan<byte> base32, out Ulid ulid)
    {
        if (base32.Length != 26)
        {
            ulid = default;
            return false;
        }
        try
        {
            ulid = ParseCore(base32);
            return true;
        }
        catch
        {
            ulid = default;
            return false;
        }
    }

    private static Ulid ParseCore(ReadOnlySpan<byte> base32)
    {
        if (base32.Length != 26)
        {
            throw new ArgumentException("invalid base32 length, length:" + base32.Length);
        }
        var ulid = default(Ulid);
        Unsafe.Add(ref Unsafe.As<Ulid, byte>(ref ulid), 15) = (byte)((CharToBase32[base32[24]] << 5) | CharToBase32[base32[25]]);
        Unsafe.Add(ref Unsafe.As<Ulid, byte>(ref ulid), 0) = (byte)((CharToBase32[base32[0]] << 5) | CharToBase32[base32[1]]);
        Unsafe.Add(ref Unsafe.As<Ulid, byte>(ref ulid), 1) = (byte)((CharToBase32[base32[2]] << 3) | (CharToBase32[base32[3]] >> 2));
        Unsafe.Add(ref Unsafe.As<Ulid, byte>(ref ulid), 2) = (byte)((CharToBase32[base32[3]] << 6) | (CharToBase32[base32[4]] << 1) | (CharToBase32[base32[5]] >> 4));
        Unsafe.Add(ref Unsafe.As<Ulid, byte>(ref ulid), 3) = (byte)((CharToBase32[base32[5]] << 4) | (CharToBase32[base32[6]] >> 1));
        Unsafe.Add(ref Unsafe.As<Ulid, byte>(ref ulid), 4) = (byte)((CharToBase32[base32[6]] << 7) | (CharToBase32[base32[7]] << 2) | (CharToBase32[base32[8]] >> 3));
        Unsafe.Add(ref Unsafe.As<Ulid, byte>(ref ulid), 5) = (byte)((CharToBase32[base32[8]] << 5) | CharToBase32[base32[9]]);
        Unsafe.Add(ref Unsafe.As<Ulid, byte>(ref ulid), 6) = (byte)((CharToBase32[base32[10]] << 3) | (CharToBase32[base32[11]] >> 2));
        Unsafe.Add(ref Unsafe.As<Ulid, byte>(ref ulid), 7) = (byte)((CharToBase32[base32[11]] << 6) | (CharToBase32[base32[12]] << 1) | (CharToBase32[base32[13]] >> 4));
        Unsafe.Add(ref Unsafe.As<Ulid, byte>(ref ulid), 8) = (byte)((CharToBase32[base32[13]] << 4) | (CharToBase32[base32[14]] >> 1));
        Unsafe.Add(ref Unsafe.As<Ulid, byte>(ref ulid), 9) = (byte)((CharToBase32[base32[14]] << 7) | (CharToBase32[base32[15]] << 2) | (CharToBase32[base32[16]] >> 3));
        Unsafe.Add(ref Unsafe.As<Ulid, byte>(ref ulid), 10) = (byte)((CharToBase32[base32[16]] << 5) | CharToBase32[base32[17]]);
        Unsafe.Add(ref Unsafe.As<Ulid, byte>(ref ulid), 11) = (byte)((CharToBase32[base32[18]] << 3) | (CharToBase32[base32[19]] >> 2));
        Unsafe.Add(ref Unsafe.As<Ulid, byte>(ref ulid), 12) = (byte)((CharToBase32[base32[19]] << 6) | (CharToBase32[base32[20]] << 1) | (CharToBase32[base32[21]] >> 4));
        Unsafe.Add(ref Unsafe.As<Ulid, byte>(ref ulid), 13) = (byte)((CharToBase32[base32[21]] << 4) | (CharToBase32[base32[22]] >> 1));
        Unsafe.Add(ref Unsafe.As<Ulid, byte>(ref ulid), 14) = (byte)((CharToBase32[base32[22]] << 7) | (CharToBase32[base32[23]] << 2) | (CharToBase32[base32[24]] >> 3));
        return ulid;
    }

    // Convert
    /// <summary>
    ///     <para xml:lang="en">Converts the current instance to a 16-byte array representation.</para>
    ///     <para xml:lang="zh">转换为 16 字节数组。</para>
    /// </summary>
    /// <returns>
    ///     <para xml:lang="en">A 16-byte array that represents the current instance.</para>
    ///     <para xml:lang="zh">表示当前实例的 16 字节数组。</para>
    /// </returns>
    public byte[] ToByteArray()
    {
        var bytes = new byte[16];
        MemoryMarshal.Write(bytes, in this);
        return bytes;
    }

    /// <summary>
    ///     <para xml:lang="en">Attempts to write the current instance to the specified span of bytes.</para>
    ///     <para xml:lang="zh">尝试写入 16 字节到目标 Span。</para>
    /// </summary>
    /// <param name="destination">
    ///     <para xml:lang="en">The span of bytes to which the instance will be written. Must have a length of at least 16 bytes.</para>
    ///     <para xml:lang="zh">目标字节 Span，长度至少为 16。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">
    ///     <see langword="true" /> if the instance was successfully written to the <paramref name="destination" /> span; otherwise,
    ///     <see langword="false" /> if the span is too small.
    ///     </para>
    ///     <para xml:lang="zh">写入成功返回 true，否则返回 false。</para>
    /// </returns>
    public bool TryWriteBytes(Span<byte> destination)
    {
        if (destination.Length < 16)
        {
            return false;
        }
        MemoryMarshal.Write(destination, in this);
        return true;
    }

    /// <summary>
    ///     <para xml:lang="en">Converts the current object to its Base64 string representation.</para>
    ///     <para xml:lang="zh">转换为 Base64 字符串。</para>
    /// </summary>
    /// <param name="options">
    ///     <para xml:lang="en">Specifies formatting options for the Base64 string. The default is <see cref="Base64FormattingOptions.None" />.</para>
    ///     <para xml:lang="zh">Base64 格式化选项，默认为 <see cref="Base64FormattingOptions.None" />。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">A Base64-encoded string representation of the current object.</para>
    ///     <para xml:lang="zh">当前对象的 Base64 字符串表示。</para>
    /// </returns>
    public string ToBase64(Base64FormattingOptions options = Base64FormattingOptions.None)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(16);
        try
        {
            TryWriteBytes(buffer);
            return Convert.ToBase64String(buffer, options);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///     Attempts to write a 26-character Base32-encoded string representation of a timestamp and randomness into the provided
    ///     <see cref="Span{T}" /> of bytes.
    ///     </para>
    ///     <para xml:lang="zh">尝试将当前实例以 Base32 编码写入字节 Span。</para>
    /// </summary>
    /// <param name="span">
    ///     <para xml:lang="en">The <see cref="Span{T}" /> of bytes where the encoded string will be written. Must have a length of at least 26 bytes.</para>
    ///     <para xml:lang="zh">目标字节 Span，长度至少为 26。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">
    ///     <see langword="true" /> if the operation succeeds and the encoded string is written to the span; otherwise,
    ///     <see langword="false" /> if the span's length is less than 26.
    ///     </para>
    ///     <para xml:lang="zh">写入成功返回 true，否则返回 false。</para>
    /// </returns>
    public bool TryWriteStringify(Span<byte> span)
    {
        if (span.Length < 26)
        {
            return false;
        }
        span[25] = Base32Bytes[randomness9 & 31]; // eliminate bounds-check of span

        // timestamp
        span[0] = Base32Bytes[(timestamp0 & 224) >> 5];
        span[1] = Base32Bytes[timestamp0 & 31];
        span[2] = Base32Bytes[(timestamp1 & 248) >> 3];
        span[3] = Base32Bytes[((timestamp1 & 7) << 2) | ((timestamp2 & 192) >> 6)];
        span[4] = Base32Bytes[(timestamp2 & 62) >> 1];
        span[5] = Base32Bytes[((timestamp2 & 1) << 4) | ((timestamp3 & 240) >> 4)];
        span[6] = Base32Bytes[((timestamp3 & 15) << 1) | ((timestamp4 & 128) >> 7)];
        span[7] = Base32Bytes[(timestamp4 & 124) >> 2];
        span[8] = Base32Bytes[((timestamp4 & 3) << 3) | ((timestamp5 & 224) >> 5)];
        span[9] = Base32Bytes[timestamp5 & 31];

        // randomness
        span[10] = Base32Bytes[(randomness0 & 248) >> 3];
        span[11] = Base32Bytes[((randomness0 & 7) << 2) | ((randomness1 & 192) >> 6)];
        span[12] = Base32Bytes[(randomness1 & 62) >> 1];
        span[13] = Base32Bytes[((randomness1 & 1) << 4) | ((randomness2 & 240) >> 4)];
        span[14] = Base32Bytes[((randomness2 & 15) << 1) | ((randomness3 & 128) >> 7)];
        span[15] = Base32Bytes[(randomness3 & 124) >> 2];
        span[16] = Base32Bytes[((randomness3 & 3) << 3) | ((randomness4 & 224) >> 5)];
        span[17] = Base32Bytes[randomness4 & 31];
        span[18] = Base32Bytes[(randomness5 & 248) >> 3];
        span[19] = Base32Bytes[((randomness5 & 7) << 2) | ((randomness6 & 192) >> 6)];
        span[20] = Base32Bytes[(randomness6 & 62) >> 1];
        span[21] = Base32Bytes[((randomness6 & 1) << 4) | ((randomness7 & 240) >> 4)];
        span[22] = Base32Bytes[((randomness7 & 15) << 1) | ((randomness8 & 128) >> 7)];
        span[23] = Base32Bytes[(randomness8 & 124) >> 2];
        span[24] = Base32Bytes[((randomness8 & 3) << 3) | ((randomness9 & 224) >> 5)];
        return true;
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///     Attempts to write a 26-character Base32-encoded string representation of a timestamp and randomness into the provided
    ///     <see cref="Span{T}" /> of characters.
    ///     </para>
    ///     <para xml:lang="zh">尝试将当前实例以 Base32 编码写入字符 Span。</para>
    /// </summary>
    /// <param name="span">
    ///     <para xml:lang="en">The <see cref="Span{T}" /> of characters where the encoded string will be written. Must have a length of at least 26.</para>
    ///     <para xml:lang="zh">目标字符 Span，长度至少为 26。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">
    ///     <see langword="true" /> if the operation succeeds and the encoded string is written to <paramref name="span" />; otherwise,
    ///     <see langword="false" /> if the length of <paramref name="span" /> is less than 26.
    ///     </para>
    ///     <para xml:lang="zh">写入成功返回 true，否则返回 false。</para>
    /// </returns>
    public bool TryWriteStringify(Span<char> span)
    {
        if (span.Length < 26)
        {
            return false;
        }
        span[25] = Base32Text[randomness9 & 31]; // eliminate bounds-check of span

        // timestamp
        span[0] = Base32Text[(timestamp0 & 224) >> 5];
        span[1] = Base32Text[timestamp0 & 31];
        span[2] = Base32Text[(timestamp1 & 248) >> 3];
        span[3] = Base32Text[((timestamp1 & 7) << 2) | ((timestamp2 & 192) >> 6)];
        span[4] = Base32Text[(timestamp2 & 62) >> 1];
        span[5] = Base32Text[((timestamp2 & 1) << 4) | ((timestamp3 & 240) >> 4)];
        span[6] = Base32Text[((timestamp3 & 15) << 1) | ((timestamp4 & 128) >> 7)];
        span[7] = Base32Text[(timestamp4 & 124) >> 2];
        span[8] = Base32Text[((timestamp4 & 3) << 3) | ((timestamp5 & 224) >> 5)];
        span[9] = Base32Text[timestamp5 & 31];

        // randomness
        span[10] = Base32Text[(randomness0 & 248) >> 3];
        span[11] = Base32Text[((randomness0 & 7) << 2) | ((randomness1 & 192) >> 6)];
        span[12] = Base32Text[(randomness1 & 62) >> 1];
        span[13] = Base32Text[((randomness1 & 1) << 4) | ((randomness2 & 240) >> 4)];
        span[14] = Base32Text[((randomness2 & 15) << 1) | ((randomness3 & 128) >> 7)];
        span[15] = Base32Text[(randomness3 & 124) >> 2];
        span[16] = Base32Text[((randomness3 & 3) << 3) | ((randomness4 & 224) >> 5)];
        span[17] = Base32Text[randomness4 & 31];
        span[18] = Base32Text[(randomness5 & 248) >> 3];
        span[19] = Base32Text[((randomness5 & 7) << 2) | ((randomness6 & 192) >> 6)];
        span[20] = Base32Text[(randomness6 & 62) >> 1];
        span[21] = Base32Text[((randomness6 & 1) << 4) | ((randomness7 & 240) >> 4)];
        span[22] = Base32Text[((randomness7 & 15) << 1) | ((randomness8 & 128) >> 7)];
        span[23] = Base32Text[(randomness8 & 124) >> 2];
        span[24] = Base32Text[((randomness8 & 3) << 3) | ((randomness9 & 224) >> 5)];
        return true;
    }

    /// <summary>
    ///     <para xml:lang="en">Returns a string representation of the current object.</para>
    ///     <para xml:lang="zh">返回当前对象的字符串表示。</para>
    /// </summary>
    /// <returns>
    ///     <para xml:lang="en">A string that represents the current object.</para>
    ///     <para xml:lang="zh">当前对象的字符串。</para>
    /// </returns>
    public override string ToString()
    {
        return string.Create(26, this, static (span, state) => state.TryWriteStringify(span));
    }

    //
    //ISpanFormattable
    //
    /// <summary>
    ///     <para xml:lang="en">Attempts to format the current instance into the provided character span.</para>
    ///     <para xml:lang="zh">尝试将当前实例格式化到字符 Span。</para>
    /// </summary>
    /// <param name="destination">
    ///     <para xml:lang="en">The span of characters to which the formatted value will be written.</para>
    ///     <para xml:lang="zh">目标字符 Span。</para>
    /// </param>
    /// <param name="charsWritten">
    ///     <para xml:lang="en">When this method returns, contains the number of characters written to <paramref name="destination" />.</para>
    ///     <para xml:lang="zh">方法返回时，写入的字符数。</para>
    /// </param>
    /// <param name="format">
    ///     <para xml:lang="en">A read-only span containing the format string to use, or an empty span to use the default format.</para>
    ///     <para xml:lang="zh">格式字符串，或空表示默认格式。</para>
    /// </param>
    /// <param name="provider">
    ///     <para xml:lang="en">An optional object that provides culture-specific formatting information. Can be <see langword="null" />.</para>
    ///     <para xml:lang="zh">可选的区域性信息对象，可以为 null。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">
    ///     <see langword="true" /> if the formatting operation was successful and the value was written to
    ///     <paramref name="destination" />; otherwise, <see langword="false" />.
    ///     </para>
    ///     <para xml:lang="zh">格式化成功返回 true，否则返回 false。</para>
    /// </returns>
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (TryWriteStringify(destination))
        {
            charsWritten = 26;
            return true;
        }
        charsWritten = 0;
        return false;
    }

    /// <summary>
    ///     <para xml:lang="en">Converts the current object to its string representation using the specified format and format provider.</para>
    ///     <para xml:lang="zh">使用指定格式和区域性将当前对象转换为字符串。</para>
    /// </summary>
    /// <param name="format">
    ///     <para xml:lang="en">A format string that specifies the format to use, or <see langword="null" /> to use the default format.</para>
    ///     <para xml:lang="zh">格式字符串，null 表示默认格式。</para>
    /// </param>
    /// <param name="formatProvider">
    ///     <para xml:lang="en">An object that provides culture-specific formatting information, or <see langword="null" /> to use the current culture.</para>
    ///     <para xml:lang="zh">区域性信息对象，null 表示当前区域性。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">A string representation of the current object, formatted as specified.</para>
    ///     <para xml:lang="zh">指定格式的字符串表示。</para>
    /// </returns>
    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    //
    // IParsable
    //
    /// <inheritdoc cref="IParsable{TSelf}.Parse(string, IFormatProvider?)" />
    public static Ulid Parse(string s, IFormatProvider? provider) => Parse(s);

    /// <inheritdoc cref="IParsable{TSelf}.TryParse(string?, IFormatProvider?, out TSelf)" />
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out Ulid result) => TryParse(s, out result);

    //
    // ISpanParsable
    //
    /// <inheritdoc cref="ISpanParsable{TSelf}.Parse(ReadOnlySpan{char}, IFormatProvider?)" />
    public static Ulid Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s);

    /// <inheritdoc cref="ISpanParsable{TSelf}.TryParse(ReadOnlySpan{char}, IFormatProvider?, out TSelf)" />
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Ulid result) => TryParse(s, out result);

    //
    // IUtf8SpanFormattable
    //
    /// <summary>
    ///     <para xml:lang="en">Attempts to format the current instance as a UTF-8 encoded byte sequence and write it to the specified destination buffer.</para>
    ///     <para xml:lang="zh">尝试将当前实例格式化为 UTF-8 编码的字节序列并写入目标缓冲区。</para>
    /// </summary>
    /// <remarks>
    /// If the <paramref name="destination" /> buffer is too small to contain the formatted output,
    /// the method returns <see langword="false" />  and no data is written to the buffer.
    /// </remarks>
    /// <param name="destination">The buffer to which the formatted UTF-8 byte sequence will be written.</param>
    /// <param name="charsWritten">
    ///     <para xml:lang="en">
    ///     When this method returns, contains the number of characters written to the <paramref name="destination" /> buffer,  or 0 if
    ///     the operation fails.
    ///     </para>
    ///     <para xml:lang="zh">方法返回时，写入目标缓冲区的字符数，若操作失败则为 0。</para>
    /// </param>
    /// <param name="format">
    ///     <para xml:lang="en">A read-only span containing the format string to use, or an empty span to use the default format.</para>
    ///     <para xml:lang="zh">格式字符串，或空表示默认格式。</para>
    /// </param>
    /// <param name="provider">
    ///     <para xml:lang="en">
    ///     An optional object that provides culture-specific formatting information, or <see langword="null" /> to use the default
    ///     format provider.
    ///     </para>
    ///     <para xml:lang="zh">可选的区域性信息对象，null 表示使用默认格式提供程序。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">
    ///     <see langword="true" /> if the formatting operation succeeds and the UTF-8 byte sequence is written to the
    ///     <paramref name="destination" /> buffer;  otherwise, <see langword="false" />.
    ///     </para>
    ///     <para xml:lang="zh">如果格式化操作成功并将 UTF-8 字节序列写入目标缓冲区，则返回 true；否则返回 false。</para>
    /// </returns>
    public bool TryFormat(Span<byte> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (TryWriteStringify(destination))
        {
            charsWritten = 26;
            return true;
        }
        charsWritten = 0;
        return false;
    }

    // Comparable/Equatable
    /// <summary>
    ///     <para xml:lang="en">Returns a hash code for the current <see cref="Ulid" /> instance.</para>
    ///     <para xml:lang="zh">返回当前 <see cref="Ulid" /> 实例的哈希代码。</para>
    /// </summary>
    /// <remarks>
    /// The hash code is computed based on the internal state of the <see cref="Ulid" /> instance. It
    /// is suitable for use in hashing algorithms and data structures such as hash tables.
    /// </remarks>
    /// <returns>
    ///     <para xml:lang="en">An integer that represents the hash code for the current <see cref="Ulid" /> instance.</para>
    ///     <para xml:lang="zh">表示当前 <see cref="Ulid" /> 实例的哈希代码的整数。</para>
    /// </returns>
    public override int GetHashCode()
    {
        ref var rA = ref Unsafe.As<Ulid, int>(ref Unsafe.AsRef(in this));
        return rA ^ Unsafe.Add(ref rA, 1) ^ Unsafe.Add(ref rA, 2) ^ Unsafe.Add(ref rA, 3);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool EqualsCore(in Ulid left, in Ulid right)
    {
#if NET10_0_OR_GREATER
        if (Vector256.IsHardwareAccelerated)
        {
            var vA = Unsafe.As<Ulid, Vector256<byte>>(ref Unsafe.AsRef(in left));
            var vB = Unsafe.As<Ulid, Vector256<byte>>(ref Unsafe.AsRef(in right));
            return vA == vB;
        }
#endif
        if (Vector128.IsHardwareAccelerated)
        {
            return Unsafe.As<Ulid, Vector128<byte>>(ref Unsafe.AsRef(in left)) == Unsafe.As<Ulid, Vector128<byte>>(ref Unsafe.AsRef(in right));
        }
        if (Sse2.IsSupported)
        {
            var vA = Unsafe.As<Ulid, Vector128<byte>>(ref Unsafe.AsRef(in left));
            var vB = Unsafe.As<Ulid, Vector128<byte>>(ref Unsafe.AsRef(in right));
            var cmp = Sse2.CompareEqual(vA, vB);
            return Sse2.MoveMask(cmp) == 0xFFFF;
        }
        ref var rA = ref Unsafe.As<Ulid, long>(ref Unsafe.AsRef(in left));
        ref var rB = ref Unsafe.As<Ulid, long>(ref Unsafe.AsRef(in right));

        // Compare each element
        return rA == rB && Unsafe.Add(ref rA, 1) == Unsafe.Add(ref rB, 1);
    }

    /// <summary>
    ///     <para xml:lang="en">Determines whether the current instance is equal to the specified <see cref="Ulid" /> instance.</para>
    ///     <para xml:lang="zh">确定当前实例是否等于指定的 <see cref="Ulid" /> 实例。</para>
    /// </summary>
    /// <param name="other">
    ///     <para xml:lang="en">The <see cref="Ulid" /> instance to compare with the current instance.</para>
    ///     <para xml:lang="zh">要与当前实例进行比较的 <see cref="Ulid" /> 实例。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">
    ///     <see langword="true" /> if the specified <see cref="Ulid" /> is equal to the current instance; otherwise,
    ///     <see langword="false" />.
    ///     </para>
    ///     <para xml:lang="zh">如果指定的 <see cref="Ulid" /> 等于当前实例，则返回 true；否则返回 false。</para>
    /// </returns>
    public bool Equals(Ulid other) => EqualsCore(this, other);

    /// <summary>
    ///     <para xml:lang="en">Determines whether the specified object is equal to the current instance.</para>
    ///     <para xml:lang="zh">确定指定对象是否等于当前实例。</para>
    /// </summary>
    /// <param name="obj">
    ///     <para xml:lang="en">The object to compare with the current instance. Can be <see langword="null" />.</para>
    ///     <para xml:lang="zh">要与当前实例进行比较的对象。可以为 null。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">
    ///     <see langword="true" /> if the specified object is of type <c>Ulid</c> and is equal to the current instance; otherwise,
    ///     <see langword="false" />.
    ///     </para>
    ///     <para xml:lang="zh">如果指定对象是 <c>Ulid</c> 类型并且等于当前实例，则返回 true；否则返回 false。</para>
    /// </returns>
    public override bool Equals(object? obj) => obj is Ulid other && EqualsCore(this, other);

    /// <summary>
    ///     <para xml:lang="en">Determines whether two <see cref="Ulid" /> instances are equal.</para>
    ///     <para xml:lang="zh">确定两个 <see cref="Ulid" /> 实例是否相等。</para>
    /// </summary>
    /// <param name="a">
    ///     <para xml:lang="en">The first <see cref="Ulid" /> instance to compare.</para>
    ///     <para xml:lang="zh">要比较的第一个 <see cref="Ulid" /> 实例。</para>
    /// </param>
    /// <param name="b">
    ///     <para xml:lang="en">The second <see cref="Ulid" /> instance to compare.</para>
    ///     <para xml:lang="zh">要比较的第二个 <see cref="Ulid" /> 实例。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en"><see langword="true" /> if the two <see cref="Ulid" /> instances are equal; otherwise, <see langword="false" />.</para>
    ///     <para xml:lang="zh">如果两个 <see cref="Ulid" /> 实例相等，则返回 true；否则返回 false。</para>
    /// </returns>
    public static bool operator ==(Ulid a, Ulid b) => EqualsCore(a, b);

    /// <summary>
    ///     <para xml:lang="en">Determines whether two <see cref="Ulid" /> instances are not equal.</para>
    ///     <para xml:lang="zh">确定两个 <see cref="Ulid" /> 实例是否不相等。</para>
    /// </summary>
    /// <param name="a">
    ///     <para xml:lang="en">The first <see cref="Ulid" /> instance to compare.</para>
    ///     <para xml:lang="zh">要比较的第一个 <see cref="Ulid" /> 实例。</para>
    /// </param>
    /// <param name="b">
    ///     <para xml:lang="en">The second <see cref="Ulid" /> instance to compare.</para>
    ///     <para xml:lang="zh">要比较的第二个 <see cref="Ulid" /> 实例。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en"><see langword="true" /> if the two <see cref="Ulid" /> instances are not equal; otherwise, <see langword="false" />.</para>
    ///     <para xml:lang="zh">如果两个 <see cref="Ulid" /> 实例不相等，则返回 true；否则返回 false。</para>
    /// </returns>
    public static bool operator !=(Ulid a, Ulid b) => !EqualsCore(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetResult(byte me, byte them) => me < them ? -1 : 1;

    /// <summary>
    ///     <para xml:lang="en">
    ///     Compares the current <see cref="Ulid" /> instance to another <see cref="Ulid" /> instance and returns an integer that
    ///     indicates their relative order.
    ///     </para>
    ///     <para xml:lang="zh">将当前 <see cref="Ulid" /> 实例与另一个 <see cref="Ulid" /> 实例进行比较，并返回一个整数，指示它们的相对顺序。</para>
    /// </summary>
    /// <remarks>
    /// The comparison is performed by evaluating the timestamp components of the <see cref="Ulid" />
    /// instances first, followed by the randomness components if the timestamps are equal. This ensures a consistent
    /// lexicographical ordering of <see cref="Ulid" /> values.
    /// </remarks>
    /// <param name="other">
    ///     <para xml:lang="en">The <see cref="Ulid" /> instance to compare with the current instance.</para>
    ///     <para xml:lang="zh">要与当前实例进行比较的 <see cref="Ulid" /> 实例。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">A signed integer that indicates the relative order of the two <see cref="Ulid" /> instances:</para>
    ///     <para xml:lang="zh">一个有符号整数，指示两个 <see cref="Ulid" /> 实例的相对顺序：</para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 <para xml:lang="en">Less than zero if the current instance precedes <paramref name="other" /> in the sort order.</para>
    ///                 <para xml:lang="zh">如果当前实例在排序顺序中位于 <paramref name="other" /> 之前，则小于零。</para>
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <para xml:lang="en">Zero if the current instance is equal to <paramref name="other" />.</para>
    ///                 <para xml:lang="zh">如果当前实例等于 <paramref name="other" />，则为零。</para>
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <para xml:lang="en">Greater than zero if the current instance follows <paramref name="other" /> in the sort order.</para>
    ///                 <para xml:lang="zh">如果当前实例在排序顺序中位于 <paramref name="other" /> 之后，则大于零。</para>
    ///             </description>
    ///         </item>
    ///     </list>
    /// </returns>
    public int CompareTo(Ulid other)
    {
        if (timestamp0 != other.timestamp0)
        {
            return GetResult(timestamp0, other.timestamp0);
        }
        if (timestamp1 != other.timestamp1)
        {
            return GetResult(timestamp1, other.timestamp1);
        }
        if (timestamp2 != other.timestamp2)
        {
            return GetResult(timestamp2, other.timestamp2);
        }
        if (timestamp3 != other.timestamp3)
        {
            return GetResult(timestamp3, other.timestamp3);
        }
        if (timestamp4 != other.timestamp4)
        {
            return GetResult(timestamp4, other.timestamp4);
        }
        if (timestamp5 != other.timestamp5)
        {
            return GetResult(timestamp5, other.timestamp5);
        }
        if (randomness0 != other.randomness0)
        {
            return GetResult(randomness0, other.randomness0);
        }
        if (randomness1 != other.randomness1)
        {
            return GetResult(randomness1, other.randomness1);
        }
        if (randomness2 != other.randomness2)
        {
            return GetResult(randomness2, other.randomness2);
        }
        if (randomness3 != other.randomness3)
        {
            return GetResult(randomness3, other.randomness3);
        }
        if (randomness4 != other.randomness4)
        {
            return GetResult(randomness4, other.randomness4);
        }
        if (randomness5 != other.randomness5)
        {
            return GetResult(randomness5, other.randomness5);
        }
        if (randomness6 != other.randomness6)
        {
            return GetResult(randomness6, other.randomness6);
        }
        if (randomness7 != other.randomness7)
        {
            return GetResult(randomness7, other.randomness7);
        }
        if (randomness8 != other.randomness8)
        {
            return GetResult(randomness8, other.randomness8);
        }
        if (randomness9 != other.randomness9)
        {
            return GetResult(randomness9, other.randomness9);
        }
        return 0;
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///     Compares the current instance with another object of the same type and returns an integer that indicates whether the current
    ///     instance precedes, follows, or occurs in the same position in the sort order as the other object.
    ///     </para>
    ///     <para xml:lang="zh">将当前实例与另一个相同类型的对象进行比较，并返回一个整数，指示当前实例在排序顺序中是位于另一个对象之前、之后还是在相同位置。</para>
    /// </summary>
    /// <exception cref="ArgumentException">
    ///     <para xml:lang="en">Thrown when one or more arguments have unsupported or illegal values.</para>
    ///     <para xml:lang="zh">当一个或多个参数具有不支持或非法的值时抛出。</para>
    /// </exception>
    /// <param name="other">
    ///     <para xml:lang="en">An object to compare with this instance.</para>
    ///     <para xml:lang="zh">要与此实例进行比较的对象。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">A value that indicates the relative order of the objects being compared. The return value has these meanings:</para>
    ///     <para xml:lang="zh">指示正在比较的对象的相对顺序的值。返回值具有以下含义：</para>
    ///     <list type="table">
    ///         <listheader>
    ///             <term>
    ///                 <para xml:lang="en">Value</para>
    ///                 <para xml:lang="zh">值</para>
    ///             </term>
    ///             <description>
    ///                 <para xml:lang="en">Meaning</para>
    ///                 <para xml:lang="zh">含义</para>
    ///             </description>
    ///         </listheader>
    ///         <item>
    ///             <term>
    ///                 <para xml:lang="en">Less than zero</para>
    ///                 <para xml:lang="zh">小于零</para>
    ///             </term>
    ///             <description>
    ///                 <para xml:lang="en">This instance precedes <paramref name="other" /> in the sort order.</para>
    ///                 <para xml:lang="zh">此实例在排序顺序中位于 <paramref name="other" /> 之前。</para>
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <term>
    ///                 <para xml:lang="en">Zero</para>
    ///                 <para xml:lang="zh">零</para>
    ///             </term>
    ///             <description>
    ///                 <para xml:lang="en">This instance occurs in the same position in the sort order as <paramref name="other" />.</para>
    ///                 <para xml:lang="zh">此实例在排序顺序中与 <paramref name="other" /> 处于相同位置。</para>
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <term>
    ///                 <para xml:lang="en">Greater than zero</para>
    ///                 <para xml:lang="zh">大于零</para>
    ///             </term>
    ///             <description>
    ///                 <para xml:lang="en">This instance follows <paramref name="other" /> in the sort order.</para>
    ///                 <para xml:lang="zh">此实例在排序顺序中位于 <paramref name="other" /> 之后。</para>
    ///             </description>
    ///         </item>
    ///     </list>
    /// </returns>
    public int CompareTo(object? other)
    {
        return other switch
        {
            null      => 1,
            Ulid ulid => CompareTo(ulid),
            _         => throw new ArgumentException("Object must be of type ULID.", nameof(other))
        };
    }

    /// <summary>
    ///     <para xml:lang="en">Converts the specified <see cref="Ulid" /> instance to a <see cref="Guid" />.</para>
    ///     <para xml:lang="zh">将指定的 <see cref="Ulid" /> 实例转换为 <see cref="Guid" />。</para>
    /// </summary>
    /// <param name="_this">
    ///     <para xml:lang="en">The <see cref="Ulid" /> instance to convert.</para>
    ///     <para xml:lang="zh">要转换的 <see cref="Ulid" /> 实例。</para>
    /// </param>
    public static explicit operator Guid(Ulid _this) => _this.ToGuid();

    /// <summary>
    ///     <para xml:lang="en">Convert this <c>Ulid</c> value to a <c>Guid</c> value with the same comparability.</para>
    ///     <para xml:lang="zh">将此 <c>Ulid</c> 值转换为具有相同可比性的 <c>Guid</c> 值。</para>
    /// </summary>
    /// <remarks>
    /// The byte arrangement between Ulid and Guid is not preserved.
    /// </remarks>
    /// <returns>
    ///     <para xml:lang="en">The converted <c>Guid</c> value</para>
    ///     <para xml:lang="zh">转换后的 <c>Guid</c> 值</para>
    /// </returns>
    public Guid ToGuid()
    {
        if (IsVector128Supported && BitConverter.IsLittleEndian)
        {
            var vector = Unsafe.As<Ulid, Vector128<byte>>(ref Unsafe.AsRef(in this));
            var shuffled = Shuffle(vector, Vector128.Create((byte)3, 2, 1, 0, 5, 4, 7, 6, 8, 9, 10, 11, 12, 13, 14, 15));
            return Unsafe.As<Vector128<byte>, Guid>(ref shuffled);
        }
        Span<byte> buf = stackalloc byte[16];
        if (BitConverter.IsLittleEndian)
        {
            // |A|B|C|D|E|F|G|H|I|J|K|L|M|N|O|P|
            // |D|C|B|A|...
            //      ...|F|E|H|G|...
            //              ...|I|J|K|L|M|N|O|P|
            ref var ptr = ref Unsafe.As<Ulid, uint>(ref Unsafe.AsRef(in this));
            var lower = BinaryPrimitives.ReverseEndianness(ptr);
            MemoryMarshal.Write(buf, in lower);
            ptr = ref Unsafe.Add(ref ptr, 1);
            var upper = ((ptr & 0x00_FF_00_FF) << 8) | ((ptr & 0xFF_00_FF_00) >> 8);
            MemoryMarshal.Write(buf[4..], in upper);
            ref var upperBytes = ref Unsafe.As<uint, ulong>(ref Unsafe.Add(ref ptr, 1));
            MemoryMarshal.Write(buf[8..], in upperBytes);
        }
        else
        {
            MemoryMarshal.Write(buf, in Unsafe.AsRef(in this));
        }
        return MemoryMarshal.Read<Guid>(buf);
    }

    private static bool IsVector128Supported => Vector128.IsHardwareAccelerated;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector128<byte> Shuffle(Vector128<byte> value, Vector128<byte> mask)
    {
        Debug.Assert(BitConverter.IsLittleEndian);
        Debug.Assert(IsVector128Supported);
        return Vector128.IsHardwareAccelerated
                   ? Vector128.Shuffle(value, mask)
                   : Ssse3.IsSupported
                       ? Ssse3.Shuffle(value, mask)
                       : throw new NotImplementedException();
    }
}

static file class RandomProvider
{
    // 优先使用 Random.Shared (线程安全, .NET 6+)
    public static Random GetRandom() => Random.Shared;

    public static XorShift64 GetXorShift64()
    {
        Span<byte> buffer = stackalloc byte[sizeof(ulong)];
        RandomNumberGenerator.Fill(buffer);
        var seed = BitConverter.ToUInt64(buffer);
        return new(seed);
    }
}

internal sealed class XorShift64
{
    private ulong x = 88172645463325252UL;

    public XorShift64(ulong seed)
    {
        if (seed != 0)
        {
            x = seed;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong Next()
    {
        x ^= x << 7;
        return x ^= x >> 9;
    }
}