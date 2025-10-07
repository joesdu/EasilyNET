using System.Text;
using Org.BouncyCastle.Utilities.Encoders;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ReturnTypeCanBeEnumerable.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.Security;

/// <summary>
///     <para xml:lang="en">SM4 Encryption - SM4 is a block cipher with 128-bit key and 128-bit block size</para>
///     <para xml:lang="zh">SM4加密 - SM4是一种分组密码算法,密钥长度和分组长度均为128位</para>
/// </summary>
public static class Sm4Crypt
{
    private const int KeySize = 16;   // 128 bits = 16 bytes
    private const int BlockSize = 16; // 128 bits = 16 bytes

    /// <summary>
    ///     <para xml:lang="en">Validates and decodes the secret key</para>
    ///     <para xml:lang="zh">验证并解码密钥</para>
    /// </summary>
    private static byte[] ValidateAndDecodeKey(string secretKey, bool hexString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secretKey);
        var keyBytes = hexString ? Hex.Decode(secretKey) : Encoding.UTF8.GetBytes(secretKey);
        return keyBytes.Length != KeySize ? throw new ArgumentException($"Invalid key length. SM4 requires a {KeySize * 8}-bit ({KeySize} bytes) key. Provided key is {keyBytes.Length} bytes.", nameof(secretKey)) : keyBytes;
    }

    /// <summary>
    ///     <para xml:lang="en">Validates and decodes the initialization vector (IV)</para>
    ///     <para xml:lang="zh">验证并解码初始化向量(IV)</para>
    /// </summary>
    private static byte[] ValidateAndDecodeIV(string iv, bool hexString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(iv);
        var ivBytes = hexString ? Hex.Decode(iv) : Encoding.UTF8.GetBytes(iv);
        return ivBytes.Length != BlockSize ? throw new ArgumentException($"Invalid IV length. SM4 CBC mode requires a {BlockSize * 8}-bit ({BlockSize} bytes) IV. Provided IV is {ivBytes.Length} bytes.", nameof(iv)) : ivBytes;
    }

    /// <summary>
    ///     <para xml:lang="en">Encrypt using ECB mode</para>
    ///     <para xml:lang="zh">加密ECB模式</para>
    /// </summary>
    /// <param name="secretKey">
    ///     <para xml:lang="en">Secret key (must be 16 bytes or 32 hex characters)</para>
    ///     <para xml:lang="zh">密钥(必须是16字节或32个十六进制字符)</para>
    /// </param>
    /// <param name="hexString">
    ///     <para xml:lang="en">Whether the key is in hexadecimal format</para>
    ///     <para xml:lang="zh">密钥是否是十六进制格式</para>
    /// </param>
    /// <param name="plainText">
    ///     <para xml:lang="en">Plain text in binary format</para>
    ///     <para xml:lang="zh">二进制格式加密的内容</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Encrypted data as byte array</para>
    ///     <para xml:lang="zh">加密后的字节数组</para>
    /// </returns>
    public static byte[] EncryptECB(string secretKey, bool hexString, ReadOnlySpan<byte> plainText)
    {
        var keyBytes = ValidateAndDecodeKey(secretKey, hexString);
        var ctx = new Sm4Context
        {
            IsPadding = true,
            Mode = ESm4Model.Encrypt
        };
        var sm4 = new Sm4();
        sm4.SetKeyEnc(ctx, keyBytes);
        return sm4.ECB(ctx, plainText);
    }

    /// <summary>
    ///     <para xml:lang="en">Encrypt a string using ECB mode</para>
    ///     <para xml:lang="zh">使用ECB模式加密字符串</para>
    /// </summary>
    /// <param name="secretKey">
    ///     <para xml:lang="en">Secret key (must be 16 bytes or 32 hex characters)</para>
    ///     <para xml:lang="zh">密钥(必须是16字节或32个十六进制字符)</para>
    /// </param>
    /// <param name="hexString">
    ///     <para xml:lang="en">Whether the key is in hexadecimal format</para>
    ///     <para xml:lang="zh">密钥是否是十六进制格式</para>
    /// </param>
    /// <param name="plainText">
    ///     <para xml:lang="en">Plain text string to encrypt</para>
    ///     <para xml:lang="zh">要加密的明文字符串</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Encrypted data as byte array</para>
    ///     <para xml:lang="zh">加密后的字节数组</para>
    /// </returns>
    public static byte[] EncryptECB(string secretKey, bool hexString, string plainText)
    {
        ArgumentException.ThrowIfNullOrEmpty(plainText);
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        return EncryptECB(secretKey, hexString, plainBytes);
    }

    /// <summary>
    ///     <para xml:lang="en">Encrypt a string using ECB mode and return as hexadecimal string</para>
    ///     <para xml:lang="zh">使用ECB模式加密字符串并返回十六进制字符串</para>
    /// </summary>
    /// <param name="secretKey">
    ///     <para xml:lang="en">Secret key (must be 16 bytes or 32 hex characters)</para>
    ///     <para xml:lang="zh">密钥(必须是16字节或32个十六进制字符)</para>
    /// </param>
    /// <param name="hexString">
    ///     <para xml:lang="en">Whether the key is in hexadecimal format</para>
    ///     <para xml:lang="zh">密钥是否是十六进制格式</para>
    /// </param>
    /// <param name="plainText">
    ///     <para xml:lang="en">Plain text string to encrypt</para>
    ///     <para xml:lang="zh">要加密的明文字符串</para>
    /// </param>
    /// <param name="upperCase">
    ///     <para xml:lang="en">Whether to return uppercase hexadecimal string (default: true)</para>
    ///     <para xml:lang="zh">是否返回大写的十六进制字符串(默认: true)</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Encrypted data as hexadecimal string</para>
    ///     <para xml:lang="zh">加密后的十六进制字符串</para>
    /// </returns>
    public static string EncryptECBToHex(string secretKey, bool hexString, string plainText, bool upperCase = true)
    {
        var encrypted = EncryptECB(secretKey, hexString, plainText);
        var hex = Convert.ToHexString(encrypted);
        return upperCase ? hex : hex.ToLowerInvariant();
    }

    /// <summary>
    ///     <para xml:lang="en">Encrypt a string using ECB mode and return as Base64 string</para>
    ///     <para xml:lang="zh">使用ECB模式加密字符串并返回Base64字符串</para>
    /// </summary>
    /// <param name="secretKey">
    ///     <para xml:lang="en">Secret key (must be 16 bytes or 32 hex characters)</para>
    ///     <para xml:lang="zh">密钥(必须是16字节或32个十六进制字符)</para>
    /// </param>
    /// <param name="hexString">
    ///     <para xml:lang="en">Whether the key is in hexadecimal format</para>
    ///     <para xml:lang="zh">密钥是否是十六进制格式</para>
    /// </param>
    /// <param name="plainText">
    ///     <para xml:lang="en">Plain text string to encrypt</para>
    ///     <para xml:lang="zh">要加密的明文字符串</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Encrypted data as Base64 string</para>
    ///     <para xml:lang="zh">加密后的Base64字符串</para>
    /// </returns>
    public static string EncryptECBToBase64(string secretKey, bool hexString, string plainText)
    {
        var encrypted = EncryptECB(secretKey, hexString, plainText);
        return Convert.ToBase64String(encrypted);
    }

    /// <summary>
    ///     <para xml:lang="en">Decrypt using ECB mode</para>
    ///     <para xml:lang="zh">解密ECB模式</para>
    /// </summary>
    /// <param name="secretKey">
    ///     <para xml:lang="en">Secret key (must be 16 bytes or 32 hex characters)</para>
    ///     <para xml:lang="zh">密钥(必须是16字节或32个十六进制字符)</para>
    /// </param>
    /// <param name="hexString">
    ///     <para xml:lang="en">Whether the key is in hexadecimal format</para>
    ///     <para xml:lang="zh">密钥是否是十六进制格式</para>
    /// </param>
    /// <param name="cipherBytes">
    ///     <para xml:lang="en">Ciphertext in binary format</para>
    ///     <para xml:lang="zh">二进制格式密文</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Decrypted data as byte array</para>
    ///     <para xml:lang="zh">解密后的字节数组</para>
    /// </returns>
    public static byte[] DecryptECB(string secretKey, bool hexString, ReadOnlySpan<byte> cipherBytes)
    {
        var keyBytes = ValidateAndDecodeKey(secretKey, hexString);
        var ctx = new Sm4Context
        {
            IsPadding = true,
            Mode = ESm4Model.Decrypt
        };
        var sm4 = new Sm4();
        sm4.SetKeyDec(ctx, keyBytes);
        return sm4.ECB(ctx, cipherBytes);
    }

    /// <summary>
    ///     <para xml:lang="en">Decrypt hexadecimal string using ECB mode</para>
    ///     <para xml:lang="zh">使用ECB模式解密十六进制字符串</para>
    /// </summary>
    /// <param name="secretKey">
    ///     <para xml:lang="en">Secret key (must be 16 bytes or 32 hex characters)</para>
    ///     <para xml:lang="zh">密钥(必须是16字节或32个十六进制字符)</para>
    /// </param>
    /// <param name="hexString">
    ///     <para xml:lang="en">Whether the key is in hexadecimal format</para>
    ///     <para xml:lang="zh">密钥是否是十六进制格式</para>
    /// </param>
    /// <param name="cipherHex">
    ///     <para xml:lang="en">Ciphertext as hexadecimal string</para>
    ///     <para xml:lang="zh">十六进制格式的密文</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Decrypted string</para>
    ///     <para xml:lang="zh">解密后的字符串</para>
    /// </returns>
    public static string DecryptECBFromHex(string secretKey, bool hexString, string cipherHex)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cipherHex);
        var cipherBytes = Convert.FromHexString(cipherHex);
        var decrypted = DecryptECB(secretKey, hexString, cipherBytes);
        return Encoding.UTF8.GetString(decrypted);
    }

    /// <summary>
    ///     <para xml:lang="en">Decrypt Base64 string using ECB mode</para>
    ///     <para xml:lang="zh">使用ECB模式解密Base64字符串</para>
    /// </summary>
    /// <param name="secretKey">
    ///     <para xml:lang="en">Secret key (must be 16 bytes or 32 hex characters)</para>
    ///     <para xml:lang="zh">密钥(必须是16字节或32个十六进制字符)</para>
    /// </param>
    /// <param name="hexString">
    ///     <para xml:lang="en">Whether the key is in hexadecimal format</para>
    ///     <para xml:lang="zh">密钥是否是十六进制格式</para>
    /// </param>
    /// <param name="cipherBase64">
    ///     <para xml:lang="en">Ciphertext as Base64 string</para>
    ///     <para xml:lang="zh">Base64格式的密文</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Decrypted string</para>
    ///     <para xml:lang="zh">解密后的字符串</para>
    /// </returns>
    public static string DecryptECBFromBase64(string secretKey, bool hexString, string cipherBase64)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cipherBase64);
        var cipherBytes = Convert.FromBase64String(cipherBase64);
        var decrypted = DecryptECB(secretKey, hexString, cipherBytes);
        return Encoding.UTF8.GetString(decrypted);
    }

    /// <summary>
    ///     <para xml:lang="en">Encrypt using CBC mode</para>
    ///     <para xml:lang="zh">加密CBC模式</para>
    /// </summary>
    /// <param name="secretKey">
    ///     <para xml:lang="en">Secret key (must be 16 bytes or 32 hex characters)</para>
    ///     <para xml:lang="zh">密钥(必须是16字节或32个十六进制字符)</para>
    /// </param>
    /// <param name="hexString">
    ///     <para xml:lang="en">Whether the key and IV are in hexadecimal format</para>
    ///     <para xml:lang="zh">密钥和IV是否是十六进制格式</para>
    /// </param>
    /// <param name="iv">
    ///     <para xml:lang="en">Initialization vector (must be 16 bytes or 32 hex characters)</para>
    ///     <para xml:lang="zh">初始化向量(必须是16字节或32个十六进制字符)</para>
    /// </param>
    /// <param name="plainText">
    ///     <para xml:lang="en">Plain text in binary format</para>
    ///     <para xml:lang="zh">二进制格式明文</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Encrypted data as byte array</para>
    ///     <para xml:lang="zh">加密后的字节数组</para>
    /// </returns>
    public static byte[] EncryptCBC(string secretKey, bool hexString, string iv, ReadOnlySpan<byte> plainText)
    {
        var keyBytes = ValidateAndDecodeKey(secretKey, hexString);
        var ivBytes = ValidateAndDecodeIV(iv, hexString);
        var ctx = new Sm4Context
        {
            IsPadding = true,
            Mode = ESm4Model.Encrypt
        };
        var sm4 = new Sm4();
        sm4.SetKeyEnc(ctx, keyBytes);
        return sm4.CBC(ctx, ivBytes, plainText);
    }

    /// <summary>
    ///     <para xml:lang="en">Encrypt a string using CBC mode</para>
    ///     <para xml:lang="zh">使用CBC模式加密字符串</para>
    /// </summary>
    /// <param name="secretKey">
    ///     <para xml:lang="en">Secret key (must be 16 bytes or 32 hex characters)</para>
    ///     <para xml:lang="zh">密钥(必须是16字节或32个十六进制字符)</para>
    /// </param>
    /// <param name="hexString">
    ///     <para xml:lang="en">Whether the key and IV are in hexadecimal format</para>
    ///     <para xml:lang="zh">密钥和IV是否是十六进制格式</para>
    /// </param>
    /// <param name="iv">
    ///     <para xml:lang="en">Initialization vector (must be 16 bytes or 32 hex characters)</para>
    ///     <para xml:lang="zh">初始化向量(必须是16字节或32个十六进制字符)</para>
    /// </param>
    /// <param name="plainText">
    ///     <para xml:lang="en">Plain text string to encrypt</para>
    ///     <para xml:lang="zh">要加密的明文字符串</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Encrypted data as byte array</para>
    ///     <para xml:lang="zh">加密后的字节数组</para>
    /// </returns>
    public static byte[] EncryptCBC(string secretKey, bool hexString, string iv, string plainText)
    {
        ArgumentException.ThrowIfNullOrEmpty(plainText);
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        return EncryptCBC(secretKey, hexString, iv, plainBytes);
    }

    /// <summary>
    ///     <para xml:lang="en">Encrypt a string using CBC mode and return as hexadecimal string</para>
    ///     <para xml:lang="zh">使用CBC模式加密字符串并返回十六进制字符串</para>
    /// </summary>
    /// <param name="secretKey">
    ///     <para xml:lang="en">Secret key (must be 16 bytes or 32 hex characters)</para>
    ///     <para xml:lang="zh">密钥(必须是16字节或32个十六进制字符)</para>
    /// </param>
    /// <param name="hexString">
    ///     <para xml:lang="en">Whether the key and IV are in hexadecimal format</para>
    ///     <para xml:lang="zh">密钥和IV是否是十六进制格式</para>
    /// </param>
    /// <param name="iv">
    ///     <para xml:lang="en">Initialization vector (must be 16 bytes or 32 hex characters)</para>
    ///     <para xml:lang="zh">初始化向量(必须是16字节或32个十六进制字符)</para>
    /// </param>
    /// <param name="plainText">
    ///     <para xml:lang="en">Plain text string to encrypt</para>
    ///     <para xml:lang="zh">要加密的明文字符串</para>
    /// </param>
    /// <param name="upperCase">
    ///     <para xml:lang="en">Whether to return uppercase hexadecimal string (default: true)</para>
    ///     <para xml:lang="zh">是否返回大写的十六进制字符串(默认: true)</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Encrypted data as hexadecimal string</para>
    ///     <para xml:lang="zh">加密后的十六进制字符串</para>
    /// </returns>
    public static string EncryptCBCToHex(string secretKey, bool hexString, string iv, string plainText, bool upperCase = true)
    {
        var encrypted = EncryptCBC(secretKey, hexString, iv, plainText);
        var hex = Convert.ToHexString(encrypted);
        return upperCase ? hex : hex.ToLowerInvariant();
    }

    /// <summary>
    ///     <para xml:lang="en">Encrypt a string using CBC mode and return as Base64 string</para>
    ///     <para xml:lang="zh">使用CBC模式加密字符串并返回Base64字符串</para>
    /// </summary>
    /// <param name="secretKey">
    ///     <para xml:lang="en">Secret key (must be 16 bytes or 32 hex characters)</para>
    ///     <para xml:lang="zh">密钥(必须是16字节或32个十六进制字符)</para>
    /// </param>
    /// <param name="hexString">
    ///     <para xml:lang="en">Whether the key and IV are in hexadecimal format</para>
    ///     <para xml:lang="zh">密钥和IV是否是十六进制格式</para>
    /// </param>
    /// <param name="iv">
    ///     <para xml:lang="en">Initialization vector (must be 16 bytes or 32 hex characters)</para>
    ///     <para xml:lang="zh">初始化向量(必须是16字节或32个十六进制字符)</para>
    /// </param>
    /// <param name="plainText">
    ///     <para xml:lang="en">Plain text string to encrypt</para>
    ///     <para xml:lang="zh">要加密的明文字符串</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Encrypted data as Base64 string</para>
    ///     <para xml:lang="zh">加密后的Base64字符串</para>
    /// </returns>
    public static string EncryptCBCToBase64(string secretKey, bool hexString, string iv, string plainText)
    {
        var encrypted = EncryptCBC(secretKey, hexString, iv, plainText);
        return Convert.ToBase64String(encrypted);
    }

    /// <summary>
    ///     <para xml:lang="en">Decrypt using CBC mode</para>
    ///     <para xml:lang="zh">解密CBC模式</para>
    /// </summary>
    /// <param name="secretKey">
    ///     <para xml:lang="en">Secret key (must be 16 bytes or 32 hex characters)</para>
    ///     <para xml:lang="zh">密钥(必须是16字节或32个十六进制字符)</para>
    /// </param>
    /// <param name="hexString">
    ///     <para xml:lang="en">Whether the key and IV are in hexadecimal format</para>
    ///     <para xml:lang="zh">密钥和IV是否是十六进制格式</para>
    /// </param>
    /// <param name="iv">
    ///     <para xml:lang="en">Initialization vector (must be 16 bytes or 32 hex characters)</para>
    ///     <para xml:lang="zh">初始化向量(必须是16字节或32个十六进制字符)</para>
    /// </param>
    /// <param name="cipherText">
    ///     <para xml:lang="en">Ciphertext in binary format</para>
    ///     <para xml:lang="zh">二进制格式密文</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Decrypted data as byte array</para>
    ///     <para xml:lang="zh">解密后的字节数组</para>
    /// </returns>
    public static byte[] DecryptCBC(string secretKey, bool hexString, string iv, ReadOnlySpan<byte> cipherText)
    {
        var keyBytes = ValidateAndDecodeKey(secretKey, hexString);
        var ivBytes = ValidateAndDecodeIV(iv, hexString);
        var ctx = new Sm4Context
        {
            IsPadding = true,
            Mode = ESm4Model.Decrypt
        };
        var sm4 = new Sm4();
        sm4.SetKeyDec(ctx, keyBytes);
        return sm4.CBC(ctx, ivBytes, cipherText);
    }

    /// <summary>
    ///     <para xml:lang="en">Decrypt hexadecimal string using CBC mode</para>
    ///     <para xml:lang="zh">使用CBC模式解密十六进制字符串</para>
    /// </summary>
    /// <param name="secretKey">
    ///     <para xml:lang="en">Secret key (must be 16 bytes or 32 hex characters)</para>
    ///     <para xml:lang="zh">密钥(必须是16字节或32个十六进制字符)</para>
    /// </param>
    /// <param name="hexString">
    ///     <para xml:lang="en">Whether the key and IV are in hexadecimal format</para>
    ///     <para xml:lang="zh">密钥和IV是否是十六进制格式</para>
    /// </param>
    /// <param name="iv">
    ///     <para xml:lang="en">Initialization vector (must be 16 bytes or 32 hex characters)</para>
    ///     <para xml:lang="zh">初始化向量(必须是16字节或32个十六进制字符)</para>
    /// </param>
    /// <param name="cipherHex">
    ///     <para xml:lang="en">Ciphertext as hexadecimal string</para>
    ///     <para xml:lang="zh">十六进制格式的密文</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Decrypted string</para>
    ///     <para xml:lang="zh">解密后的字符串</para>
    /// </returns>
    public static string DecryptCBCFromHex(string secretKey, bool hexString, string iv, string cipherHex)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cipherHex);
        var cipherBytes = Convert.FromHexString(cipherHex);
        var decrypted = DecryptCBC(secretKey, hexString, iv, cipherBytes);
        return Encoding.UTF8.GetString(decrypted);
    }

    /// <summary>
    ///     <para xml:lang="en">Decrypt Base64 string using CBC mode</para>
    ///     <para xml:lang="zh">使用CBC模式解密Base64字符串</para>
    /// </summary>
    /// <param name="secretKey">
    ///     <para xml:lang="en">Secret key (must be 16 bytes or 32 hex characters)</para>
    ///     <para xml:lang="zh">密钥(必须是16字节或32个十六进制字符)</para>
    /// </param>
    /// <param name="hexString">
    ///     <para xml:lang="en">Whether the key and IV are in hexadecimal format</para>
    ///     <para xml:lang="zh">密钥和IV是否是十六进制格式</para>
    /// </param>
    /// <param name="iv">
    ///     <para xml:lang="en">Initialization vector (must be 16 bytes or 32 hex characters)</para>
    ///     <para xml:lang="zh">初始化向量(必须是16字节或32个十六进制字符)</para>
    /// </param>
    /// <param name="cipherBase64">
    ///     <para xml:lang="en">Ciphertext as Base64 string</para>
    ///     <para xml:lang="zh">Base64格式的密文</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Decrypted string</para>
    ///     <para xml:lang="zh">解密后的字符串</para>
    /// </returns>
    public static string DecryptCBCFromBase64(string secretKey, bool hexString, string iv, string cipherBase64)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cipherBase64);
        var cipherBytes = Convert.FromBase64String(cipherBase64);
        var decrypted = DecryptCBC(secretKey, hexString, iv, cipherBytes);
        return Encoding.UTF8.GetString(decrypted);
    }
}