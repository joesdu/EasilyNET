# RSA 安全使用指南 / RSA Security Guide

## 关于 String 格式密钥的安全性考虑

### ⚠️ 安全警告 / Security Warning

**中文：**
使用 `string` 类型存储私钥存在以下安全风险：

1. **内存安全问题**

   - String 在 .NET 中是不可变的，在内存中会留下多个副本
   - 垃圾回收器不会立即清理，私钥可能长时间保留在内存中
   - 内存转储或调试时可能暴露私钥

2. **缺少安全擦除**

   - String 无法被安全擦除（置零），即使对象被销毁，内存中的数据仍可能被恢复

3. **日志泄露风险**
   - 容易被意外记录到日志文件中
   - 异常堆栈跟踪可能包含密钥信息

**English:**
Storing private keys as `string` type has the following security risks:

1. **Memory Safety Issues**

   - Strings are immutable in .NET, leaving multiple copies in memory
   - Garbage collector doesn't immediately clean up, keys may persist in memory
   - Memory dumps or debugging may expose private keys

2. **Lack of Secure Erasure**

   - Strings cannot be securely erased (zeroed), data may be recoverable even after object destruction

3. **Log Leakage Risk**
   - Easy to accidentally log to files
   - Exception stack traces may contain key information

---

## 推荐的安全实践 / Recommended Security Practices

### 1. 使用安全的密钥存储 / Use Secure Key Storage

```csharp
// ❌ 不推荐 - 直接在代码中硬编码
var keys = RsaCrypt.GenerateKey(ERsaKeyLength.Bit2048);
var privateKey = keys.PrivateKey; // 危险！

// ✅ 推荐 - 使用 Azure Key Vault, AWS KMS, 或 Windows DPAPI
// Example using DPAPI (Windows only)
var encryptedKey = ProtectedData.Protect(
    Encoding.UTF8.GetBytes(keys.PrivateKey),
    null,
    DataProtectionScope.CurrentUser
);
```

### 2. 最小化密钥生命周期 / Minimize Key Lifetime

```csharp
// ✅ 使用完立即处理
var keys = RsaCrypt.GenerateKey(ERsaKeyLength.Bit2048);
try
{
    // 使用密钥进行操作
    var encrypted = RsaCrypt.Encrypt(keys.PublicKey, data);
}
finally
{
    // 注意：string 类型无法真正安全清除
    // 考虑使用 SecureString 或字节数组替代方案
}
```

### 3. 使用环境变量或配置文件（加密） / Use Environment Variables or Encrypted Configuration

```csharp
// ✅ 从加密的配置源读取
var privateKey = configuration["Rsa:PrivateKey"]; // 确保配置文件已加密
```

### 4. 使用 PEM 格式进行跨平台兼容 / Use PEM Format for Cross-Platform Compatibility

```csharp
// ✅ PEM 格式是工业标准
var keys = RsaCrypt.GenerateKey(ERsaKeyLength.Bit2048);
var pemPrivateKey = keys.GetPrivateKeyPem();
var pemPublicKey = keys.GetPublicKeyPem();

// 保存到文件时使用适当的文件权限（仅所有者可读）
File.WriteAllText("private.pem", pemPrivateKey);
// Linux/Unix: chmod 600 private.pem
```

---

## 密钥长度建议 / Key Length Recommendations

| 密钥长度 | 安全性    | 推荐使用场景                       |
| -------- | --------- | ---------------------------------- |
| 512 位   | ❌ 不安全 | 已弃用，不应使用                   |
| 1024 位  | ⚠️ 弱     | 已弃用，不应使用                   |
| 2048 位  | ✅ 安全   | **最小推荐长度**，适用于大多数场景 |
| 4096 位  | ✅ 高安全 | 高安全性要求的场景，性能会降低     |

| Key Length | Security         | Recommended Use Case                                 |
| ---------- | ---------------- | ---------------------------------------------------- |
| 512-bit    | ❌ Insecure      | Deprecated, do not use                               |
| 1024-bit   | ⚠️ Weak          | Deprecated, do not use                               |
| 2048-bit   | ✅ Secure        | **Minimum recommended**, suitable for most scenarios |
| 4096-bit   | ✅ High Security | High-security requirements, lower performance        |

---

## 加密填充模式 / Encryption Padding Modes

### OAEP (推荐) / OAEP (Recommended)

```csharp
// ✅ 使用 OAEP-SHA256（默认）
var encrypted = RsaCrypt.Encrypt(publicKey, data, useOaep: true);
```

**优点 / Advantages:**

- 更安全，防止选择密文攻击
- 现代密码学标准推荐

**缺点 / Disadvantages:**

- 可加密的数据略小（每块减少 66 字节 vs PKCS#1 的 11 字节）

### PKCS#1 v1.5

```csharp
// ⚠️ 仅用于兼容旧系统
var encrypted = RsaCrypt.Encrypt(publicKey, data, useOaep: false);
```

**使用场景 / Use Cases:**

- 需要与旧系统兼容时
- 不推荐用于新项目

---

## 完整示例 / Complete Example

```csharp
using EasilyNET.Security;
using System.Security.Cryptography;
using System.Text;

// 1. 生成密钥对（推荐至少 2048 位）
var keys = RsaCrypt.GenerateKey(ERsaKeyLength.Bit2048);

// 2. 导出为 PEM 格式（推荐用于存储和传输）
var privatePem = keys.GetPrivateKeyPem();
var publicPem = keys.GetPublicKeyPem();

Console.WriteLine("=== 公钥 (可以公开分享) ===");
Console.WriteLine(publicPem);

// 3. 加密数据
var originalData = Encoding.UTF8.GetBytes("敏感数据");
var encryptedData = RsaCrypt.Encrypt(keys.PublicKey, originalData, useOaep: true);

// 4. 解密数据
var decryptedData = RsaCrypt.Decrypt(keys.PrivateKey, encryptedData, useOaep: true);
var decryptedText = Encoding.UTF8.GetString(decryptedData);

Console.WriteLine($"解密后: {decryptedText}");

// 5. 数字签名
var signature = RsaCrypt.Signature(keys.PrivateKey, originalData);

// 6. 验证签名
var isValid = RsaCrypt.Verification(keys.PublicKey, originalData, signature);
Console.WriteLine($"签名验证: {(isValid ? "通过" : "失败")}");

// 7. 长数据加密（自动分块）
var longData = Encoding.UTF8.GetBytes(new string('A', 10000));
RsaCrypt.Encrypt(keys.PublicKey, longData, out var encryptedLongData, useOaep: true);
RsaCrypt.Decrypt(keys.PrivateKey, encryptedLongData, out var decryptedLongData, useOaep: true);
```

---

## 最佳实践总结 / Best Practices Summary

### ✅ 应该做的 / DO

1. ✅ 使用至少 2048 位密钥
2. ✅ 使用 OAEP 填充模式
3. ✅ 将私钥存储在安全位置（Key Vault、HSM）
4. ✅ 使用 PEM 格式进行跨平台兼容
5. ✅ 定期轮换密钥
6. ✅ 使用 HTTPS 传输密钥材料
7. ✅ 记录密钥使用但不记录密钥本身

### ❌ 不应该做的 / DON'T

1. ❌ 不要在源代码中硬编码私钥
2. ❌ 不要使用 512 或 1024 位密钥
3. ❌ 不要将私钥存储在未加密的文件中
4. ❌ 不要通过不安全的渠道传输私钥
5. ❌ 不要将私钥记录到日志文件
6. ❌ 不要在异常消息中包含密钥信息
7. ❌ 不要使用同一密钥对进行加密和签名

---

## 关于 String 类型的替代方案（未来改进）

### 当前限制 / Current Limitations

目前库使用 `string` 类型存储密钥是为了：

- 简化 API 使用
- 与现有 .NET API（如 `FromXmlString`）兼容
- 支持序列化和配置

### 可能的改进方向 / Potential Improvements

未来版本可以考虑：

1. **添加 `SecureRsaKey` 类**

   ```csharp
   // 使用 SecureString 或 byte[] + IDisposable
   public class SecureRsaKey : IDisposable
   {
       private byte[] _privateKeyBytes;

       public void Dispose()
       {
           if (_privateKeyBytes != null)
           {
               Array.Clear(_privateKeyBytes, 0, _privateKeyBytes.Length);
           }
       }
   }
   ```

2. **支持直接使用 RSAParameters**

   ```csharp
   public static byte[] Encrypt(RSAParameters publicKey, ReadOnlySpan<byte> data);
   ```

3. **集成硬件安全模块（HSM）支持**

---

## 参考资料 / References

- [NIST SP 800-57: Recommendation for Key Management](https://csrc.nist.gov/publications/detail/sp/800-57-part-1/rev-5/final)
- [RFC 8017: PKCS #1: RSA Cryptography Specifications Version 2.2](https://tools.ietf.org/html/rfc8017)
- [OWASP Cryptographic Storage Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Cryptographic_Storage_Cheat_Sheet.html)

---

**最后更新 / Last Updated:** 2025 年 10 月 7 日
