# AES 加密解密使用指南

## 功能特性

本库提供了完整的 AES 加密解密功能,支持:

- **密钥长度**: AES128 (128位), AES192 (192位), AES256 (256位)
- **运算模式**: CBC, ECB, CFB, OFB, CTS (通过 CipherMode 枚举支持)
- **填充模式**: PKCS7, Zeros, ANSIX923, ISO10126, None (通过 PaddingMode 枚举支持)
- **编码格式**: Base64, Hex (十六进制), 原始字节数组
- **字符编码**: UTF8, ASCII, Unicode 等 (可自定义)

## 基本用法

### 1. 字节数组加密/解密

```csharp
using EasilyNET.Security;

// 准备要加密的数据
var data = "敏感数据"u8.ToArray();
var password = "my-secure-password";

// 加密 - 使用 AES256, CBC 模式, PKCS7 填充
var encrypted = AesCrypt.Encrypt(data, password, AesKeyModel.AES256);

// 解密
var decrypted = AesCrypt.Decrypt(encrypted, password, AesKeyModel.AES256);
var result = Encoding.UTF8.GetString(decrypted); // "敏感数据"
```

### 2. 字符串加密/解密 (Base64 格式)

```csharp
using EasilyNET.Security;

var content = "中国abc";
var password = "12345678";

// 加密为 Base64 字符串
var encrypted = AesCrypt.EncryptToBase64(content, password, AesKeyModel.AES256);

// 从 Base64 字符串解密
var decrypted = AesCrypt.DecryptFromBase64(encrypted, password, AesKeyModel.AES256);
Console.WriteLine(decrypted); // 输出: 中国abc
```

### 3. 字符串加密/解密 (Hex 格式)

```csharp
using EasilyNET.Security;

var content = "中国abc";
var password = "12345678";

// 加密为十六进制字符串
var encrypted = AesCrypt.EncryptToHex(content, password, AesKeyModel.AES256);

// 从十六进制字符串解密
var decrypted = AesCrypt.DecryptFromHex(encrypted, password, AesKeyModel.AES256);
Console.WriteLine(decrypted); // 输出: 中国abc
```

## 高级用法

### 使用不同的密钥长度

```csharp
// AES128 - 128位密钥
var encrypted128 = AesCrypt.EncryptToBase64(content, password, AesKeyModel.AES128);

// AES192 - 192位密钥
var encrypted192 = AesCrypt.EncryptToBase64(content, password, AesKeyModel.AES192);

// AES256 - 256位密钥 (最安全)
var encrypted256 = AesCrypt.EncryptToBase64(content, password, AesKeyModel.AES256);
```

### 使用不同的运算模式

```csharp
// ECB 模式 - 最简单但安全性较低
var encryptedECB = AesCrypt.EncryptToBase64(
    content, password, AesKeyModel.AES256,
    mode: CipherMode.ECB
);

// CBC 模式 - 默认,安全性较好
var encryptedCBC = AesCrypt.EncryptToBase64(
    content, password, AesKeyModel.AES256,
    mode: CipherMode.CBC
);

// CFB 模式 - 流密码模式
var encryptedCFB = AesCrypt.EncryptToBase64(
    content, password, AesKeyModel.AES256,
    mode: CipherMode.CFB
);
```

### 使用不同的填充模式

```csharp
// PKCS7 填充 - 默认,最常用
var encrypted = AesCrypt.EncryptToBase64(
    content, password, AesKeyModel.AES256,
    padding: PaddingMode.PKCS7
);

// Zeros 填充
var encryptedZeros = AesCrypt.EncryptToBase64(
    content, password, AesKeyModel.AES256,
    padding: PaddingMode.Zeros
);

// ANSIX923 填充
var encryptedANSI = AesCrypt.EncryptToBase64(
    content, password, AesKeyModel.AES256,
    padding: PaddingMode.ANSIX923
);
```

### 指定字符编码

```csharp
// 使用 UTF8 编码 (默认)
var encrypted = AesCrypt.EncryptToBase64(
    content, password, AesKeyModel.AES256,
    encoding: Encoding.UTF8
);

// 使用 ASCII 编码
var encryptedASCII = AesCrypt.EncryptToBase64(
    content, password, AesKeyModel.AES256,
    encoding: Encoding.ASCII
);

// 使用 Unicode 编码
var encryptedUnicode = AesCrypt.EncryptToBase64(
    content, password, AesKeyModel.AES256,
    encoding: Encoding.Unicode
);
```

## 完整示例

```csharp
using System.Security.Cryptography;
using System.Text;
using EasilyNET.Security;

// 综合示例
var plainText = "这是一段需要加密的敏感信息";
var password = "MyStrongPassword!@#";

// 加密: AES256 + CBC + PKCS7 + Base64
var encrypted = AesCrypt.EncryptToBase64(
    plainText,
    password,
    AesKeyModel.AES256,
    mode: CipherMode.CBC,
    padding: PaddingMode.PKCS7,
    encoding: Encoding.UTF8
);

Console.WriteLine($"加密结果 (Base64): {encrypted}");

// 解密
var decrypted = AesCrypt.DecryptFromBase64(
    encrypted,
    password,
    AesKeyModel.AES256,
    mode: CipherMode.CBC,
    padding: PaddingMode.PKCS7,
    encoding: Encoding.UTF8
);

Console.WriteLine($"解密结果: {decrypted}");

// 使用 Hex 格式
var encryptedHex = AesCrypt.EncryptToHex(
    plainText,
    password,
    AesKeyModel.AES256
);

Console.WriteLine($"加密结果 (Hex): {encryptedHex}");

var decryptedHex = AesCrypt.DecryptFromHex(
    encryptedHex,
    password,
    AesKeyModel.AES256
);

Console.WriteLine($"解密结果: {decryptedHex}");
```

## 重要说明

### 1. 密钥处理
本库对密钥进行了哈希算法处理,因此:
- ✅ 使用本库加密的数据**必须**使用本库解密
- ❌ 与其他 AES 实现**不兼容**
- ✅ 相同密码在不同模式(AES128/192/256)下生成不同的密钥

### 2. 安全建议
- 推荐使用 **AES256** (最高安全性)
- 推荐使用 **CBC 或 CFB** 模式
- 推荐使用 **PKCS7** 填充
- 密码应足够复杂和随机
- 定期更换密钥

### 3. 运算模式说明

| 模式 | 特点 | 适用场景 |
|------|------|----------|
| CBC | 安全性好,不能并行加密 | 通用加密 (推荐) |
| ECB | 简单但安全性低,可并行 | 不推荐使用 |
| CFB | 流密码,逐位加密 | 实时加密 |
| OFB | 流密码,不需要填充 | 任意长度数据 |
| CTS | 保持长度一致 | 特殊需求 |

### 4. 填充模式说明

| 模式 | 说明 |
|------|------|
| PKCS7 | 填充字节值为填充长度 (推荐) |
| Zeros | 填充全部为 0 |
| ANSIX923 | 最后一字节为长度,其余为 0 |
| ISO10126 | 最后一字节为长度,其余随机 |
| None | 不填充 (需要数据长度为块大小整倍数) |

### 5. 性能优化
- 密钥和 IV 使用了缓存机制,相同密码和模式会复用
- 使用了 `Span<T>` 和 stackalloc 减少内存分配
- 支持 ReadOnlySpan<byte> 避免不必要的复制

## API 参考

### 核心方法

```csharp
// 字节数组加密
byte[] Encrypt(ReadOnlySpan<byte> content, string pwd, AesKeyModel model, 
               CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7)

// 字节数组解密
byte[] Decrypt(ReadOnlySpan<byte> secret, string pwd, AesKeyModel model, 
               CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7)

// 字符串加密为 Base64
string EncryptToBase64(string content, string pwd, AesKeyModel model, 
                       CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7,
                       Encoding? encoding = null)

// 从 Base64 解密字符串
string DecryptFromBase64(string base64Content, string pwd, AesKeyModel model, 
                         CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7,
                         Encoding? encoding = null)

// 字符串加密为 Hex
string EncryptToHex(string content, string pwd, AesKeyModel model, 
                    CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7,
                    Encoding? encoding = null)

// 从 Hex 解密字符串
string DecryptFromHex(string hexContent, string pwd, AesKeyModel model, 
                      CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7,
                      Encoding? encoding = null)
```

### AesKeyModel 枚举

```csharp
public enum AesKeyModel
{
    AES128,  // 128位密钥
    AES192,  // 192位密钥
    AES256   // 256位密钥 (推荐)
}
```
