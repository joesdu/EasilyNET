##### EasilyNET.Security

常用加密/哈希算法封装，降低使用复杂度，面向 .NET 高性能与易用性。

- 算法：AES、DES、RC4、TripleDES、RSA、SM2、SM3、SM4、RIPEMD(128/160/256/320)
- RSA 支持 XML/Base64/PEM 互转，提供签名验签与文件 SHA256
- 国密算法基于 BouncyCastle 实现（SM2/SM3/RIPEMD）

> 说明：本库不“重写算法”，而是对 .NET/BouncyCastle 做轻量封装。

#### 可能的编码问题

若你使用了非 UTF-8 的旧编码（如 GBK），可能需要注册 CodePages 编码提供器：

```csharp
System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
```

#### 快速开始

```csharp
// AES (注意：本库会对密钥做 hash 处理，密文仅与本库兼容)
var cipher = AesCrypt.EncryptToBase64("hello", "pwd", AesKeyModel.AES256);
var plain = AesCrypt.DecryptFromBase64(cipher, "pwd", AesKeyModel.AES256);
```

#### AES

- 支持 AES128/192/256（通过 `AesKeyModel`）
- 默认模式：CBC + PKCS7
- 提供字符串便捷方法（Base64/Hex）

#### DES / TripleDES（兼容性用途）

- 已标记为不安全/遗留算法，仅建议用于兼容旧系统
- 默认模式：CBC + PKCS7
- **注意**：密钥会被内部 hash 处理，密文仅能用本库解密

#### RC4（不安全，仅兼容）

- **强烈不推荐用于新系统**
- 支持原始字节与“密码派生”的便捷方法

```csharp
var cipher = Rc4Crypt.EncryptToBase64("hello", "pwd");
var plain = Rc4Crypt.DecryptFromBase64(cipher, "pwd");
```

#### RSA

- 生成密钥：`RsaCrypt.GenerateKey(ERsaKeyLength)`
- 加解密：默认 `OaepSHA256`（推荐）
- 大数据分段加解密：`Encrypt/Decrypt` 带 `out` 重载
- 签名验签：默认 `SHA256 + Pkcs1`
- 支持 XML/Base64/PEM 格式互转

```csharp
var key = RsaCrypt.GenerateKey(ERsaKeyLength.Bit2048);
RsaCrypt.Encrypt(key.PublicKey, "hello"u8.ToArray(), out var secret);
RsaCrypt.Decrypt(key.PrivateKey, secret, out var plainBytes);

var sign = RsaCrypt.Signature(key.PrivateKey, "hello"u8.ToArray());
var ok = RsaCrypt.Verification(key.PublicKey, "hello"u8.ToArray(), sign);

var pemPri = RsaCrypt.ExportPrivateKeyToPem(key.PrivateKey);
var pemPub = RsaCrypt.ExportPublicKeyToPem(key.PublicKey);
```

#### SM2

- 支持密钥生成、加解密、签名/验签
- 默认模式：C1C3C2（可选 C1C2C3）

```csharp
Sm2Crypt.GenerateKey(out var pub, out var pri);
var cipher = Sm2Crypt.Encrypt(pub, "hello"u8.ToArray());
var plain = Sm2Crypt.Decrypt(pri, cipher);
var sig = Sm2Crypt.Signature(pri, "hello"u8.ToArray());
var ok = Sm2Crypt.Verify(pub, "hello"u8.ToArray(), sig);
```

#### SM3

```csharp
var hex = Sm3Signature.HashToHex("hello", upperCase: true);
var base64 = Sm3Signature.HashToBase64("hello");
```

#### SM4

- 128-bit key / block
- 传参 `hexString=true` 表示 key/iv 为 16 进制字符串

```csharp
var cipherHex = Sm4Crypt.EncryptECBToHex("701d1cc0cfbe7ee11824df718855c0c6", true, "hello");
var plain = Sm4Crypt.DecryptECBFromHex("701d1cc0cfbe7ee11824df718855c0c6", true, cipherHex);

var cipherBase64 = Sm4Crypt.EncryptCBCToBase64("701d1cc0cfbe7ee11824df718855c0c6", true, "701d1cc0cfbe7ee11824df718855c0c5", "hello");
var plain2 = Sm4Crypt.DecryptCBCFromBase64("701d1cc0cfbe7ee11824df718855c0c6", true, "701d1cc0cfbe7ee11824df718855c0c5", cipherBase64);
```

#### RIPEMD

支持 128/160/256/320 变体：`RipeMD128/160/256/320`，提供 `Hash/HashToHex/HashToBase64`。

#### 注意事项

- AES/DES/TripleDES 会对密钥进行内部 hash 派生，**密文仅能用本库解密**。
- RC4/DES/TripleDES 为遗留算法，仅用于兼容旧系统。
- 所有字符串 API 默认使用 UTF-8。
