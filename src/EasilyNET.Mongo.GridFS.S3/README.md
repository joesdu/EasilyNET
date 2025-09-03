# EasilyNET.Mongo.GridFS.S3

基于 MongoDB GridFS 的 S3 兼容 API 实现，为 MongoDB 提供完整的 S3 REST API 接口。

## 功能特性

- ✅ **完整的 S3 API 兼容性** - 支持所有核心 S3 操作
- ✅ **多部分上传** - 支持大文件分块上传
- ✅ **服务器端加密** - AES-256 加密支持
- ✅ **对象版本控制** - 完整的版本管理功能
- ✅ **IAM 策略管理** - 细粒度的访问控制
- ✅ **流式处理** - 高效的内存使用
- ✅ **元数据支持** - 自定义元数据存储
- ✅ **Range 请求** - 断点续传支持

## 快速开始

### 安装依赖

```bash
dotnet add package EasilyNET.Mongo.GridFS.S3
```

### 配置服务

在 `Program.cs` 中注册服务：

```csharp
var builder = WebApplication.CreateBuilder(args);

// 注册 MongoDB 和 GridFS
builder.Services.AddMongoContext<YourDbContext>(builder.Configuration);
builder.Services.AddMongoGridFS(builder.Configuration);

// 注册 S3 兼容服务
builder.Services.AddMongoS3Services(builder.Configuration);

var app = builder.Build();

// 启用 S3 认证中间件（可选）
app.UseS3Authentication();

// 映射控制器
app.MapControllers();

app.Run();
```

### 配置认证

在 `appsettings.json` 中配置访问密钥：

```json
{
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
    "MasterKey": "Your32CharacterMasterKey123456789012"
  }
}
```

## API 端点

### 基础对象操作

- `PUT /s3/{bucket}/{key}` - 上传对象
- `GET /s3/{bucket}/{key}` - 下载对象
- `HEAD /s3/{bucket}/{key}` - 获取对象元数据
- `DELETE /s3/{bucket}/{key}` - 删除对象

### 高级功能

- `POST /s3/{bucket}/upload/{key}?uploads=1` - 初始化多部分上传
- `PUT /s3/{bucket}/part/{key}?uploadId=...&partNumber=...` - 上传部分
- `POST /s3/{bucket}/complete/{key}?uploadId=...` - 完成多部分上传
- `GET /s3/{bucket}/list?list-type=2` - 列出对象（V2）
- `PUT /s3/{bucket}` - 创建存储桶
- `DELETE /s3/{bucket}` - 删除存储桶

## 客户端兼容性

### AWS CLI

```bash
# 配置端点
aws configure set endpoint_url http://localhost:5000/s3

# 基本操作
aws s3 cp file.txt s3://mybucket/file.txt
aws s3 ls s3://mybucket/
aws s3 cp s3://mybucket/file.txt downloaded.txt
```

### AWS SDK for .NET

```csharp
var s3Client = new AmazonS3Client(
    "access-key",
    "secret-key",
    new AmazonS3Config
    {
        ServiceURL = "http://localhost:5000/s3",
        ForcePathStyle = true
    });

// 上传
await s3Client.PutObjectAsync(new PutObjectRequest
{
    BucketName = "mybucket",
    Key = "file.txt",
    ContentBody = "Hello World"
});

// 下载
var response = await s3Client.GetObjectAsync("mybucket", "file.txt");
```

### MinIO Client

```bash
mc alias set gridfs http://localhost:5000/s3 access-key secret-key --api S3v4
mc cp file.txt gridfs/mybucket/
```

## 安全特性

### 服务器端加密

设置环境变量启用 AES-256 加密：

```bash
export EASILYNET_MASTER_KEY="Your32CharacterMasterKey123456789012"
```

### IAM 策略管理

```csharp
// 创建管理员策略
var adminPolicy = S3IamPolicyManager.CreateAdminPolicy();
iam.AddPolicy("Admin", adminPolicy);

// 创建用户
iam.AddUser("admin", "AKIAIOSFODNN7EXAMPLE", "secret-key", ["Admin"]);
```

## 性能优化

- **流式处理** - 避免大文件内存加载
- **智能缓存** - 元数据缓存减少数据库查询
- **异步操作** - 高并发支持
- **连接池** - MongoDB 连接池优化

## 许可证

本项目采用 MIT 许可证。