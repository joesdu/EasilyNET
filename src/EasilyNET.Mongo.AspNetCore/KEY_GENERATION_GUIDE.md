# AWS S3 兼容访问密钥生成指南

## 概述

为了实现完整的 AWS S3 兼容性，系统需要生成 AWS 风格的访问密钥对（Access Key ID 和 Secret Access Key）。这些密钥用于身份验证和权限控制。

## 密钥生成方法

### 方法 1：使用 AWS CLI 生成

如果您有 AWS 账户，可以使用以下命令生成密钥：

```bash
# 安装AWS CLI（如果尚未安装）
pip install awscli

# 配置AWS CLI（会提示输入访问密钥）
aws configure

# 或者直接设置
aws configure set aws_access_key_id YOUR_ACCESS_KEY
aws configure set aws_secret_access_key YOUR_SECRET_KEY
```

### 方法 2：使用 AWS IAM 控制台

1. 登录 AWS 管理控制台
2. 导航到 IAM 服务
3. 创建新用户或选择现有用户
4. 在"安全凭证"选项卡中创建访问密钥
5. 下载.csv 文件或复制密钥

### 方法 3：程序化生成（推荐用于测试）

使用以下 C#代码生成兼容的密钥对：

```csharp
using System;
using System.Security.Cryptography;
using System.Text;

public class AwsKeyGenerator
{
    public static (string AccessKeyId, string SecretAccessKey) GenerateAwsCompatibleKeys()
    {
        // 生成20字符的访问密钥ID（AWS格式：AKIA开头 + 16个随机字符）
        var accessKeyId = "AKIA" + GenerateRandomString(16, "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789");

        // 生成40字符的秘密访问密钥
        var secretAccessKey = GenerateRandomString(40, "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/");

        return (accessKeyId, secretAccessKey);
    }

    private static string GenerateRandomString(int length, string charset)
    {
        var random = RandomNumberGenerator.Create();
        var result = new StringBuilder(length);
        var charsetBytes = Encoding.UTF8.GetBytes(charset);

        for (var i = 0; i < length; i++)
        {
            var randomByte = new byte[1];
            random.GetBytes(randomByte);
            var index = randomByte[0] % charsetBytes.Length;
            result.Append((char)charsetBytes[index]);
        }

        return result.ToString();
    }
}

// 使用示例
var keys = AwsKeyGenerator.GenerateAwsCompatibleKeys();
Console.WriteLine($"Access Key ID: {keys.AccessKeyId}");
Console.WriteLine($"Secret Access Key: {keys.SecretAccessKey}");
```

### 方法 4：使用 OpenSSL 生成

```bash
# 生成访问密钥ID
openssl rand -base64 20 | tr -d "=+/" | cut -c1-20

# 生成秘密访问密钥
openssl rand -base64 30 | tr -d "=+/" | cut -c1-40
```

## 密钥格式要求

### Access Key ID

- 长度：20 个字符
- 格式：通常以`AKIA`开头（AWS 标准），但也可以是其他前缀
- 字符集：大写字母 A-Z 和数字 0-9

### Secret Access Key

- 长度：40 个字符
- 字符集：大写字母、小写字母、数字和特殊字符`+/`
- 用于 HMAC-SHA256 签名计算

## 在应用程序中使用密钥

### 1. 环境变量配置

```bash
# 设置环境变量
export AWS_ACCESS_KEY_ID=AKIAIOSFODNN7EXAMPLE
export AWS_SECRET_ACCESS_KEY=wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY
```

### 2. 配置文件

在`appsettings.json`中添加：

```json
{
  "AWS": {
    "AccessKeyId": "AKIAIOSFODNN7EXAMPLE",
    "SecretAccessKey": "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY"
  }
}
```

### 3. 代码中使用

```csharp
// 从配置中读取
var accessKeyId = configuration["AWS:AccessKeyId"];
var secretAccessKey = configuration["AWS:SecretAccessKey"];

// 或者从环境变量读取
var accessKeyId = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
var secretAccessKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");
```

## 安全注意事项

1. **不要硬编码密钥**：永远不要在代码中硬编码访问密钥
2. **定期轮换密钥**：定期更换访问密钥以提高安全性
3. **使用最小权限原则**：只授予必要的权限
4. **保护秘密访问密钥**：秘密访问密钥应该保密，只在服务器端使用
5. **使用 IAM 角色**：在 AWS 环境中，优先使用 IAM 角色而不是访问密钥

## 测试生成的密钥

您可以使用以下命令测试生成的密钥是否有效：

```bash
# 使用AWS CLI测试
aws s3 ls --endpoint-url=http://your-endpoint:port

# 或者使用curl测试
curl -H "Authorization: AWS4-HMAC-SHA256 Credential=$AWS_ACCESS_KEY_ID/$(date +%Y%m%d)/us-east-1/s3/aws4_request, SignedHeaders=host;x-amz-date, Signature=..." \
     http://your-endpoint:port/bucket-name/object-key
```

## 故障排除

### 常见问题

1. **签名不匹配错误**

   - 检查时钟同步
   - 验证密钥格式
   - 确保请求格式正确

2. **访问被拒绝**

   - 检查 IAM 策略
   - 验证密钥权限
   - 确认端点 URL

3. **无效访问密钥 ID**
   - 确保密钥格式正确
   - 检查密钥是否激活

## 高级配置

### 多租户支持

对于多租户应用，可以为每个租户生成独立的密钥对：

```csharp
public class TenantKeyManager
{
    private readonly Dictionary<string, (string AccessKey, string SecretKey)> _tenantKeys = new();

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

### 密钥轮换

实现自动密钥轮换：

```csharp
public class KeyRotationService
{
    public async Task RotateKeysAsync(string tenantId)
    {
        // 生成新密钥
        var newKeys = AwsKeyGenerator.GenerateAwsCompatibleKeys();

        // 存储新密钥（带过期时间）
        // 更新数据库/配置

        // 通知客户端使用新密钥
        // 保留旧密钥一段时间以实现平滑过渡
    }
}
```

## 集成配置

### 服务器端加密主密钥

为了启用服务器端加密，需要配置主密钥：

```bash
# 设置主密钥（必须是32个字符）
export EASILYNET_MASTER_KEY="Your32CharacterMasterKey123456789012"
```

### IAM 策略配置

创建 IAM 策略进行访问控制：

```csharp
var policyManager = new S3IamPolicyManager();

// 创建管理员策略
var adminPolicy = S3IamPolicyManager.CreateAdminPolicy();
policyManager.AddPolicy("AdminPolicy", adminPolicy);

// 添加用户
policyManager.AddUser("demo-user", accessKeyId, secretAccessKey, ["AdminPolicy"]);

// 检查权限
var hasPermission = policyManager.HasPermission(accessKeyId, "s3:GetObject", "arn:aws:s3:::bucket/key");
```

## 总结

正确生成和配置 AWS 兼容的访问密钥是实现 S3 兼容 API 安全性的关键。通过遵循上述指南，您可以确保系统的安全性和兼容性。
