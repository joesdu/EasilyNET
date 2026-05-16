using System.Buffers;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

// ReSharper disable UnusedMember.Global

// ReSharper disable UnusedType.Global
// ReSharper disable MemberCanBePrivate.Global

namespace EasilyNET.Security;

/// <summary>
///     <para xml:lang="en">AES encryption/decryption with PBKDF2 key derivation and salt support</para>
///     <para xml:lang="zh">支持 PBKDF2 密钥派生和盐值的 AES 加密/解密</para>
/// </summary>
public static class AesCrypt
{
    // PBKDF2 参数
    private const int SaltLength = 16; // 128-bit salt
    private const int IvLength = 16;   // 128-bit IV

    // 密钥长度映射
    private static int GetKeyLength(AesKeyModel model) =>
        model switch
        {
            AesKeyModel.AES128 => 16,
            AesKeyModel.AES192 => 24,
            _                  => 32
        };

    // AES block size = 16 bytes, PKCS7 padding ensures output is block-aligned
    // Using bit-shift: ((n / 16) + 1) * 16 = ((n >> 4) + 1) << 4
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetCipherTextLength(int dataLength) => ((dataLength >> 4) + 1) << 4;

    #region 验证方法

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ValidateInput(ReadOnlySpan<byte> data, string password)
    {
        if (data.IsEmpty)
        {
            throw new ArgumentException("Data cannot be empty.", nameof(data));
        }
        if (string.IsNullOrEmpty(password))
        {
            throw new ArgumentNullException(nameof(password), "Password cannot be null or empty.");
        }
    }

    #endregion

    #region 基础字节加密

    /// <summary>
    ///     <para xml:lang="en">Encrypts data using AES with PBKDF2 key derivation (includes salt and IV)</para>
    ///     <para xml:lang="zh">使用 AES 加密数据，使用 PBKDF2 密钥派生（包含盐值和 IV）</para>
    /// </summary>
    public static byte[] Encrypt(ReadOnlySpan<byte> data, string password, AesKeyModel model = AesKeyModel.AES256)
    {
        ValidateInput(data, password);
        var keyLength = GetKeyLength(model);
        var salt = CryptographicUtilities.GenerateSalt();
        var iv = CryptographicUtilities.GenerateIV();
        var key = CryptographicUtilities.DeriveKey(password, salt, keyLength);
        var cipherTextLength = GetCipherTextLength(data.Length);
        var poolBuffer = ArrayPool<byte>.Shared.Rent(SaltLength + IvLength + cipherTextLength);
        try
        {
            // 将 salt 和 IV 写入池化缓冲区的开头
            salt.CopyTo(poolBuffer, 0);
            iv.CopyTo(poolBuffer, SaltLength);

            // 使用 MemoryStream 直接包装池化缓冲区切片（从 IV 之后开始），避免 ms.ToArray() 额外分配
            using var ms = new MemoryStream(poolBuffer, SaltLength + IvLength, cipherTextLength);
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            using var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write);
            cs.Write(data);
            cs.FlushFinalBlock();
            // ms.Length 现在是加密数据长度（不含 salt+IV 前缀）

            // 组合输出：[salt][iv][encryptedData]
            var result = new byte[SaltLength + IvLength + ms.Length];
            Buffer.BlockCopy(poolBuffer, 0, result, 0, SaltLength + IvLength + (int)ms.Length);
            return result;
        }
        finally
        {
            CryptographicUtilities.SecureClear(key);
            ArrayPool<byte>.Shared.Return(poolBuffer, true);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Decrypts data using AES with PBKDF2 key derivation</para>
    ///     <para xml:lang="zh">使用 AES 解密数据，使用 PBKDF2 密钥派生</para>
    /// </summary>
    public static byte[] Decrypt(ReadOnlySpan<byte> encryptedData,
        string password,
        AesKeyModel model = AesKeyModel.AES256)
    {
        ValidateInput(encryptedData, password);
        if (encryptedData.Length < SaltLength + IvLength + 16) // 最小长度检查
        {
            throw new CryptographicException("Invalid encrypted data format: data is too short.");
        }
        var keyLength = GetKeyLength(model);
        var salt = encryptedData[..SaltLength].ToArray();
        var iv = encryptedData[SaltLength..(SaltLength + IvLength)].ToArray();
        var cipherData = encryptedData[(SaltLength + IvLength)..];
        var key = CryptographicUtilities.DeriveKey(password, salt, keyLength);
        try
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            var decryptor = aes.CreateDecryptor();
            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Write))
            {
                cs.Write(cipherData);
                cs.FlushFinalBlock();
            }
            return ms.ToArray();
        }
        catch (CryptographicException ex)
        {
            throw new CryptographicException("Decryption failed. Check password and data integrity.", ex);
        }
        finally
        {
            CryptographicUtilities.SecureClear(key);
        }
    }

    #endregion

    #region 字符串加密 (Base64/Hex)

    /// <summary>
    ///     <para xml:lang="en">Encrypts string to Base64 using AES-256</para>
    ///     <para xml:lang="zh">将字符串加密为 Base64，使用 AES-256</para>
    /// </summary>
    public static string EncryptToBase64(string content, string password)
    {
        if (string.IsNullOrEmpty(content))
        {
            return string.Empty;
        }
        var data = Encoding.UTF8.GetBytes(content);
        var encrypted = Encrypt(data, password);
        return Convert.ToBase64String(encrypted);
    }

    /// <summary>
    ///     <para xml:lang="en">Decrypts Base64 string using AES-256</para>
    ///     <para xml:lang="zh">从 Base64 解密字符串，使用 AES-256</para>
    /// </summary>
    public static string DecryptFromBase64(string base64Content, string password)
    {
        if (string.IsNullOrEmpty(base64Content))
        {
            return string.Empty;
        }
        var encryptedData = Convert.FromBase64String(base64Content);
        var decrypted = Decrypt(encryptedData, password);
        return Encoding.UTF8.GetString(decrypted);
    }

    /// <summary>
    ///     <para xml:lang="en">Encrypts string to Hex using AES-256</para>
    ///     <para xml:lang="zh">将字符串加密为十六进制，使用 AES-256</para>
    /// </summary>
    public static string EncryptToHex(string content, string password)
    {
        if (string.IsNullOrEmpty(content))
        {
            return string.Empty;
        }
        var data = Encoding.UTF8.GetBytes(content);
        var encrypted = Encrypt(data, password);
        return CryptographicUtilities.ToHexStringOptimized(encrypted);
    }

    /// <summary>
    ///     <para xml:lang="en">Decrypts Hex string using AES-256</para>
    ///     <para xml:lang="zh">从十六进制解密字符串，使用 AES-256</para>
    /// </summary>
    public static string DecryptFromHex(string hexContent, string password)
    {
        if (string.IsNullOrEmpty(hexContent))
        {
            return string.Empty;
        }
        var encryptedData = CryptographicUtilities.FromHexStringOptimized(hexContent);
        var decrypted = Decrypt(encryptedData, password);
        return Encoding.UTF8.GetString(decrypted);
    }

    #endregion

    #region 异步 API

    /// <summary>
    ///     <para xml:lang="en">Asynchronously encrypts stream data</para>
    ///     <para xml:lang="zh">异步加密流数据</para>
    /// </summary>
    public static async Task EncryptAsync(
        Stream input,
        Stream output,
        string password,
        AesKeyModel model = AesKeyModel.AES256,
        CancellationToken cancellationToken = default)
    {
        if (input == null || output == null)
        {
            throw new ArgumentNullException(input == null ? nameof(input) : nameof(output), "Input and output streams cannot be null.");
        }
        if (string.IsNullOrEmpty(password))
        {
            throw new ArgumentNullException(nameof(password), "Password cannot be null or empty.");
        }
        var keyLength = GetKeyLength(model);
        var salt = CryptographicUtilities.GenerateSalt();
        var iv = CryptographicUtilities.GenerateIV();
        var key = CryptographicUtilities.DeriveKey(password, salt, keyLength);
        try
        {
            // 写入 salt 和 IV
            await output.WriteAsync(salt, cancellationToken);
            await output.WriteAsync(iv, cancellationToken);
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            await using var cs = new CryptoStream(output, aes.CreateEncryptor(), CryptoStreamMode.Write);
            await input.CopyToAsync(cs, cancellationToken);
            await cs.FlushFinalBlockAsync(cancellationToken);
        }
        finally
        {
            CryptographicUtilities.SecureClear(key);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Asynchronously decrypts stream data</para>
    ///     <para xml:lang="zh">异步解密流数据</para>
    /// </summary>
    public static async Task DecryptAsync(
        Stream input,
        Stream output,
        string password,
        AesKeyModel model = AesKeyModel.AES256,
        CancellationToken cancellationToken = default)
    {
        if (input == null || output == null)
        {
            throw new ArgumentNullException(input == null ? nameof(input) : nameof(output), "Input and output streams cannot be null.");
        }
        if (string.IsNullOrEmpty(password))
        {
            throw new ArgumentNullException(nameof(password), "Password cannot be null or empty.");
        }
        var keyLength = GetKeyLength(model);

        // 读取 salt 和 IV
        var salt = new byte[SaltLength];
        var iv = new byte[IvLength];
        if (await input.ReadAsync(salt.AsMemory(0, SaltLength), cancellationToken) != SaltLength)
        {
            throw new CryptographicException("Failed to read salt from input stream: stream is too short.");
        }
        if (await input.ReadAsync(iv.AsMemory(0, IvLength), cancellationToken) != IvLength)
        {
            throw new CryptographicException("Failed to read IV from input stream: stream is too short.");
        }
        var key = CryptographicUtilities.DeriveKey(password, salt, keyLength);
        try
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            await using var cs = new CryptoStream(input, aes.CreateDecryptor(), CryptoStreamMode.Read);
            await cs.CopyToAsync(output, cancellationToken);
        }
        catch (CryptographicException ex)
        {
            throw new CryptographicException("Decryption failed. Check password and data integrity.", ex);
        }
        finally
        {
            CryptographicUtilities.SecureClear(key);
        }
    }

    #endregion
}