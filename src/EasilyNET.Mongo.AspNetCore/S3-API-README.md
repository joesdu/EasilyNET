# GridFS S3 兼容 API

本模块提供基于 MongoDB GridFS 的 S3 兼容 REST API，你可以使用标准的 S3 客户端（AWS CLI/SDK、MinIO 客户端、s3cmd 等）读写对象。

主要特性

- 支持 Put/Get/Head/Delete/Copy、List/ListV2 等常用 S3 接口
- 支持多段上传（初始化/上传分片/完成/中止）
- 支持通过 x-amz-meta-\* 头设置自定义元数据
- 支持 Range 请求有关生成密钥的建议，参见本项目的 KEY_GENERATION_GUIDE.md。

---

IAM 管理 API

系统提供了完整的 REST API 来管理用户、策略和访问密钥，所有数据持久化存储在 MongoDB 中。

### 用户管理

**创建用户**

```http
POST /api/iam/users
Content-Type: application/json

{
  "userId": "john-doe",
  "userName": "John Doe",
  "policies": ["ReadOnly"]
}
```

响应：

```json
{
  "userId": "john-doe",
  "userName": "John Doe",
  "accessKeyId": "AKIAIOSFODNN7EXAMPLE",
  "secretAccessKey": "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
  "policies": ["ReadOnly"]
}
```

**获取所有用户**

```http
GET /api/iam/users
```

**获取特定用户**

```http
GET /api/iam/users/{userId}
```

**删除用户**

```http
DELETE /api/iam/users/{userId}
```

### 策略管理

**创建策略**

```http
POST /api/iam/policies
Content-Type: application/json

{
  "policyName": "CustomPolicy",
  "version": "2012-10-17",
  "statements": [
    {
      "effect": "Allow",
      "action": ["s3:GetObject"],
      "resource": ["arn:aws:s3:::mybucket/*"]
    }
  ]
}
```

**创建默认策略**

```http
POST /api/iam/policies/admin
POST /api/iam/policies/readonly
```

**获取所有策略**

```http
GET /api/iam/policies
```

**获取特定策略**

```http
GET /api/iam/policies/{policyName}
```

**删除策略**

```http
DELETE /api/iam/policies/{policyName}
```

### 访问密钥管理

**为用户生成新密钥**

```http
POST /api/iam/users/{userId}/keys
```

**获取所有访问密钥**

```http
GET /api/iam/keys
```

---

动态密钥管理

与静态配置不同，新的实现支持：

- **动态添加用户**：无需重启服务即可创建新用户和访问密钥
- **密钥轮换**：可以为现有用户生成新的访问密钥
- **策略更新**：实时更新用户权限，无需重启
- **持久化存储**：所有 IAM 数据存储在 MongoDB 中，重启服务后数据保持
- **审计跟踪**：记录密钥创建时间、使用时间等

这使得系统更适合生产环境，支持动态用户管理和权限控制。

</details>下载）
- 可选的服务器端加密（SSE，AES256）
- 可插拔的 IAM 风格授权（策略管理器）
- 支持 AWS Signature Version 4 认证中间件

---

服务器端配置

1. 注册 Mongo 与 GridFS

在 Program.cs 中：

```csharp
var builder = WebApplication.CreateBuilder(args);

// MongoDB + GridFS
builder.Services.AddMongoContext<YourDbContext>(builder.Configuration);
builder.Services.AddMongoGridFS(builder.Configuration);

// IAM 策略管理器 (持久化到 MongoDB)
builder.Services.AddMongoS3IamPolicyManager();

// 控制器
builder.Services.AddControllers();
```

2. 配置认证（SigV4）

中间件会校验 AWS SigV4。Access Keys 现在存储在 MongoDB 中，可以通过 API 动态管理。

```csharp
var app = builder.Build();

// 启用 SigV4 认证
app.UseS3Authentication(opts =>
{
    opts.Enabled = builder.Configuration.GetValue("EasilyNET:S3Auth:Enabled", true);
    opts.RequireAuthentication = builder.Configuration.GetValue("EasilyNET:S3Auth:RequireAuthentication", true);
});

app.MapControllers();
app.Run();
```

3. 初始化 IAM 数据（可选）

在应用启动时，你可以创建默认的策略和用户：

```csharp
var app = builder.Build();

// 可选：初始化默认策略
var iam = app.Services.GetRequiredService<MongoS3IamPolicyManager>();

// 创建管理员策略
var adminPolicy = MongoS3IamPolicyManager.CreateAdminPolicy();
await iam.AddPolicyAsync("Admin", adminPolicy);

// 创建只读策略
var readOnlyPolicy = MongoS3IamPolicyManager.CreateReadOnlyPolicy();
await iam.AddPolicyAsync("ReadOnly", readOnlyPolicy);

// 创建用户（会自动生成 Access Keys）
await iam.AddUserAsync("admin-user", "Administrator", "AKIAEXAMPLE1234567890", "secret-key-example", ["Admin"]);

app.MapControllers();
app.Run();
```

---

接口与协议细节

基路径：/s3

- 桶级操作路径：/s3/{bucket}/{key}
- 对象键支持斜线；路由使用 {\*\*key} 捕获完整路径

支持的操作

- PUT /s3/{bucket}/{key}: 上传对象（支持 Content-Type、x-amz-meta-\*, SSE）
- GET /s3/{bucket}/{key}: 下载对象（支持 Range）
- HEAD /s3/{bucket}/{key}: 获取对象元数据
- DELETE /s3/{bucket}/{key}: 删除对象
- PUT /s3/copy/{key}（头 x-amz-copy-source: /{bucket}/{key}）：服务端拷贝
- 多段上传：POST /s3/{bucket}/upload/{key}?uploads=1，PUT /s3/{bucket}/part/{key}?uploadId=&partNumber=，POST /s3/{bucket}/complete/{key}?uploadId=，DELETE /s3/{bucket}/abort/{key}?uploadId=
- 列表：GET /s3/{bucket}?prefix=&marker=&max-keys= 或 GET /s3/{bucket}/list?list-type=2&prefix=&continuation-token=&start-after=&max-keys=
- 桶管理：PUT /s3/{bucket}（创建逻辑桶）、DELETE /s3/{bucket}（删除桶并清理对象）、HEAD /s3/{bucket}（检查存在）、GET /s3（列出桶）

响应头

- Content-Type, Content-Length, ETag, Last-Modified, Accept-Ranges
- x-amz-meta-\*
- SSE 对象附带：x-amz-server-side-encryption, x-amz-server-side-encryption-aws-kms-key-id

---

客户端配置示例

AWS CLI

```bash
aws configure set aws_access_key_id AKIAIOSFODNN7EXAMPLE
aws configure set aws_secret_access_key wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY
aws configure set region us-east-1
# 注意：endpoint_url 需包含 /s3 基路径
aws configure set endpoint_url http://localhost:5000/s3

# 上传
aws s3 cp myfile.txt s3://mybucket/myfile.txt

# 列表
aws s3 ls s3://mybucket/

# 下载
aws s3 cp s3://mybucket/myfile.txt myfile.down.txt
```

AWS SDK for .NET

```csharp
using Amazon.S3;
using Amazon.S3.Model;

var s3 = new AmazonS3Client(
    "AKIAIOSFODNN7EXAMPLE",
    "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
    new AmazonS3Config
    {
        ServiceURL = "http://localhost:5000/s3",
        ForcePathStyle = true // 使用 path-style
    });

// 上传
await s3.PutObjectAsync(new PutObjectRequest
{
    BucketName = "mybucket",
    Key = "folder/myfile.txt",
    ContentBody = "Hello, GridFS!",
    ContentType = "text/plain"
});

// 下载
using var get = await s3.GetObjectAsync("mybucket", "folder/myfile.txt");
using var reader = new StreamReader(get.ResponseStream);
var content = await reader.ReadToEndAsync();

// SSE
await s3.PutObjectAsync(new PutObjectRequest
{
    BucketName = "mybucket",
    Key = "secret.txt",
    ContentBody = "Top Secret",
    ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
});
```

MinIO 客户端 (mc)

```bash
mc alias set gridfs http://localhost:5000/s3 AKIAIOSFODNN7EXAMPLE wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY --api S3v4
mc ls gridfs/mybucket
mc cp myfile.txt gridfs/mybucket/myfile.txt
```

s3cmd

```bash
s3cmd --access_key=AKIAIOSFODNN7EXAMPLE \
      --secret_key=wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY \
      --host=localhost:5000 --host-bucket=localhost:5000 \
      --signature-v2=off --force-path-style \
      ls s3://mybucket/
```

Python (boto3)

```python
import boto3
s3 = boto3.resource(
    's3',
    aws_access_key_id='AKIAIOSFODNN7EXAMPLE',
    aws_secret_access_key='wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY',
    endpoint_url='http://localhost:5000/s3',
    region_name='us-east-1'
)

s3.Bucket('mybucket').upload_file('myfile.txt', 'myfile.txt')
for obj in s3.Bucket('mybucket').objects.all():
    print(obj.key)
```

---

使用说明与故障排查

- 基路径为 /s3，客户端的 endpoint_url/ServiceURL 必须包含 /s3。
- SigV4 依赖时间同步（建议启用 NTP），时间偏差会导致签名错误。
- 当 RequireAuthentication = true 时，所有请求必须签名。
- Range 下载通过 Range: bytes=start-end 头实现。
- 对象键支持斜线，路由使用 {\*\*key} 捕获完整键。
- 使用 SSE 时 MasterKey 必须为 32 个字符（AES-256）。
- 策略默认存内存，生产可做持久化与集中管理。
- 生产建议启用 HTTPS、结构化日志与限流。

---

附录：快速开始（一体化示例）

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddMongoContext<YourDbContext>(builder.Configuration);
builder.Services.AddMongoGridFS(builder.Configuration);

var app = builder.Build();

// 从配置读取 AccessKeys
var keys = new Dictionary<string, string>();
foreach (var e in builder.Configuration.GetSection("EasilyNET:S3Auth:AccessKeys").GetChildren())
{
    var id = e.GetValue<string>("AccessKeyId");
    var sk = e.GetValue<string>("SecretAccessKey");
    if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(sk)) keys[id] = sk;
}

app.UseRouting();
app.UseS3Authentication(o =>
{
    o.Enabled = true;
    o.RequireAuthentication = true;
    foreach (var kv in keys) o.AccessKeys[kv.Key] = kv.Value;
});

app.MapControllers();

// 可选 IAM 初始化
var iam = app.Services.GetRequiredService<EasilyNET.Mongo.AspNetCore.Security.S3IamPolicyManager>();
iam.AddPolicy("Admin", EasilyNET.Mongo.AspNetCore.Security.S3IamPolicyManager.CreateAdminPolicy());
iam.AddUser("admin", "AKIAIOSFODNN7EXAMPLE", "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY", ["Admin"]);

app.Run();
```

有关生成密钥的建议，参见本项目的 KEY_GENERATION_GUIDE.md。

<details>
<summary style="font-size:14px">English (collapse to expand)</summary>

# GridFS S3 Compatible API

This module exposes S3-compatible REST API endpoints backed by MongoDB GridFS so you can use standard S3 clients (AWS CLI/SDKs, MinIO client, s3cmd) to store and retrieve objects.

Key highlights

- S3-compatible endpoints for Put/Get/Head/Delete/Copy, List/ListV2
- Multipart upload (initiate/upload-part/complete/abort)
- Custom metadata via x-amz-meta-\*
- Range requests (partial download)
- Optional server-side encryption (SSE, AES256)
- Pluggable IAM-style authorization (policy manager)
- AWS Signature Version 4 authentication middleware

Server setup

1. Register MongoDB and GridFS

In Program.cs:

```csharp
var builder = WebApplication.CreateBuilder(args);

// MongoDB + GridFS
builder.Services.AddMongoContext<YourDbContext>(builder.Configuration);
builder.Services.AddMongoGridFS(builder.Configuration);

// Controllers
builder.Services.AddControllers();
```

2. Configure Authentication (SigV4) and Access Keys

The middleware validates AWS Signature V4. You must provide an AccessKeyId -> SecretAccessKey mapping. You can keep them in appsettings.json, env vars, or code.

appsettings.json example:

```json
{
  "ConnectionStrings": {
    "Mongo": "mongodb://user:pass@localhost:27017/yourdb"
  },
  "EasilyNET": {
    "S3Auth": {
      "Enabled": true,
      "RequireAuthentication": true,
      "AccessKeys": [
        {
          "AccessKeyId": "AKIAIOSFODNN7EXAMPLE",
          "SecretAccessKey": "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY"
        }
      ]
    },
    // 32 chars for AES-256 SSE (see section 3)
    "MasterKey": "Your32CharacterMasterKey123456789012"
  }
}
```

Configure middleware in Program.cs:

```csharp
var app = builder.Build();

// Load AccessKeys from configuration
var accessKeys = new Dictionary<string, string>(StringComparer.Ordinal);
foreach (var entry in builder.Configuration.GetSection("EasilyNET:S3Auth:AccessKeys").GetChildren())
{
    var id = entry.GetValue<string>("AccessKeyId");
    var sk = entry.GetValue<string>("SecretAccessKey");
    if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(sk))
    {
        accessKeys[id] = sk;
    }
}

// Enable SigV4 auth
app.UseS3Authentication(opts =>
{
    opts.Enabled = builder.Configuration.GetValue("EasilyNET:S3Auth:Enabled", true);
    opts.RequireAuthentication = builder.Configuration.GetValue("EasilyNET:S3Auth:RequireAuthentication", true);
    foreach (var kv in accessKeys) opts.AccessKeys[kv.Key] = kv.Value;
});

app.MapControllers();
app.Run();
```

3. Configure MasterKey for Server-Side Encryption (SSE)

For SSE (AES256) you must provide a 32-character master key (256-bit). Set via configuration or environment variable.

- Configuration (preferred): EasilyNET:MasterKey
- Environment variable: EASILYNET_MASTER_KEY

Examples (PowerShell / Bash):

```powershell
# Windows PowerShell
$env:EASILYNET_MASTER_KEY = "Your32CharacterMasterKey123456789012"
```

```bash
# Linux / macOS bash
export EASILYNET_MASTER_KEY="Your32CharacterMasterKey123456789012"
```

4. Configure IAM-style Authorization (optional)

An in-memory S3-like IAM policy manager is available for fine-grained authorization. Define policies and assign them to users by AccessKeyId at startup:

```csharp
using EasilyNET.Mongo.AspNetCore.Security;

// After app.Build(); before app.Run();
var iam = app.Services.GetRequiredService<S3IamPolicyManager>();

// Built-in helper to grant all S3 actions
var adminPolicy = S3IamPolicyManager.CreateAdminPolicy();
iam.AddPolicy("Admin", adminPolicy);

// Or define custom policy
var readOnlyPolicy = new IamPolicy
{
    Version = "2012-10-17",
    Statement =
    [
        new IamStatement
        {
            Effect = "Allow",
            Action = ["s3:GetObject", "s3:ListBucket"],
            Resource = [
                "arn:aws:s3:::mybucket",
                "arn:aws:s3:::mybucket/*"
            ]
        }
    ]
};

iam.AddPolicy("ReadOnly", readOnlyPolicy);

// Bind access keys to policies
iam.AddUser(
    userName: "demo-user",
    accessKeyId: "AKIAIOSFODNN7EXAMPLE",
    secretAccessKey: "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
    attachedPolicies: ["Admin"]
);
```

Notes

- The sample IAM is in-memory. Persist/centralize as needed in production.
- Ensure system time is synchronized for SigV4 validation.

Endpoints and protocol details

Base path: /s3

- Bucket-scoped operations use routes like /s3/{bucket}/{key}
- Keys can include slashes; routes support catch-all {\*\*key}

Supported operations

- PUT /s3/{bucket}/{key}: upload object (supports Content-Type, x-amz-meta-\*, SSE)
- GET /s3/{bucket}/{key}: download object (supports Range)
- HEAD /s3/{bucket}/{key}: object metadata
- DELETE /s3/{bucket}/{key}: delete object
- PUT /s3/copy/{key} with header x-amz-copy-source: /{bucket}/{key}: server-side copy
- Multipart upload
  - POST /s3/{bucket}/upload/{key}?uploads=1
  - PUT /s3/{bucket}/part/{key}?uploadId=...&partNumber=...
  - POST /s3/{bucket}/complete/{key}?uploadId=...
  - DELETE /s3/{bucket}/abort/{key}?uploadId=...
- List
  - GET /s3/{bucket}?prefix=&marker=&max-keys=
  - GET /s3/{bucket}/list?list-type=2&prefix=&continuation-token=&start-after=&max-keys=
- Buckets
  - PUT /s3/{bucket}: create bucket (logical)
  - DELETE /s3/{bucket}: delete bucket (logical, removes objects)
  - HEAD /s3/{bucket}: check bucket existence
  - GET /s3: list buckets

Client configuration

AWS CLI

```bash
aws configure set aws_access_key_id AKIAIOSFODNN7EXAMPLE
aws configure set aws_secret_access_key wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY
aws configure set region us-east-1
# Note: endpoint_url includes /s3 base path
aws configure set endpoint_url http://localhost:5000/s3

# Upload
aws s3 cp myfile.txt s3://mybucket/myfile.txt

# List
aws s3 ls s3://mybucket/

# Download
aws s3 cp s3://mybucket/myfile.txt myfile.down.txt
```

AWS SDK for .NET

```csharp
using Amazon.S3;
using Amazon.S3.Model;

var s3 = new AmazonS3Client(
    "AKIAIOSFODNN7EXAMPLE",
    "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
    new AmazonS3Config
    {
        ServiceURL = "http://localhost:5000/s3",
        ForcePathStyle = true
    });

// Put
await s3.PutObjectAsync(new PutObjectRequest
{
    BucketName = "mybucket",
    Key = "folder/myfile.txt",
    ContentBody = "Hello, GridFS!",
    ContentType = "text/plain"
});

// Get
using var get = await s3.GetObjectAsync("mybucket", "folder/myfile.txt");
using var reader = new StreamReader(get.ResponseStream);
var content = await reader.ReadToEndAsync();

// SSE
await s3.PutObjectAsync(new PutObjectRequest
{
    BucketName = "mybucket",
    Key = "secret.txt",
    ContentBody = "Top Secret",
    ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
});
```

MinIO Client (mc)

```bash
mc alias set gridfs http://localhost:5000/s3 AKIAIOSFODNN7EXAMPLE wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY --api S3v4
mc ls gridfs/mybucket
mc cp myfile.txt gridfs/mybucket/myfile.txt
```

s3cmd

```bash
s3cmd --access_key=AKIAIOSFODNN7EXAMPLE \
      --secret_key=wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY \
      --host=localhost:5000 --host-bucket=localhost:5000 \
      --signature-v2=off --force-path-style \
      ls s3://mybucket/
```

Python (boto3)

```python
import boto3
s3 = boto3.resource(
    's3',
    aws_access_key_id='AKIAIOSFODNN7EXAMPLE',
    aws_secret_access_key='wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY',
    endpoint_url='http://localhost:5000/s3',
    region_name='us-east-1'
)

s3.Bucket('mybucket').upload_file('myfile.txt', 'myfile.txt')
for obj in s3.Bucket('mybucket').objects.all():
    print(obj.key)
```

Usage notes and troubleshooting

- Path base is /s3. Ensure clients’ endpoint_url or ServiceURL includes /s3.
- SigV4 requires synchronized system clocks (NTP recommended). A skew can cause signature mismatch.
- If RequireAuthentication = true, all requests must be signed.
- Range downloads are supported via Range: bytes=start-end.
- Keys support slashes. Routes are defined with {\*\*key} catch-all.
- For SSE, MasterKey must be exactly 32 characters for AES-256.
- Policies are in-memory by default; persist as needed in production.
- Use HTTPS in production, enable structured logging and rate limiting as required.

Appendix: Quick start (all-in-one)

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddMongoContext<YourDbContext>(builder.Configuration);
builder.Services.AddMongoGridFS(builder.Configuration);

var app = builder.Build();

// AccessKeys from config
var keys = new Dictionary<string, string>();
foreach (var e in builder.Configuration.GetSection("EasilyNET:S3Auth:AccessKeys").GetChildren())
{
    var id = e.GetValue<string>("AccessKeyId");
    var sk = e.GetValue<string>("SecretAccessKey");
    if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(sk)) keys[id] = sk;
}

app.UseRouting();
app.UseS3Authentication(o =>
{
    o.Enabled = true;
    o.RequireAuthentication = true;
    foreach (var kv in keys) o.AccessKeys[kv.Key] = kv.Value;
});

app.MapControllers();

// Optional IAM bootstrap
var iam = app.Services.GetRequiredService<EasilyNET.Mongo.AspNetCore.Security.S3IamPolicyManager>();
iam.AddPolicy("Admin", EasilyNET.Mongo.AspNetCore.Security.S3IamPolicyManager.CreateAdminPolicy());
iam.AddUser("admin", "AKIAIOSFODNN7EXAMPLE", "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY", ["Admin"]);

app.Run();
```

For key generation tips, see KEY_GENERATION_GUIDE.md in this project.

---

IAM Management API

The system provides a complete REST API for managing users, policies, and access keys, with all data persisted in MongoDB.

### User Management

**Create User**

```http
POST /api/iam/users
Content-Type: application/json

{
  "userId": "john-doe",
  "userName": "John Doe",
  "policies": ["ReadOnly"]
}
```

Response:

```json
{
  "userId": "john-doe",
  "userName": "John Doe",
  "accessKeyId": "AKIAIOSFODNN7EXAMPLE",
  "secretAccessKey": "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
  "policies": ["ReadOnly"]
}
```

**Get All Users**

```http
GET /api/iam/users
```

**Get Specific User**

```http
GET /api/iam/users/{userId}
```

**Delete User**

```http
DELETE /api/iam/users/{userId}
```

### Policy Management

**Create Policy**

```http
POST /api/iam/policies
Content-Type: application/json

{
  "policyName": "CustomPolicy",
  "version": "2012-10-17",
  "statements": [
    {
      "effect": "Allow",
      "action": ["s3:GetObject"],
      "resource": ["arn:aws:s3:::mybucket/*"]
    }
  ]
}
```

**Create Default Policies**

```http
POST /api/iam/policies/admin
POST /api/iam/policies/readonly
```

**Get All Policies**

```http
GET /api/iam/policies
```

**Get Specific Policy**

```http
GET /api/iam/policies/{policyName}
```

**Delete Policy**

```http
DELETE /api/iam/policies/{policyName}
```

### Access Key Management

**Generate New Keys for User**

```http
POST /api/iam/users/{userId}/keys
```

**Get All Access Keys**

```http
GET /api/iam/keys
```

---

Dynamic Key Management

Unlike static configuration, the new implementation supports:

- **Dynamic User Addition**: Create new users and access keys without restarting the service
- **Key Rotation**: Generate new access keys for existing users
- **Policy Updates**: Update user permissions in real-time without restart
- **Persistent Storage**: All IAM data stored in MongoDB, survives service restarts
- **Audit Trail**: Track key creation time, last used time, etc.

This makes the system more suitable for production environments, supporting dynamic user management and access control.

</details>
