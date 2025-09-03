namespace EasilyNET.Mongo.AspNetCore.Security;

/// <summary>
///     <para xml:lang="en">S3 Action Types</para>
///     <para xml:lang="zh">S3操作类型</para>
/// </summary>
// ReSharper disable once UnusedType.Global
public static class S3Actions
{
    // Object operations
#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释
    public const string GetObject = "s3:GetObject";
    public const string PutObject = "s3:PutObject";
    public const string DeleteObject = "s3:DeleteObject";
    public const string CopyObject = "s3:CopyObject";
    public const string HeadObject = "s3:HeadObject";

    // Bucket operations
    public const string ListBucket = "s3:ListBucket";
    public const string CreateBucket = "s3:CreateBucket";
    public const string DeleteBucket = "s3:DeleteBucket";
    public const string HeadBucket = "s3:HeadBucket";

    // Multipart upload operations
    public const string InitiateMultipartUpload = "s3:InitiateMultipartUpload";
    public const string UploadPart = "s3:UploadPart";
    public const string CompleteMultipartUpload = "s3:CompleteMultipartUpload";
    public const string AbortMultipartUpload = "s3:AbortMultipartUpload";

    // Batch operations
    public const string DeleteObjects = "s3:DeleteObjects";

    // All operations
    public const string All = "s3:*";
#pragma warning restore CS1591 // 缺少对公共可见类型或成员的 XML 注释
}