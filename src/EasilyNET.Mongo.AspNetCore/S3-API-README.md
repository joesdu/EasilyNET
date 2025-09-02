# GridFS S3 Compatible API

This module provides S3-compatible REST API endpoints for MongoDB GridFS, allowing you to use standard S3 clients and tools to interact with GridFS data.

## Features

- **S3 Compatible API**: Supports major S3 operations (PutObject, GetObject, DeleteObject, ListObjects, HeadObject)
- **Standard S3 Clients**: Works with AWS CLI, SDKs, and other S3-compatible tools
- **GridFS Backend**: All data is stored in MongoDB GridFS
- **Metadata Support**: Supports custom metadata via S3 metadata headers
- **Security Features**: IAM policies, server-side encryption, object versioning
- **Authentication**: AWS Signature Version 4 compatible authentication

## Setup

### 1. Register Services

```csharp
// In Program.cs or Startup.cs
builder.Services.AddMongoGridFS(builder.Configuration);
```

### 2. Configure Authentication

```csharp
// In Program.cs
app.UseS3Authentication(new S3AuthenticationOptions
{
    Enabled = true,
    RequireAuthentication = true,
    AccessKeys = new Dictionary<string, string>
    {
        ["AKIAIOSFODNN7EXAMPLE"] = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY"
    }
});
app.MapControllers();
```

### 3. Configure Master Key for Encryption

```bash
# Set master key for server-side encryption
export EASILYNET_MASTER_KEY="Your32CharacterMasterKey123456789012"
```

### 4. Configure S3 Client

Configure your S3 client to point to your GridFS endpoint:

```bash
# AWS CLI example
aws configure set aws_access_key_id AKIAIOSFODNN7EXAMPLE
aws configure set aws_secret_access_key wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY
aws configure set region us-east-1
aws configure set endpoint_url http://localhost:5000/s3

# Upload a file
aws s3 cp myfile.txt s3://mybucket/myfile.txt

# List objects
aws s3 ls s3://mybucket/

# Download a file
aws s3 cp s3://mybucket/myfile.txt downloaded.txt
```

## API Endpoints

### PUT /{bucket}/{key}

Upload an object to GridFS.

**Headers:**

- `Content-Type`: Object content type
- `x-amz-meta-*`: Custom metadata
- `x-amz-server-side-encryption`: Enable server-side encryption (AES256)
- `x-amz-server-side-encryption-aws-kms-key-id`: KMS key ID (optional)

**Example:**

```bash
curl -X PUT -H "Content-Type: text/plain" \
     -H "x-amz-meta-author: John Doe" \
     -H "x-amz-server-side-encryption: AES256" \
     -d "Hello, World!" \
     http://localhost:5000/s3/mybucket/myfile.txt
```

### GET /{bucket}/{key}

Download an object from GridFS.

**Query Parameters:**

- `versionId`: Get specific version of object

**Response Headers:**

- `Content-Type`: Object content type
- `Content-Length`: Object size
- `ETag`: Object ETag
- `Last-Modified`: Last modification time
- `x-amz-meta-*`: Custom metadata
- `x-amz-version-id`: Object version ID

**Example:**

```bash
curl http://localhost:5000/s3/mybucket/myfile.txt
```

### DELETE /{bucket}/{key}

Delete an object from GridFS.

**Query Parameters:**

- `versionId`: Delete specific version of object

**Example:**

```bash
curl -X DELETE http://localhost:5000/s3/mybucket/myfile.txt
```

### HEAD /{bucket}/{key}

Get object metadata without downloading the content.

**Response Headers:** Same as GET request.

### GET /{bucket}?prefix={prefix}&marker={marker}&max-keys={maxKeys}

List objects in a bucket.

**Query Parameters:**

- `prefix`: Filter objects by key prefix
- `marker`: Pagination marker
- `max-keys`: Maximum number of keys to return (default: 1000)
- `continuation-token`: Continuation token for V2 API
- `start-after`: Start listing after this key

**Example:**

```bash
curl "http://localhost:5000/s3/mybucket?prefix=docs/&max-keys=10"
```

## Security Features

### IAM Policies

The system supports JSON-based IAM policies for fine-grained access control:

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

### Server-Side Encryption

Objects can be encrypted server-side using AES-256:

```csharp
// Upload encrypted object
await s3Client.PutObjectAsync(new PutObjectRequest
{
    BucketName = "mybucket",
    Key = "encrypted-file.txt",
    ContentBody = "Secret data",
    ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
});
```

### Object Versioning

Enable versioning to keep multiple versions of objects:

```csharp
// Enable versioning
var versioningManager = new S3ObjectVersioningManager();
versioningManager.EnableVersioning("mybucket");

// List versions
var versions = versioningManager.ListVersions("mybucket", "myfile.txt");
```

## Authentication

The system implements AWS Signature Version 4 authentication:

### Signature Calculation

```csharp
// The middleware automatically validates signatures
// No manual signature calculation needed for clients
```

### Access Keys

Configure access keys in the authentication options:

```csharp
new S3AuthenticationOptions
{
    AccessKeys = new Dictionary<string, string>
    {
        ["AKIAIOSFODNN7EXAMPLE"] = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY"
    }
}
```

## Advanced Features

### Multipart Upload

Support for large file uploads through multipart operations:

```csharp
// Initiate multipart upload
var initiateResponse = await s3Client.InitiateMultipartUploadAsync(new InitiateMultipartUploadRequest
{
    BucketName = "mybucket",
    Key = "large-file.zip"
});

// Upload parts
var parts = new List<PartETag>();
for (int i = 0; i < partCount; i++)
{
    var uploadResponse = await s3Client.UploadPartAsync(new UploadPartRequest
    {
        BucketName = "mybucket",
        Key = "large-file.zip",
        UploadId = initiateResponse.UploadId,
        PartNumber = i + 1,
        InputStream = partStream
    });
    parts.Add(new PartETag(uploadResponse.PartNumber, uploadResponse.ETag));
}

// Complete upload
await s3Client.CompleteMultipartUploadAsync(new CompleteMultipartUploadRequest
{
    BucketName = "mybucket",
    Key = "large-file.zip",
    UploadId = initiateResponse.UploadId,
    PartETags = parts
});
```

### Range Requests

Support for partial content retrieval:

```bash
# Download first 1024 bytes
curl -H "Range: bytes=0-1023" http://localhost:5000/s3/mybucket/large-file.zip
```

## Production Considerations

1. **Authentication**: Use proper AWS Signature Version 4 verification
2. **Authorization**: Implement comprehensive IAM policies
3. **Rate Limiting**: Add rate limiting to prevent abuse
4. **Logging**: Implement comprehensive audit logging
5. **Error Handling**: Improve error responses to match S3 specifications
6. **Performance**: Add caching and optimization for large files
7. **Security**: Use HTTPS in production
8. **Monitoring**: Add metrics and health checks
9. **Backup**: Implement regular GridFS backups
10. **Scaling**: Consider MongoDB sharding for large deployments

## Example Usage

### Using AWS SDK for .NET

```csharp
using Amazon.S3;
using Amazon.S3.Model;

var s3Client = new AmazonS3Client(
    "AKIAIOSFODNN7EXAMPLE",
    "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
    new AmazonS3Config
    {
        ServiceURL = "http://localhost:5000/s3",
        ForcePathStyle = true
    });

// Upload with encryption
await s3Client.PutObjectAsync(new PutObjectRequest
{
    BucketName = "mybucket",
    Key = "myfile.txt",
    ContentBody = "Hello, GridFS!",
    ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
});

// Download
var response = await s3Client.GetObjectAsync("mybucket", "myfile.txt");
using var reader = new StreamReader(response.ResponseStream);
var content = await reader.ReadToEndAsync();
```

### Using AWS CLI

```bash
# Configure CLI
aws configure set aws_access_key_id AKIAIOSFODNN7EXAMPLE
aws configure set aws_secret_access_key wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY
aws configure set endpoint_url http://localhost:5000/s3

# Operations
aws s3 cp myfile.txt s3://mybucket/myfile.txt
aws s3 ls s3://mybucket/
aws s3 cp s3://mybucket/myfile.txt downloaded.txt
aws s3 rm s3://mybucket/myfile.txt
```

## Troubleshooting

### Common Issues

1. **Signature Mismatch**: Check system clock synchronization
2. **Access Denied**: Verify IAM policies and access keys
3. **Invalid Key ID**: Ensure access key format is correct
4. **Encryption Errors**: Verify master key configuration
5. **Version Not Found**: Check if versioning is enabled

### Debug Mode

Enable debug logging to troubleshoot issues:

```csharp
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);
```

## API Compatibility

This implementation is compatible with:

- AWS CLI
- AWS SDK for .NET
- AWS SDK for Java
- AWS SDK for Python (Boto3)
- MinIO Client (mc)
- Other S3-compatible tools

The API follows AWS S3 REST API specifications for maximum compatibility.
