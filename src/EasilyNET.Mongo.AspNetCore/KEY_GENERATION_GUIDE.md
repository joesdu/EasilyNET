# AWS S3 兼容访问密钥生成与使用指南

本指南说明如何生成 AWS 风格的访问密钥（Access Key ID 与 Secret Access Key）、如何安全存储与在本项目中配置使用，以及常见问题和高级用法（多租户、密钥轮换等）。

主要内容
- 生成密钥的多种方法（CLI、控制台、程序化、OpenSSL）
- 密钥格式要求与测试方法
- 在本项目中如何配置与使用（appsettings、环境变量、在中间件中加载）
- 安全建议与密钥生命周期管理
- 高级：多租户、密钥轮换、密钥存储建议

----------------------------------------------------------------
生成密钥的方法

方法一：使用 AWS IAM 控制台（推荐用于生产）
1. 登录 AWS 管理控制台
2. 导航到 IAM 服务
3. 创建新用户或选择现有用户
4. 在“安全凭证”中创建访问密钥并妥善保存（只会显示一次）

方法二：使用 AWS CLI（便捷）

```bash
# 安装并配置 AWS CLI（如未安装）
aws configure
# 或者直接设置
aws configure set aws_access_key_id YOUR_ACCESS_KEY
aws configure set aws_secret_access_key YOUR_SECRET_KEY
```

方法三：程序化生成（适合测试/自托管场景）
以下 C# 示例生成 AWS 兼容格式的密钥对。生成的密钥只作示例用途，生产请使用安全的密钥管理服务（KMS、Vault 等）。

```csharp
using System;
using System.Security.Cryptography;
using System.Text;

public static class AwsKeyGenerator
{
    public static (string AccessKeyId, string SecretAccessKey) GenerateAwsCompatibleKeys()
    {
        // 生成20字符的访问密钥ID（以 AKIA 前缀为示例）
        var accessKeyId = "AKIA" + GenerateRandomString(16, "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789");

        // 生成40字符的秘密访问密钥，使用 URL/BASE64 兼容字符集
        var secretAccessKey = GenerateRandomString(40, "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=");

        return (accessKeyId, secretAccessKey);
    }

    private static string GenerateRandomString(int length, string charset)
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[length];
        rng.GetBytes(bytes);
        var sb = new StringBuilder(length);
        for (var i = 0; i < length; i++)
        {
            sb.Append(charset[bytes[i] % charset.Length]);
        }
        return sb.ToString();
    }
}

// 使用示例
var keys = AwsKeyGenerator.GenerateAwsCompatibleKeys();
Console.WriteLine($"Access Key ID: {keys.AccessKeyId}");
Console.WriteLine($"Secret Access Key: {keys.SecretAccessKey}");
```

方法四：使用 OpenSSL（快速临时生成）

```bash
# 生成访问密钥ID（20 字符）
openssl rand -base64 20 | tr -d "=+/" | cut -c1-20

# 生成秘密访问密钥（40 字符）
openssl rand -base64 30 | tr -d "=+/" | cut -c1-40
```

----------------------------------------------------------------
密钥格式要求

Access Key ID
- 长度：通常 20 字符（如 AKIA 开头 + 16 字符）
- 字符集：大写字母 A-Z 与数字 0-9（AWS 真实格式可能有不同前缀）

Secret Access Key
- 长度：通常 40 字符
- 字符集：大小写字母、数字及部分特殊字符（如 + / =）
- 用于 HMAC-SHA256 签名计算

----------------------------------------------------------------
在本项目中的配置与使用

1) 环境变量（推荐用于容器与 CI）

```bash
# 设置访问密钥（示例）
export AWS_ACCESS_KEY_ID=AKIAIOSFODNN7EXAMPLE
export AWS_SECRET_ACCESS_KEY=wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY

# 设置服务器端加密主密钥（必须 32 字符）
export EASILYNET_MASTER_KEY="Your32CharacterMasterKey123456789012"
```

2) appsettings.json

```json
{
  "EasilyNET": {
    "S3Auth": {
      "Enabled": true,
      "RequireAuthentication": true,
      "AccessKeys": [
        { "AccessKeyId": "AKIA...", "SecretAccessKey": "..." }
      ]
    },
    "MasterKey": "Your32CharacterMasterKey123456789012"
  }
}
```

3) 在 Program.cs 中加载并注入中间件（示例）

```csharp
var app = builder.Build();

var accessKeys = new Dictionary<string, string>(StringComparer.Ordinal);
foreach (var e in builder.Configuration.GetSection("EasilyNET:S3Auth:AccessKeys").GetChildren())
{
    var id = e.GetValue<string>("AccessKeyId");
    var sk = e.GetValue<string>("SecretAccessKey");
    if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(sk)) accessKeys[id] = sk;
}

app.UseS3Authentication(o =>
{
    o.Enabled = true;
    o.RequireAuthentication = true;
    foreach (var kv in accessKeys) o.AccessKeys[kv.Key] = kv.Value;
});
```

4) 与 IAM 策略管理集成

- 使用 S3IamPolicyManager 将 AccessKeyId 与策略绑定（示例见 KEY_GENERATION_GUIDE 中的 IAM 示例）。
- 权限评估由 S3IamPolicyManager 提供，用于阻止未经授权的操作。

----------------------------------------------------------------
安全建议

1. 不要在代码中硬编码密钥。
2. 使用专用的密钥管理服务（例如 AWS KMS、HashiCorp Vault、Azure Key Vault）来存储与旋转密钥。
3. 使用最小权限原则，尽量限定策略中的资源与操作。
4. 定期轮换密钥并保留旧密钥一段时间以支持平滑切换。
5. 将 Secret Access Key 仅保存在受保护的服务器端，不要在客户端、浏览器或公开仓库中暴露。
6. 启用审计与访问日志以便追踪异常行为。
7. 在生产环境中强制使用 HTTPS。

----------------------------------------------------------------
高级场景

多租户支持

为每个租户生成独立的密钥对并存储在安全的存储中：

```csharp
public class TenantKeyManager
{
    private readonly ConcurrentDictionary<string, (string AccessKey, string SecretKey)> _tenantKeys = new();

    public void AddTenantKeys(string tenantId, string accessKey, string secretKey)
    {
        _tenantKeys[tenantId] = (accessKey, secretKey);
    }

    public (string AccessKey, string SecretKey)? GetTenantKeys(string tenantId)
    {
        return _tenantKeys.TryGetValue(tenantId, out var keys) ? keys : null;
    }
}
```

密钥轮换策略

实现自动轮换：生成新密钥并在配置/数据库中替换，保留旧密钥短期回退：

```csharp
public class KeyRotationService
{
    public async Task RotateKeysAsync(string tenantId)
    {
        var newKeys = AwsKeyGenerator.GenerateAwsCompatibleKeys();
        // 将 newKeys 存储到安全存储并在过渡期通知客户端
        // 可以保留旧密钥若干小时以便平滑切换
    }
}
```

----------------------------------------------------------------
测试生成的密钥

使用 AWS CLI 或 curl 测试签名与访问：

```bash
# 使用 AWS CLI 与自定义 endpoint 测试
aws s3 ls --endpoint-url=http://localhost:5000/s3

# 或者使用 curl 手动构建 SigV4 Authorization 头进行测试（较复杂）
```

常见问题

- 签名不匹配：检查系统时间、密钥是否正确、请求 canonicalization 是否正确。
- 访问被拒绝：检查 IAM 策略与 AccessKey 是否绑定。
- 无效访问密钥 ID：确保格式与长度正确并且密钥已启用。

----------------------------------------------------------------
附录：英文版（折叠）

<details>
<summary style="font-size:14px">English (collapse to expand)</summary>

# AWS S3 Compatible Key Generation & Usage Guide

This document explains how to generate AWS-style access keys (Access Key ID and Secret Access Key), how to securely store and configure them in this project, and advanced topics like multi-tenant keys and rotation.

[English content mirrors the Chinese guidance above with the same code samples and recommendations.]

</details>
