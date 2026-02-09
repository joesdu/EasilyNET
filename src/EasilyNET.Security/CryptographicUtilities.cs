using System.Buffers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

// ReSharper disable UnusedMember.Global

namespace EasilyNET.Security;

/// <summary>
///     <para xml:lang="en">Utility class for secure cryptographic key derivation and validation</para>
///     <para xml:lang="zh">用于安全加密密钥派生和验证的工具类</para>
/// </summary>
public static class CryptographicUtilities
{
    // PBKDF2 默认参数
    private const int DefaultIterationCount = 100_000;
    private const int DefaultSaltLength = 16;

    /// <summary>
    ///     <para xml:lang="en">Derives a cryptographic key from a password using PBKDF2</para>
    ///     <para xml:lang="zh">使用 PBKDF2 从密码派生加密密钥</para>
    /// </summary>
    /// <param name="password">Password to derive key from</param>
    /// <param name="salt">Salt for key derivation</param>
    /// <param name="keyLength">Length of the derived key in bytes</param>
    /// <param name="iterations">Number of PBKDF2 iterations (default: 100,000)</param>
    /// <returns>Derived key bytes</returns>
    public static byte[] DeriveKey(
        string password,
        byte[] salt,
        int keyLength,
        int iterations = DefaultIterationCount)
    {
        if (string.IsNullOrEmpty(password))
        {
            throw new("Password cannot be null or empty");
        }
        if (salt is null or { Length: < 8 })
        {
            throw new("Salt must be at least 8 bytes");
        }
        if (iterations < 10_000)
        {
            throw new("Iteration count must be at least 10,000 for security");
        }
        return Rfc2898DeriveBytes.Pbkdf2(password,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            keyLength);
    }

    /// <summary>
    ///     <para xml:lang="en">Generates a cryptographically secure random salt</para>
    ///     <para xml:lang="zh">生成加密安全的随机盐值</para>
    /// </summary>
    /// <param name="length">Length of salt in bytes (default: 16)</param>
    /// <returns>Random salt bytes</returns>
    public static byte[] GenerateSalt(int length = DefaultSaltLength)
    {
        if (length < 8)
        {
            throw new("Salt length must be at least 8 bytes");
        }
        var salt = new byte[length];
        RandomNumberGenerator.Fill(salt);
        return salt;
    }

    /// <summary>
    ///     <para xml:lang="en">Generates a cryptographically secure random IV</para>
    ///     <para xml:lang="zh">生成加密安全的随机初始化向量 (IV)</para>
    /// </summary>
    /// <param name="blockSize">Block size in bytes (default: 16 for AES)</param>
    /// <returns>Random IV bytes</returns>
    public static byte[] GenerateIV(int blockSize = 16)
    {
        var iv = new byte[blockSize];
        RandomNumberGenerator.Fill(iv);
        return iv;
    }

    /// <summary>
    ///     <para xml:lang="en">Combines salt and encrypted data into a single array</para>
    ///     <para xml:lang="zh">将盐值和加密数据合并为单一数组</para>
    /// </summary>
    /// <param name="salt">Salt bytes</param>
    /// <param name="encryptedData">Encrypted data</param>
    /// <returns>Combined array: [salt length (4 bytes)][salt][encrypted data]</returns>
    public static byte[] CombineSaltAndData(byte[] salt, byte[] encryptedData)
    {
        var result = new byte[4 + salt.Length + encryptedData.Length];
        BitConverter.GetBytes(salt.Length).CopyTo(result, 0);
        salt.CopyTo(result, 4);
        encryptedData.CopyTo(result, 4 + salt.Length);
        return result;
    }

    /// <summary>
    ///     <para xml:lang="en">Extracts salt and encrypted data from a combined array</para>
    ///     <para xml:lang="zh">从合并的数组中提取盐值和加密数据</para>
    /// </summary>
    /// <param name="combinedData">Combined data array</param>
    /// <param name="salt">Output salt</param>
    /// <param name="encryptedData">Output encrypted data</param>
    public static void ExtractSaltAndData(ReadOnlySpan<byte> combinedData, out byte[] salt, out byte[] encryptedData)
    {
        if (combinedData.Length < 4)
        {
            throw new("Invalid combined data format");
        }
        var saltLength = BitConverter.ToInt32(combinedData[..4]);
        if (saltLength < 8 || 4 + saltLength > combinedData.Length)
        {
            throw new("Invalid salt length in combined data");
        }
        salt = combinedData.Slice(4, saltLength).ToArray();
        encryptedData = combinedData[(4 + saltLength)..].ToArray();
    }

    /// <summary>
    ///     <para xml:lang="en">Securely clears an array by filling it with zeros</para>
    ///     <para xml:lang="zh">通过填充零来安全清除数组</para>
    /// </summary>
    public static void SecureClear(byte[]? array)
    {
        if (array != null)
        {
            CryptographicOperations.ZeroMemory(array);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Securely clears a span by filling it with zeros</para>
    ///     <para xml:lang="zh">通过填充零来安全清除 Span</para>
    /// </summary>
    public static void SecureClear(Span<byte> span) => CryptographicOperations.ZeroMemory(span);

    /// <summary>
    ///     <para xml:lang="en">Converts bytes to hex string using ArrayPool for temporary storage</para>
    ///     <para xml:lang="zh">使用 ArrayPool 进行临时存储将字节转换为十六进制字符串</para>
    /// </summary>
    public static string ToHexStringOptimized(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
        {
            return string.Empty;
        }
        const int maxStackSize = 256;
        var hexLength = data.Length * 2;
        if (hexLength <= maxStackSize)
        {
            Span<char> hexChars = stackalloc char[hexLength];
            ConvertToHex(data, hexChars);
            return new(hexChars);
        }
        var rented = ArrayPool<byte>.Shared.Rent(hexLength * sizeof(char));
        try
        {
            var hexChars = MemoryMarshal.Cast<byte, char>(rented.AsSpan(0, hexLength * sizeof(char)));
            ConvertToHex(data, hexChars);
            return new(hexChars);
        }
        finally
        {
            SecureClear(rented);
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Converts hex string to bytes using ArrayPool</para>
    ///     <para xml:lang="zh">使用 ArrayPool 将十六进制字符串转换为字节</para>
    /// </summary>
    public static byte[] FromHexStringOptimized(string hex)
    {
        if (string.IsNullOrEmpty(hex))
        {
            return [];
        }
        if (hex.Length % 2 != 0)
        {
            throw new("Hex string length must be even");
        }
        var length = hex.Length / 2;
        var result = new byte[length];
        for (var i = 0; i < length; i++)
        {
            result[i] = (byte)((GetHexValue(hex[i * 2]) << 4) | GetHexValue(hex[(i * 2) + 1]));
        }
        return result;
    }

    private static int GetHexValue(char c) =>
        c switch
        {
            >= '0' and <= '9' => c - '0',
            >= 'A' and <= 'F' => (c - 'A') + 10,
            >= 'a' and <= 'f' => (c - 'a') + 10,
            _                 => throw new($"Invalid hex character: {c}")
        };

    private static void ConvertToHex(ReadOnlySpan<byte> data, Span<char> output)
    {
        const string hexChars = "0123456789ABCDEF";
        for (var i = 0; i < data.Length; i++)
        {
            output[i * 2] = hexChars[data[i] >> 4];
            output[(i * 2) + 1] = hexChars[data[i] & 0xF];
        }
    }
}