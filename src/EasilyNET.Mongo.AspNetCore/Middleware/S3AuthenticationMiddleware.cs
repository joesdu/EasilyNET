using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EasilyNET.Mongo.AspNetCore.Middleware;

/// <summary>
///     <para xml:lang="en">S3 Compatible Authentication Middleware</para>
///     <para xml:lang="zh">S3兼容认证中间件</para>
/// </summary>
/// <remarks>
///     <para xml:lang="en">Constructor</para>
///     <para xml:lang="zh">构造函数</para>
/// </remarks>
public class S3AuthenticationMiddleware(RequestDelegate next, ILogger<S3AuthenticationMiddleware> logger, IOptions<S3AuthenticationOptions> options)
{
    private readonly S3AuthenticationOptions _options = options.Value;

    /// <summary>
    ///     <para xml:lang="en">Invoke the middleware</para>
    ///     <para xml:lang="zh">调用中间件</para>
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.Enabled)
        {
            await next(context);
            return;
        }
        var authorization = context.Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(authorization))
        {
            if (_options.RequireAuthentication)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Missing authentication");
                return;
            }
            await next(context);
            return;
        }
        if (!authorization.StartsWith("AWS4-HMAC-SHA256"))
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Invalid authentication method");
            return;
        }
        try
        {
            if (ValidateSignature(context, authorization))
            {
                logger.LogInformation("S3 Authentication successful for request: {Method} {Path}", context.Request.Method, context.Request.Path);
                await next(context);
            }
            else
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("Invalid signature");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating S3 signature");
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync("Authentication error");
        }
    }

    private bool ValidateSignature(HttpContext context, string authorization)
    {
        // Parse authorization header
        // Format: AWS4-HMAC-SHA256 Credential=AKIAIOSFODNN7EXAMPLE/20130524/us-east-1/s3/aws4_request, SignedHeaders=host;range;x-amz-date, Signature=fe5f80f77d5fa3beca038a248ff027d0445342fe2855ddc963176630326f1024
        var authParts = authorization.Split(' ');
        if (authParts is not ["AWS4-HMAC-SHA256", _])
        {
            return false;
        }
        var credentialParts = authParts[1].Split(',');
        var credential = "";
        var signedHeaders = "";
        var signature = "";
        foreach (var part in credentialParts)
        {
            var kvp = part.Split('=');
            if (kvp.Length != 2)
            {
                continue;
            }
            switch (kvp[0])
            {
                case "Credential":
                    credential = kvp[1];
                    break;
                case "SignedHeaders":
                    signedHeaders = kvp[1];
                    break;
                case "Signature":
                    signature = kvp[1];
                    break;
            }
        }

        // Parse credential: AKIAIOSFODNN7EXAMPLE/20130524/us-east-1/s3/aws4_request
        var credParts = credential.Split('/');
        if (credParts.Length != 5)
        {
            return false;
        }
        var accessKey = credParts[0];
        var date = credParts[1];
        var region = credParts[2];
        var service = credParts[3];

        // Get secret key for access key
        if (!_options.AccessKeys.TryGetValue(accessKey, out var secretKey))
        {
            return false;
        }

        // Reconstruct canonical request
        var canonicalRequest = BuildCanonicalRequest(context, signedHeaders);

        // Build string to sign
        var stringToSign = BuildStringToSign(canonicalRequest, date, region, service);

        // Calculate signature
        var calculatedSignature = CalculateSignature(stringToSign, secretKey, date, region, service);
        return string.Equals(calculatedSignature, signature, StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildCanonicalRequest(HttpContext context, string signedHeaders)
    {
        var method = context.Request.Method;
        var canonicalUri = context.Request.Path.Value ?? "/";
        var canonicalQueryString = BuildCanonicalQueryString(context.Request.Query);
        var canonicalHeaders = BuildCanonicalHeaders(context.Request.Headers, signedHeaders);
        var payloadHash = CalculatePayloadHash(context);
        return $"{method}\n{canonicalUri}\n{canonicalQueryString}\n{canonicalHeaders}\n{signedHeaders}\n{payloadHash}";
    }

    private static string BuildCanonicalQueryString(IQueryCollection query)
    {
        var sortedParams = query.OrderBy(p => p.Key).Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value.ToString())}");
        return string.Join("&", sortedParams);
    }

    private static string BuildCanonicalHeaders(IHeaderDictionary headers, string signedHeaders)
    {
        var signedHeaderList = signedHeaders.Split(';');
        var canonicalHeaders = new StringBuilder();
        foreach (var headerName in signedHeaderList.OrderBy(h => h))
        {
            if (!headers.TryGetValue(headerName, out var values))
            {
                continue;
            }
            var headerValue = string.Join(",", values.Where(v => v != null)).Trim();
            canonicalHeaders.Append($"{headerName.ToLower()}:{headerValue}\n");
        }
        return canonicalHeaders.ToString();
    }

    private static string CalculatePayloadHash(HttpContext context)
    {
        // For simplicity, we'll use SHA256 of empty string for GET requests
        // In production, you should hash the actual request body
        var hash = SHA256.HashData(context.Request.Body);
        return Convert.ToHexString(hash).ToLower();
    }

    private static string BuildStringToSign(string canonicalRequest, string date, string region, string service)
    {
        var canonicalRequestHash = SHA256.HashData(Encoding.UTF8.GetBytes(canonicalRequest));
        var canonicalRequestHashString = Convert.ToHexString(canonicalRequestHash).ToLower();
        return $"AWS4-HMAC-SHA256\n{date}T000000Z\n{date}/{region}/{service}/aws4_request\n{canonicalRequestHashString}";
    }

    private static string CalculateSignature(string stringToSign, string secretKey, string date, string region, string service)
    {
        var kSecret = Encoding.UTF8.GetBytes($"AWS4{secretKey}");
        var kDate = HmacSha256(kSecret, Encoding.UTF8.GetBytes(date));
        var kRegion = HmacSha256(kDate, Encoding.UTF8.GetBytes(region));
        var kService = HmacSha256(kRegion, Encoding.UTF8.GetBytes(service));
        var kSigning = HmacSha256(kService, "aws4_request"u8.ToArray());
        var signature = HmacSha256(kSigning, Encoding.UTF8.GetBytes(stringToSign));
        return Convert.ToHexString(signature).ToLower();
    }

    private static byte[] HmacSha256(byte[] key, byte[] data)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(data);
    }
}

/// <summary>
///     <para xml:lang="en">S3 Authentication Options</para>
///     <para xml:lang="zh">S3认证选项</para>
/// </summary>
public class S3AuthenticationOptions
{
    /// <summary>
    ///     <para xml:lang="en">Whether authentication is enabled</para>
    ///     <para xml:lang="zh">是否启用认证</para>
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    ///     <para xml:lang="en">Whether authentication is required</para>
    ///     <para xml:lang="zh">是否要求认证</para>
    /// </summary>
    public bool RequireAuthentication { get; set; } = false;

    /// <summary>
    ///     <para xml:lang="en">Access keys and secret keys mapping</para>
    ///     <para xml:lang="zh">访问密钥和秘密密钥映射</para>
    /// </summary>
    // ReSharper disable once CollectionNeverUpdated.Global
    public Dictionary<string, string> AccessKeys { get; set; } = new();
}

/// <summary>
///     <para xml:lang="en">Extension methods for S3 authentication middleware</para>
///     <para xml:lang="zh">S3认证中间件的扩展方法</para>
/// </summary>
// ReSharper disable once UnusedType.Global
public static class S3AuthenticationMiddlewareExtensions
{
    /// <summary>
    ///     <para xml:lang="en">Use S3 compatible authentication</para>
    ///     <para xml:lang="zh">使用S3兼容认证</para>
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public static IApplicationBuilder UseS3Authentication(this IApplicationBuilder builder) => builder.UseMiddleware<S3AuthenticationMiddleware>();

    /// <summary>
    ///     <para xml:lang="en">Use S3 compatible authentication with options</para>
    ///     <para xml:lang="zh">使用S3兼容认证（带选项）</para>
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public static IApplicationBuilder UseS3Authentication(this IApplicationBuilder builder, Action<S3AuthenticationOptions> configureOptions)
    {
        var options = new S3AuthenticationOptions();
        configureOptions(options);

        // Create a service collection to configure the options
        var services = new ServiceCollection();
        services.AddSingleton(options);
        var serviceProvider = services.BuildServiceProvider();

        // Use middleware with configured options
        return builder.UseMiddleware<S3AuthenticationMiddleware>(serviceProvider.GetRequiredService<S3AuthenticationOptions>());
    }
}