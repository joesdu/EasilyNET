# S3 兼容 GridFS 实现概述

本仓库现已实现一个 S3 兼容的 GridFS REST API。以下为实现摘要、功能清单与使用说明（中文为主，英文内容折叠在下方）。

## ✅ 主要完成项

### 1. 统一的对象存储接口（`IObjectStorage`）

- 提供统一的对象存储接口，支持上传、下载、删除、列举与元数据操作。
- 该接口按 S3 风格设计，便于替换或扩展后端实现（例如支持不同存储引擎）。

### 2. GridFS 实现（`GridFSObjectStorage`）

- 基于 MongoDB GridFS 完整实现 `IObjectStorage`。
- 支持文件上传、流式下载、元数据存储与自定义元数据。
- 增强功能：
  - IAM 策略基础的权限检查
  - 对象版本管理（`S3ObjectVersioningManager`）
  - 服务器端 AES-256 加密（SSE）
  - 改进的多段上传元数据管理与合并逻辑

### 3. S3 兼容 REST API（`S3CompatibleController`）

- 支持主要 S3 操作：PUT/GET/HEAD/DELETE、List（含 V2）、Copy 以及 Multipart Upload 一套 API。
- 路由支持带斜线的对象键（catch-all 路由 {**key}）。
- 按 S3 风格返回 ETag、Last-Modified、x-amz-meta-* 等头信息。

### 4. 认证中间件（`S3AuthenticationMiddleware`）

- 支持 AWS Signature Version 4 的格式校验（实现为可拓展的中间件）。
- 在实现中改进了 Canonical Request、签名构建与请求体处理方式，避免直接消费请求流。
- 支持通过配置注入 AccessKey/Secret 映射。

### 5. 安全特性

- IAM 策略管理（`S3IamPolicyManager`）：基于 JSON 的策略、可在启动时注册并绑定到访问密钥。
- 对象版本管理（`S3ObjectVersioningManager`）：在 GridFS 之上模拟版本控制。
- 服务器端加密（`S3ServerSideEncryptionManager`）：AES-256 加密/解密，主密钥来自配置或环境变量。
- 在对象操作处集成权限检查（`CheckPermissionAsync`），可用于细粒度控制。

### 6. 服务注册与集成

- 扩展了 `AddMongoGridFS` 方法以注册 GridFS 与 S3 相关服务。
- 将 `IGridFSBucket`、`IObjectStorage`、策略管理器与加密管理器注入 DI 容器，方便使用。
- 支持从配置或环境变量加载 master key（`EASILYNET_MASTER_KEY`）用于 SSE。

## 🚀 使用与示例

在 `Program.cs` 中注册：

```csharp
builder.Services.AddMongoGridFS(builder.Configuration);
```

启用认证中间件与控制器：

```csharp
app.UseS3Authentication(); // 可选
app.MapControllers();
```

### 与 AWS CLI 联动：

```bash
# 配置 AWS CLI 指向你的 endpoint
aws configure set endpoint_url http://localhost:5000/s3

# 上传文件
aws s3 cp myfile.txt s3://mybucket/myfile.txt

# 列举对象
aws s3 ls s3://mybucket/

# 下载文件
aws s3 cp s3://mybucket/myfile.txt downloaded.txt
```

### 与 .NET SDK 联动：

```csharp
var s3Client = new AmazonS3Client(
    "dummy-access-key",
    "dummy-secret-key",
    new AmazonS3Config
    {
        ServiceURL = "http://localhost:5000/s3",
        ForcePathStyle = true
    });

// 上传
await s3Client.PutObjectAsync(new PutObjectRequest
{
    BucketName = "mybucket",
    Key = "myfile.txt",
    ContentBody = "Hello, GridFS!"
});

// 下载
var response = await s3Client.GetObjectAsync("mybucket", "myfile.txt");
```

## 📁 文件结构

```
src/EasilyNET.Mongo.AspNetCore/
├── Abstraction/
│   ├── IObjectStorage.cs          # 对象存储接口
│   ├── GridFSObjectStorage.cs     # GridFS 实现
│   └── IGridFSBucketFactory.cs    # 工厂接口
├── Controllers/
│   └── S3CompatibleController.cs  # S3 REST API 控制器
├── Middleware/
│   └── S3AuthenticationMiddleware.cs # 认证中间件
├── Security/
│   └── S3IamPolicyManager.cs      # IAM 策略管理
├── Versioning/
│   └── S3ObjectVersioningManager.cs # 对象版本管理
├── Encryption/
│   └── S3ServerSideEncryptionManager.cs # 服务器端加密
├── GridFSCollectionExtensions.cs  # 服务注册
└── S3-API-README.md              # 文档
```

## ⚠️ 生产注意事项

- 认证：确保 SigV4 验证在生产环境中健全并严格（时间、header、payload 校验）。
- 授权：使用 IAM 策略并将策略持久化（示例为内存实现），生产需集中化管理。
- 传输安全：生产强制使用 HTTPS。
- 密钥管理：使用 KMS 或 Vault 管理 master key 与访问密钥，避免硬编码。
- 可观测性：开启审计日志、指标与限流以防滥用。

## 🔐 安全配置示例

### Master Key：

```bash
export EASILYNET_MASTER_KEY="Your32CharacterMasterKey123456789012"
```

### IAM 策略示例（JSON）可用于定义范围与权限。

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": ["s3:GetObject", "s3:PutObject"],
      "Resource": "arn:aws:s3:::mybucket/*"
    }
  ]
}
```

----------------------------------------------------------------
<details>
<summary style="font-size:14px">English (click to expand)</summary>

# S3 Compatible GridFS Implementation Complete

I have successfully implemented a S3-compatible REST API for MongoDB GridFS. Here's what has been accomplished:

## ✅ Completed Features

### 1. Unified Object Storage Interface (IObjectStorage)

- Created a common interface for object storage operations
- Supports PutObject, GetObject, DeleteObject, ListObjects, and metadata operations
- Compatible with S3-style APIs

### 2. GridFS Implementation (GridFSObjectStorage)

- Full implementation of IObjectStorage using MongoDB GridFS
- Handles file uploads, downloads, and metadata
- Supports custom metadata storage
- Enhanced Features:
  - Complete IAM policy-based permission checking
  - Full object versioning support using S3ObjectVersioningManager
  - Server-side encryption with AES-256
  - Improved multipart upload metadata management

### 3. S3 Compatible REST API (S3CompatibleController)

- PUT /{bucket}/{key}: Upload objects
- GET /{bucket}/{key}: Download objects
- DELETE /{bucket}/{key}: Delete objects
- HEAD /{bucket}/{key}: Get object metadata
- GET /{bucket}: List objects with pagination support

### 4. Authentication Middleware (S3AuthenticationMiddleware)

- Basic S3 signature validation
- Compatible with AWS Signature Version 4 format
- Extensible for production authentication
- Enhanced: Proper dependency injection and configuration options

### 5. Security Features

- IAM Policy Manager (S3IamPolicyManager): JSON-based access control policies
- Object Versioning (S3ObjectVersioningManager): Complete versioning support
- Server-Side Encryption (S3ServerSideEncryptionManager): AES-256 encryption with key management
- Permission Checking: Integrated policy evaluation for all operations

### 6. Service Registration

- Extended existing AddMongoGridFS method to register S3 services
- Automatic dependency injection setup
- Enhanced: Proper master key configuration from environment variables

## Usage Example

### Setup in Program.cs:

```csharp
builder.Services.AddMongoGridFS(builder.Configuration);
```

### Configure Middleware:

```csharp
app.UseS3Authentication(); // Optional
app.MapControllers();
```

### Use with AWS CLI:

```bash
# Configure AWS CLI to point to your endpoint
aws configure set endpoint_url http://localhost:5000/s3

# Upload a file
aws s3 cp myfile.txt s3://mybucket/myfile.txt

# List objects
aws s3 ls s3://mybucket/

# Download a file
aws s3 cp s3://mybucket/myfile.txt downloaded.txt
```

### Use with .NET SDK:

```csharp
var s3Client = new AmazonS3Client(
    "dummy-access-key",
    "dummy-secret-key",
    new AmazonS3Config
    {
        ServiceURL = "http://localhost:5000/s3",
        ForcePathStyle = true
    });

// Upload
await s3Client.PutObjectAsync(new PutObjectRequest
{
    BucketName = "mybucket",
    Key = "myfile.txt",
    ContentBody = "Hello, GridFS!"
});

// Download
var response = await s3Client.GetObjectAsync("mybucket", "myfile.txt");
```

## File Structure

```
src/EasilyNET.Mongo.AspNetCore/
├── Abstraction/
│   ├── IObjectStorage.cs
│   ├── GridFSObjectStorage.cs
│   └── IGridFSBucketFactory.cs
├── Controllers/
│   └── S3CompatibleController.cs
├── Middleware/
│   └── S3AuthenticationMiddleware.cs
├── Security/
│   └── S3IamPolicyManager.cs
├── Versioning/
│   └── S3ObjectVersioningManager.cs
├── Encryption/
│   └── S3ServerSideEncryptionManager.cs
├── GridFSCollectionExtensions.cs
└── S3-API-README.md
```

## Production Notes

- Authentication: Implement full AWS Signature Version 4 verification
- Authorization: Add IAM-style access control
- HTTPS: Use SSL/TLS in production
- Rate Limiting: Implement request throttling
- Master Key: Configure EASILYNET_MASTER_KEY environment variable for encryption
- Monitoring: Add logging and metrics

</details>
