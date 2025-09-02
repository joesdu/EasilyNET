using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using EasilyNET.Mongo.AspNetCore.Abstraction;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace WebApi.Test.Unit.Controllers;

/// <summary>
///     <para xml:lang="en">S3 Compatible API Integration Test Controller</para>
///     <para xml:lang="zh">S3兼容API集成测试控制器</para>
/// </summary>
[ApiController]
[Route("api/s3-tests")]
[Produces("application/json")]
public class S3IntegrationTestController : ControllerBase
{
    private readonly IObjectStorage _objectStorage;

    /// <summary>
    ///     <para xml:lang="en">Constructor</para>
    ///     <para xml:lang="zh">构造函数</para>
    /// </summary>
    public S3IntegrationTestController(IObjectStorage objectStorage)
    {
        _objectStorage = objectStorage;
    }

    /// <summary>
    ///     <para xml:lang="en">Test PUT Object functionality</para>
    ///     <para xml:lang="zh">测试PUT对象功能</para>
    /// </summary>
    [HttpPost("put-object")]
    [ProducesResponseType(typeof(TestResult), 200)]
    public async Task<IActionResult> TestPutObject()
    {
        try
        {
            const string bucketName = "test-bucket";
            const string key = "test-object.txt";
            const string content = "Hello, S3 Compatible API!";
            var contentBytes = Encoding.UTF8.GetBytes(content);

            // Create bucket first
            await _objectStorage.CreateBucketAsync(bucketName);

            // Put object
            using var contentStream = new MemoryStream(contentBytes);
            await _objectStorage.PutObjectAsync(bucketName, key, contentStream, "text/plain");

            return Ok(new TestResult
            {
                TestName = "TestPutObject",
                Success = true,
                Message = "PUT object test passed",
                Details = new
                {
                    BucketName = bucketName,
                    Key = key,
                    ContentLength = contentBytes.Length,
                    ContentType = "text/plain"
                }
            });
        }
        catch (Exception ex)
        {
            return Ok(new TestResult
            {
                TestName = "TestPutObject",
                Success = false,
                Message = $"PUT object test failed: {ex.Message}",
                Error = ex.ToString()
            });
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Test GET Object functionality</para>
    ///     <para xml:lang="zh">测试GET对象功能</para>
    /// </summary>
    [HttpGet("get-object")]
    [ProducesResponseType(typeof(TestResult), 200)]
    public async Task<IActionResult> TestGetObject()
    {
        try
        {
            const string bucketName = "test-bucket";
            const string key = "test-object-get.txt";
            const string content = "Test content for GET";
            var contentBytes = Encoding.UTF8.GetBytes(content);

            // Put object first
            await _objectStorage.CreateBucketAsync(bucketName);
            using (var contentStream = new MemoryStream(contentBytes))
            {
                await _objectStorage.PutObjectAsync(bucketName, key, contentStream, "text/plain");
            }

            // Get object
            using var getStream = await _objectStorage.GetObjectAsync(bucketName, key);
            using var reader = new StreamReader(getStream);
            var retrievedContent = await reader.ReadToEndAsync();

            var success = retrievedContent == content;

            return Ok(new TestResult
            {
                TestName = "TestGetObject",
                Success = success,
                Message = success ? "GET object test passed" : "GET object test failed - content mismatch",
                Details = new
                {
                    BucketName = bucketName,
                    Key = key,
                    ExpectedContent = content,
                    RetrievedContent = retrievedContent,
                    ContentLength = retrievedContent.Length
                }
            });
        }
        catch (Exception ex)
        {
            return Ok(new TestResult
            {
                TestName = "TestGetObject",
                Success = false,
                Message = $"GET object test failed: {ex.Message}",
                Error = ex.ToString()
            });
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Test HEAD Object functionality</para>
    ///     <para xml:lang="zh">测试HEAD对象功能</para>
    /// </summary>
    [HttpHead("head-object")]
    [ProducesResponseType(typeof(TestResult), 200)]
    public async Task<IActionResult> TestHeadObject()
    {
        try
        {
            const string bucketName = "test-bucket";
            const string key = "test-object-head.txt";
            const string content = "Test content for HEAD";
            var contentBytes = Encoding.UTF8.GetBytes(content);

            // Put object first
            await _objectStorage.CreateBucketAsync(bucketName);
            using (var contentStream = new MemoryStream(contentBytes))
            {
                await _objectStorage.PutObjectAsync(bucketName, key, contentStream, "text/plain");
            }

            // Get object metadata
            var metadata = await _objectStorage.GetObjectMetadataAsync(bucketName, key);

            return Ok(new TestResult
            {
                TestName = "TestHeadObject",
                Success = true,
                Message = "HEAD object test passed",
                Details = new
                {
                    BucketName = bucketName,
                    Key = key,
                    ContentType = metadata.ContentType,
                    ContentLength = metadata.ContentLength,
                    LastModified = metadata.LastModified,
                    ETag = metadata.ETag
                }
            });
        }
        catch (Exception ex)
        {
            return Ok(new TestResult
            {
                TestName = "TestHeadObject",
                Success = false,
                Message = $"HEAD object test failed: {ex.Message}",
                Error = ex.ToString()
            });
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Test List Objects V2 functionality</para>
    ///     <para xml:lang="zh">测试ListObjectsV2功能</para>
    /// </summary>
    [HttpGet("list-objects-v2")]
    [ProducesResponseType(typeof(TestResult), 200)]
    public async Task<IActionResult> TestListObjectsV2()
    {
        try
        {
            const string bucketName = "test-bucket";

            // Create bucket and put some objects
            await _objectStorage.CreateBucketAsync(bucketName);
            for (var i = 1; i <= 3; i++)
            {
                var key = $"test-list-object-{i}.txt";
                var content = $"Content for object {i}";
                var contentBytes = Encoding.UTF8.GetBytes(content);
                using var contentStream = new MemoryStream(contentBytes);
                await _objectStorage.PutObjectAsync(bucketName, key, contentStream, "text/plain");
            }

            // List objects
            var result = await _objectStorage.ListObjectsV2Async(bucketName);

            return Ok(new TestResult
            {
                TestName = "TestListObjectsV2",
                Success = true,
                Message = "ListObjectsV2 test passed",
                Details = new
                {
                    BucketName = bucketName,
                    ObjectCount = result.Objects.Count,
                    IsTruncated = result.IsTruncated,
                    Objects = result.Objects.Select(obj => new
                    {
                        obj.Key,
                        obj.Size,
                        obj.LastModified,
                        obj.ETag
                    })
                }
            });
        }
        catch (Exception ex)
        {
            return Ok(new TestResult
            {
                TestName = "TestListObjectsV2",
                Success = false,
                Message = $"ListObjectsV2 test failed: {ex.Message}",
                Error = ex.ToString()
            });
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Test Create Bucket functionality</para>
    ///     <para xml:lang="zh">测试创建存储桶功能</para>
    /// </summary>
    [HttpPost("create-bucket")]
    [ProducesResponseType(typeof(TestResult), 200)]
    public async Task<IActionResult> TestCreateBucket()
    {
        try
        {
            const string bucketName = "test-create-bucket";

            // Create bucket
            await _objectStorage.CreateBucketAsync(bucketName);

            // Verify bucket exists
            var exists = await _objectStorage.BucketExistsAsync(bucketName);

            return Ok(new TestResult
            {
                TestName = "TestCreateBucket",
                Success = exists,
                Message = exists ? "Create bucket test passed" : "Create bucket test failed - bucket not found after creation",
                Details = new
                {
                    BucketName = bucketName,
                    BucketExists = exists
                }
            });
        }
        catch (Exception ex)
        {
            return Ok(new TestResult
            {
                TestName = "TestCreateBucket",
                Success = false,
                Message = $"Create bucket test failed: {ex.Message}",
                Error = ex.ToString()
            });
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Test Delete Object functionality</para>
    ///     <para xml:lang="zh">测试删除对象功能</para>
    /// </summary>
    [HttpDelete("delete-object")]
    [ProducesResponseType(typeof(TestResult), 200)]
    public async Task<IActionResult> TestDeleteObject()
    {
        try
        {
            const string bucketName = "test-bucket";
            const string key = "test-object-delete.txt";
            const string content = "Test content for DELETE";
            var contentBytes = Encoding.UTF8.GetBytes(content);

            // Put object first
            await _objectStorage.CreateBucketAsync(bucketName);
            using (var contentStream = new MemoryStream(contentBytes))
            {
                await _objectStorage.PutObjectAsync(bucketName, key, contentStream, "text/plain");
            }

            // Delete object
            await _objectStorage.DeleteObjectAsync(bucketName, key);

            // Verify object is deleted
            var exists = await _objectStorage.ObjectExistsAsync(bucketName, key);

            return Ok(new TestResult
            {
                TestName = "TestDeleteObject",
                Success = !exists,
                Message = !exists ? "Delete object test passed" : "Delete object test failed - object still exists",
                Details = new
                {
                    BucketName = bucketName,
                    Key = key,
                    ObjectExistsAfterDelete = exists
                }
            });
        }
        catch (Exception ex)
        {
            return Ok(new TestResult
            {
                TestName = "TestDeleteObject",
                Success = false,
                Message = $"Delete object test failed: {ex.Message}",
                Error = ex.ToString()
            });
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Test PUT Encrypted Object functionality</para>
    ///     <para xml:lang="zh">测试PUT加密对象功能</para>
    /// </summary>
    [HttpPost("put-encrypted-object")]
    [ProducesResponseType(typeof(TestResult), 200)]
    public async Task<IActionResult> TestPutEncryptedObject()
    {
        try
        {
            const string bucketName = "test-bucket";
            const string key = "test-encrypted-object.txt";
            const string content = "This is encrypted content";
            var contentBytes = Encoding.UTF8.GetBytes(content);

            await _objectStorage.CreateBucketAsync(bucketName);

            // Put encrypted object
            using var contentStream = new MemoryStream(contentBytes);
            var encryptionConfig = new ServerSideEncryptionConfiguration
            {
                Enabled = true,
                Algorithm = "AES256"
            };

            var result = await _objectStorage.PutEncryptedObjectAsync(bucketName, key, contentStream, "text/plain", null, encryptionConfig);

            return Ok(new TestResult
            {
                TestName = "TestPutEncryptedObject",
                Success = true,
                Message = "PUT encrypted object test passed",
                Details = new
                {
                    BucketName = bucketName,
                    Key = key,
                    EncryptionAlgorithm = result.ServerSideEncryption,
                    EncryptionKeyId = result.EncryptionKeyId,
                    ETag = result.ETag
                }
            });
        }
        catch (Exception ex)
        {
            return Ok(new TestResult
            {
                TestName = "TestPutEncryptedObject",
                Success = false,
                Message = $"PUT encrypted object test failed: {ex.Message}",
                Error = ex.ToString()
            });
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Test GET Object Version functionality</para>
    ///     <para xml:lang="zh">测试GET对象版本功能</para>
    /// </summary>
    [HttpGet("get-object-version")]
    [ProducesResponseType(typeof(TestResult), 200)]
    public async Task<IActionResult> TestGetObjectVersion()
    {
        try
        {
            const string bucketName = "test-bucket";
            const string key = "test-version-object.txt";
            const string content = "Version test content";
            var contentBytes = Encoding.UTF8.GetBytes(content);

            // Put object first
            await _objectStorage.CreateBucketAsync(bucketName);
            using (var contentStream = new MemoryStream(contentBytes))
            {
                await _objectStorage.PutObjectAsync(bucketName, key, contentStream, "text/plain");
            }

            // Get object version
            var version = await _objectStorage.GetObjectVersionAsync(bucketName, key);

            return Ok(new TestResult
            {
                TestName = "TestGetObjectVersion",
                Success = true,
                Message = "GET object version test passed",
                Details = new
                {
                    BucketName = bucketName,
                    Key = key,
                    VersionId = version.VersionId,
                    Size = version.Size,
                    LastModified = version.LastModified,
                    IsLatest = version.IsLatest,
                    ETag = version.ETag
                }
            });
        }
        catch (Exception ex)
        {
            return Ok(new TestResult
            {
                TestName = "TestGetObjectVersion",
                Success = false,
                Message = $"GET object version test failed: {ex.Message}",
                Error = ex.ToString()
            });
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Test List Object Versions functionality</para>
    ///     <para xml:lang="zh">测试列出对象版本功能</para>
    /// </summary>
    [HttpGet("list-object-versions")]
    [ProducesResponseType(typeof(TestResult), 200)]
    public async Task<IActionResult> TestListObjectVersions()
    {
        try
        {
            const string bucketName = "test-bucket";

            // Create bucket and put some objects
            await _objectStorage.CreateBucketAsync(bucketName);
            for (var i = 1; i <= 2; i++)
            {
                var key = $"test-version-list-{i}.txt";
                var content = $"Version content {i}";
                var contentBytes = Encoding.UTF8.GetBytes(content);
                using var contentStream = new MemoryStream(contentBytes);
                await _objectStorage.PutObjectAsync(bucketName, key, contentStream, "text/plain");
            }

            // List object versions
            var result = await _objectStorage.ListObjectVersionsAsync(bucketName);

            return Ok(new TestResult
            {
                TestName = "TestListObjectVersions",
                Success = true,
                Message = "List object versions test passed",
                Details = new
                {
                    BucketName = bucketName,
                    VersionCount = result.Versions.Count,
                    DeleteMarkerCount = result.DeleteMarkers.Count,
                    Versions = result.Versions.Select(v => new
                    {
                        v.Key,
                        v.VersionId,
                        v.Size,
                        v.LastModified,
                        v.IsLatest
                    })
                }
            });
        }
        catch (Exception ex)
        {
            return Ok(new TestResult
            {
                TestName = "TestListObjectVersions",
                Success = false,
                Message = $"List object versions test failed: {ex.Message}",
                Error = ex.ToString()
            });
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Test Range Request functionality</para>
    ///     <para xml:lang="zh">测试范围请求功能</para>
    /// </summary>
    [HttpGet("range-request")]
    [ProducesResponseType(typeof(TestResult), 200)]
    public async Task<IActionResult> TestRangeRequest()
    {
        try
        {
            const string bucketName = "test-bucket";
            const string key = "test-range-object.txt";
            const string content = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var contentBytes = Encoding.UTF8.GetBytes(content);

            // Put object first
            await _objectStorage.CreateBucketAsync(bucketName);
            using (var contentStream = new MemoryStream(contentBytes))
            {
                await _objectStorage.PutObjectAsync(bucketName, key, contentStream, "text/plain");
            }

            // Get range (bytes 5-14)
            using var rangeStream = await _objectStorage.GetObjectAsync(bucketName, key, "bytes=5-14");
            using var reader = new StreamReader(rangeStream);
            var rangeContent = await reader.ReadToEndAsync();

            var expectedRange = "56789ABCDE";
            var success = rangeContent == expectedRange;

            return Ok(new TestResult
            {
                TestName = "TestRangeRequest",
                Success = success,
                Message = success ? "Range request test passed" : "Range request test failed - content mismatch",
                Details = new
                {
                    BucketName = bucketName,
                    Key = key,
                    Range = "bytes=5-14",
                    ExpectedContent = expectedRange,
                    RetrievedContent = rangeContent,
                    FullContentLength = content.Length
                }
            });
        }
        catch (Exception ex)
        {
            return Ok(new TestResult
            {
                TestName = "TestRangeRequest",
                Success = false,
                Message = $"Range request test failed: {ex.Message}",
                Error = ex.ToString()
            });
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Test Multipart Upload functionality</para>
    ///     <para xml:lang="zh">测试多部分上传功能</para>
    /// </summary>
    [HttpPost("multipart-upload")]
    [ProducesResponseType(typeof(TestResult), 200)]
    public async Task<IActionResult> TestMultipartUpload()
    {
        try
        {
            const string bucketName = "test-bucket";
            const string key = "test-multipart-object.txt";
            const string content = "This is a test for multipart upload functionality.";
            var contentBytes = Encoding.UTF8.GetBytes(content);

            await _objectStorage.CreateBucketAsync(bucketName);

            // Initiate multipart upload
            var initiateResult = await _objectStorage.InitiateMultipartUploadAsync(bucketName, key, "text/plain");

            // Upload parts
            var parts = new List<PartETag>();
            var partSize = 10;
            for (var i = 0; i < contentBytes.Length; i += partSize)
            {
                var partNumber = (i / partSize) + 1;
                var partData = contentBytes.Skip(i).Take(partSize).ToArray();

                using var partStream = new MemoryStream(partData);
                var uploadResult = await _objectStorage.UploadPartAsync(bucketName, key, initiateResult.UploadId, partNumber, partStream);
                parts.Add(new PartETag { PartNumber = partNumber, ETag = uploadResult.ETag.Trim('"') });
            }

            // Complete multipart upload
            var completeResult = await _objectStorage.CompleteMultipartUploadAsync(bucketName, key, initiateResult.UploadId, parts);

            // Verify the uploaded content
            using var getStream = await _objectStorage.GetObjectAsync(bucketName, key);
            using var reader = new StreamReader(getStream);
            var retrievedContent = await reader.ReadToEndAsync();

            var success = retrievedContent == content;

            return Ok(new TestResult
            {
                TestName = "TestMultipartUpload",
                Success = success,
                Message = success ? "Multipart upload test passed" : "Multipart upload test failed - content mismatch",
                Details = new
                {
                    BucketName = bucketName,
                    Key = key,
                    UploadId = initiateResult.UploadId,
                    PartCount = parts.Count,
                    TotalSize = content.Length,
                    ETag = completeResult.ETag,
                    ExpectedContent = content,
                    RetrievedContent = retrievedContent
                }
            });
        }
        catch (Exception ex)
        {
            return Ok(new TestResult
            {
                TestName = "TestMultipartUpload",
                Success = false,
                Message = $"Multipart upload test failed: {ex.Message}",
                Error = ex.ToString()
            });
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Run all S3 integration tests</para>
    ///     <para xml:lang="zh">运行所有S3集成测试</para>
    /// </summary>
    [HttpGet("run-all")]
    [ProducesResponseType(typeof(TestSuiteResult), 200)]
    public async Task<IActionResult> RunAllTests()
    {
        var results = new List<TestResult>();

        // Run all individual tests
        var putObjectResult = (await TestPutObject() as OkObjectResult)?.Value as TestResult;
        if (putObjectResult != null) results.Add(putObjectResult);

        var getObjectResult = (await TestGetObject() as OkObjectResult)?.Value as TestResult;
        if (getObjectResult != null) results.Add(getObjectResult);

        var headObjectResult = (await TestHeadObject() as OkObjectResult)?.Value as TestResult;
        if (headObjectResult != null) results.Add(headObjectResult);

        var listObjectsResult = (await TestListObjectsV2() as OkObjectResult)?.Value as TestResult;
        if (listObjectsResult != null) results.Add(listObjectsResult);

        var createBucketResult = (await TestCreateBucket() as OkObjectResult)?.Value as TestResult;
        if (createBucketResult != null) results.Add(createBucketResult);

        var deleteObjectResult = (await TestDeleteObject() as OkObjectResult)?.Value as TestResult;
        if (deleteObjectResult != null) results.Add(deleteObjectResult);

        var putEncryptedResult = (await TestPutEncryptedObject() as OkObjectResult)?.Value as TestResult;
        if (putEncryptedResult != null) results.Add(putEncryptedResult);

        var getVersionResult = (await TestGetObjectVersion() as OkObjectResult)?.Value as TestResult;
        if (getVersionResult != null) results.Add(getVersionResult);

        var listVersionsResult = (await TestListObjectVersions() as OkObjectResult)?.Value as TestResult;
        if (listVersionsResult != null) results.Add(listVersionsResult);

        var rangeRequestResult = (await TestRangeRequest() as OkObjectResult)?.Value as TestResult;
        if (rangeRequestResult != null) results.Add(rangeRequestResult);

        var multipartResult = (await TestMultipartUpload() as OkObjectResult)?.Value as TestResult;
        if (multipartResult != null) results.Add(multipartResult);

        var passedTests = results.Count(r => r.Success);
        var failedTests = results.Count(r => !r.Success);

        return Ok(new TestSuiteResult
        {
            SuiteName = "S3 Compatible API Integration Tests",
            TotalTests = results.Count,
            PassedTests = passedTests,
            FailedTests = failedTests,
            SuccessRate = results.Count > 0 ? (double)passedTests / results.Count * 100 : 0,
            Results = results,
            Timestamp = DateTime.UtcNow
        });
    }
}

/// <summary>
///     <para xml:lang="en">Test Result Model</para>
///     <para xml:lang="zh">测试结果模型</para>
/// </summary>
public class TestResult
{
    /// <summary>
    ///     <para xml:lang="en">Test name</para>
    ///     <para xml:lang="zh">测试名称</para>
    /// </summary>
    public string TestName { get; set; } = string.Empty;

    /// <summary>
    ///     <para xml:lang="en">Whether the test passed</para>
    ///     <para xml:lang="zh">测试是否通过</para>
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Test message</para>
    ///     <para xml:lang="zh">测试消息</para>
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    ///     <para xml:lang="en">Test details</para>
    ///     <para xml:lang="zh">测试详情</para>
    /// </summary>
    public object? Details { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Error message if test failed</para>
    ///     <para xml:lang="zh">如果测试失败的错误消息</para>
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Execution time in milliseconds</para>
    ///     <para xml:lang="zh">执行时间（毫秒）</para>
    /// </summary>
    public long ExecutionTimeMs { get; set; }
}

/// <summary>
///     <para xml:lang="en">Test Suite Result Model</para>
///     <para xml:lang="zh">测试套件结果模型</para>
/// </summary>
public class TestSuiteResult
{
    /// <summary>
    ///     <para xml:lang="en">Test suite name</para>
    ///     <para xml:lang="zh">测试套件名称</para>
    /// </summary>
    public string SuiteName { get; set; } = string.Empty;

    /// <summary>
    ///     <para xml:lang="en">Total number of tests</para>
    ///     <para xml:lang="zh">测试总数</para>
    /// </summary>
    public int TotalTests { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Number of passed tests</para>
    ///     <para xml:lang="zh">通过的测试数量</para>
    /// </summary>
    public int PassedTests { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Number of failed tests</para>
    ///     <para xml:lang="zh">失败的测试数量</para>
    /// </summary>
    public int FailedTests { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Success rate percentage</para>
    ///     <para xml:lang="zh">成功率百分比</para>
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Individual test results</para>
    ///     <para xml:lang="zh">单个测试结果</para>
    /// </summary>
    public List<TestResult> Results { get; set; } = [];

    /// <summary>
    ///     <para xml:lang="en">Test execution timestamp</para>
    ///     <para xml:lang="zh">测试执行时间戳</para>
    /// </summary>
    public DateTime Timestamp { get; set; }
}