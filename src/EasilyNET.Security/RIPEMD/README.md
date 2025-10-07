# RIPEMD 哈希算法

## 概述

RIPEMD (RACE Integrity Primitives Evaluation Message Digest) 是一系列加密哈希函数,由比利时鲁汶大学开发。本库支持以下 4 种 RIPEMD 变体:

- **RIPEMD-128**: 生成 128 位 (16 字节) 哈希值
- **RIPEMD-160**: 生成 160 位 (20 字节) 哈希值
- **RIPEMD-256**: 生成 256 位 (32 字节) 哈希值
- **RIPEMD-320**: 生成 320 位 (40 字节) 哈希值

## 使用示例

### RIPEMD-128

```csharp
using EasilyNET.Security;

var data = "Hello World";

// 获取字节数组
var hashBytes = RipeMD128.Hash(data);

// 获取十六进制字符串 (小写)
var hexLower = RipeMD128.HashToHex(data);

// 获取十六进制字符串 (大写)
var hexUpper = RipeMD128.HashToHex(data, upperCase: true);

// 获取 Base64 字符串
var base64 = RipeMD128.HashToBase64(data);

// 对字节数组进行哈希
var inputBytes = Encoding.UTF8.GetBytes(data);
var hashFromBytes = RipeMD128.Hash(inputBytes);
var hexFromBytes = RipeMD128.HashToHex(inputBytes, upperCase: true);
var base64FromBytes = RipeMD128.HashToBase64(inputBytes);
```

### RIPEMD-160

```csharp
using EasilyNET.Security;

var data = "Hello World";

// 获取字节数组
var hashBytes = RipeMD160.Hash(data);

// 获取十六进制字符串 (小写)
var hexLower = RipeMD160.HashToHex(data);

// 获取十六进制字符串 (大写)
var hexUpper = RipeMD160.HashToHex(data, upperCase: true);

// 获取 Base64 字符串
var base64 = RipeMD160.HashToBase64(data);

// 对字节数组进行哈希
var inputBytes = Encoding.UTF8.GetBytes(data);
var hashFromBytes = RipeMD160.Hash(inputBytes);
var hexFromBytes = RipeMD160.HashToHex(inputBytes, upperCase: true);
var base64FromBytes = RipeMD160.HashToBase64(inputBytes);
```

### RIPEMD-256

```csharp
using EasilyNET.Security;

var data = "Hello World";

// 获取字节数组
var hashBytes = RipeMD256.Hash(data);

// 获取十六进制字符串 (小写)
var hexLower = RipeMD256.HashToHex(data);

// 获取十六进制字符串 (大写)
var hexUpper = RipeMD256.HashToHex(data, upperCase: true);

// 获取 Base64 字符串
var base64 = RipeMD256.HashToBase64(data);

// 对字节数组进行哈希
var inputBytes = Encoding.UTF8.GetBytes(data);
var hashFromBytes = RipeMD256.Hash(inputBytes);
var hexFromBytes = RipeMD256.HashToHex(inputBytes, upperCase: true);
var base64FromBytes = RipeMD256.HashToBase64(inputBytes);
```

### RIPEMD-320

```csharp
using EasilyNET.Security;

var data = "Hello World";

// 获取字节数组
var hashBytes = RipeMD320.Hash(data);

// 获取十六进制字符串 (小写)
var hexLower = RipeMD320.HashToHex(data);

// 获取十六进制字符串 (大写)
var hexUpper = RipeMD320.HashToHex(data, upperCase: true);

// 获取 Base64 字符串
var base64 = RipeMD320.HashToBase64(data);

// 对字节数组进行哈希
var inputBytes = Encoding.UTF8.GetBytes(data);
var hashFromBytes = RipeMD320.Hash(inputBytes);
var hexFromBytes = RipeMD320.HashToHex(inputBytes, upperCase: true);
var base64FromBytes = RipeMD320.HashToBase64(inputBytes);
```

## API 说明

每个 RIPEMD 类都提供以下方法:

### Hash 方法

- `Hash(string data)`: 计算字符串的哈希值,返回字节数组
- `Hash(ReadOnlySpan<byte> data)`: 计算字节数组的哈希值,返回字节数组

### HashToHex 方法

- `HashToHex(string data, bool upperCase = false)`: 计算字符串的哈希值,返回十六进制字符串
- `HashToHex(ReadOnlySpan<byte> data, bool upperCase = false)`: 计算字节数组的哈希值,返回十六进制字符串

### HashToBase64 方法

- `HashToBase64(string data)`: 计算字符串的哈希值,返回 Base64 字符串
- `HashToBase64(ReadOnlySpan<byte> data)`: 计算字节数组的哈希值,返回 Base64 字符串

## 输出长度

| 算法       | 位数 | 字节数 | 十六进制字符数 |
| ---------- | ---- | ------ | -------------- |
| RIPEMD-128 | 128  | 16     | 32             |
| RIPEMD-160 | 160  | 20     | 40             |
| RIPEMD-256 | 256  | 32     | 64             |
| RIPEMD-320 | 320  | 40     | 80             |

## 注意事项

- 所有方法都使用 UTF-8 编码处理字符串输入
- 字符串参数不能为 null 或空,否则会抛出 `ArgumentException`
- 所有实现都基于 BouncyCastle.Cryptography 库
- 方法使用了 `stackalloc` 进行性能优化
