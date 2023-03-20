using System.Security.Cryptography;

namespace EasilyNET.Security;

/// <summary>
/// 64-bit CRC 实现
/// </summary>
/// <remarks>
/// 支持ISO 3309标准
/// </remarks>
public class Crc64 : HashAlgorithm
{
    public const ulong DefaultSeed = 0x0;
    public const ulong Iso3309Polynomial = 0xD800000000000000;
    internal static ulong[] Table;
    private readonly ulong _seed;
    private readonly ulong[] _table;
    private ulong _hash;

    public Crc64() : this(Iso3309Polynomial) { }

    public Crc64(ulong polynomial) : this(polynomial, DefaultSeed) { }

    public Crc64(ulong polynomial, ulong seed)
    {
        if (!BitConverter.IsLittleEndian)
        {
            throw new PlatformNotSupportedException("Big Endian 处理程序不支持");
        }
        _table = InitializeTable(polynomial);
        _seed = _hash = seed;
    }

    public override int HashSize => 64;

    public override void Initialize()
    {
        _hash = _seed;
    }

    protected override void HashCore(byte[] array, int ibStart, int cbSize)
    {
        _hash = CalculateHash(_hash, _table, array, ibStart, cbSize);
    }

    protected override byte[] HashFinal()
    {
        var hashBuffer = UInt64ToBigEndianBytes(_hash);
        HashValue = hashBuffer;
        return hashBuffer;
    }

    static protected ulong CalculateHash(ulong seed, ulong[] table, IList<byte> buffer, int start, int size)
    {
        var hash = seed;
        for (var i = start; i < start + size; i++)
        {
            unchecked
            {
                hash = hash >> 8 ^ table[(buffer[i] ^ hash) & 0xff];
            }
        }
        return hash;
    }

    private static byte[] UInt64ToBigEndianBytes(ulong value)
    {
        var result = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(result);
        }
        return result;
    }

    private static ulong[] InitializeTable(ulong polynomial)
    {
        if (polynomial == Iso3309Polynomial && Table != null)
        {
            return Table;
        }
        var createTable = CreateTable(polynomial);
        if (polynomial == Iso3309Polynomial)
        {
            Table = createTable;
        }
        return createTable;
    }

    static protected ulong[] CreateTable(ulong polynomial)
    {
        var createTable = new ulong[256];
        for (var i = 0; i < 256; ++i)
        {
            var entry = (ulong)i;
            for (var j = 0; j < 8; ++j)
            {
                if ((entry & 1) == 1)
                {
                    entry = entry >> 1 ^ polynomial;
                }
                else
                {
                    entry >>= 1;
                }
            }
            createTable[i] = entry;
        }
        return createTable;
    }

    public static ulong Compute(byte[] buffer) => Compute(DefaultSeed, buffer);

    public static ulong Compute(ulong seed, byte[] buffer)
    {
        Table ??= CreateTable(Iso3309Polynomial);
        return CalculateHash(seed, Table, buffer, 0, buffer.Length);
    }
}