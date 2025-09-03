using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using EasilyNET.Mongo.GridFS.S3.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EasilyNET.Mongo.GridFS.S3.Middleware;

/// <summary>
///     <para xml:lang="en">S3 Compatible Authentication Middleware</para>
///     <para xml:lang="zh">S3兼容认证中间件</para>
/// </summary>
/// <remarks>
///     <para xml:lang="en">Constructor</para>
///     <para xml:lang="zh">构造函数</para>
/// </remarks>
public partial class S3AuthenticationMiddleware(RequestDelegate next, ILogger<S3AuthenticationMiddleware> logger, IOptions<S3AuthenticationOptions> options, MongoS3IamPolicyManager iamManager)
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
        if (!authorization.StartsWith("AWS4-HMAC-SHA256", StringComparison.Ordinal))
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Invalid authentication method");
            return;
        }
        try
        {
            if (await ValidateSignatureAsync(context, authorization))
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

    private async Task<bool> ValidateSignatureAsync(HttpContext context, string authorization)
    {
        // Parse authorization header
        // Format: AWS4-HMAC-SHA256 Credential=AKID/DATE/REGION/SERVICE/aws4_request, SignedHeaders=host;range;x-amz-date, Signature=...
        var authParts = authorization.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (authParts.Length < 2 || !string.Equals(authParts[0], "AWS4-HMAC-SHA256", StringComparison.Ordinal))
        {
            return false;
        }
        var credential = "";
        var signedHeaders = "";
        var signature = "";
        foreach (var rawPart in authParts[1].Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var idx = rawPart.IndexOf('=');
            if (idx <= 0)
            {
                continue;
            }
            var key = rawPart[..idx].Trim();
            var value = rawPart[(idx + 1)..].Trim();
            switch (key)
            {
                case "Credential":
                    credential = value;
                    break;
                case "SignedHeaders":
                    signedHeaders = value;
                    break;
                case "Signature":
                    signature = value;
                    break;
            }
        }
        if (string.IsNullOrEmpty(credential) || string.IsNullOrEmpty(signedHeaders) || string.IsNullOrEmpty(signature))
        {
            return false;
        }

        // Parse credential: AKID/yyyymmdd/region/service/aws4_request
        var credParts = credential.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (credParts.Length != 5)
        {
            return false;
        }
        var accessKey = credParts[0];
        var dateScope = credParts[1];
        var region = credParts[2];
        var service = credParts[3];

        // Get x-amz-date (full timestamp) from headers
        var amzDate = context.Request.Headers["x-amz-date"].ToString();
        if (string.IsNullOrEmpty(amzDate))
        {
            // Some clients may send 'X-Amz-Date'
            amzDate = context.Request.Headers["X-Amz-Date"].ToString();
        }
        if (string.IsNullOrEmpty(amzDate) || amzDate.Length < 16)
        {
            return false;
        }

        // Get secret key for access key
        var user = await iamManager.GetUserByAccessKeyAsync(accessKey);
        if (user == null || string.IsNullOrEmpty(user.SecretKey))
        {
            return false;
        }
        var secretKey = user.SecretKey;

        // Update last used time
        await iamManager.UpdateAccessKeyLastUsedAsync(accessKey);

        // Reconstruct canonical request
        var canonicalRequest = await BuildCanonicalRequestAsync(context, signedHeaders);

        // Build string to sign
        var stringToSign = BuildStringToSign(canonicalRequest, amzDate, dateScope, region, service);

        // Calculate signature
        var calculatedSignature = CalculateSignature(stringToSign, secretKey, dateScope, region, service);
        return string.Equals(calculatedSignature, signature, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<string> BuildCanonicalRequestAsync(HttpContext context, string signedHeaders)
    {
        var method = context.Request.Method.ToUpperInvariant();
        var canonicalUri = BuildCanonicalUri(context.Request.Path);
        var canonicalQueryString = BuildCanonicalQueryString(context.Request.Query);
        var canonicalHeaders = await BuildCanonicalHeadersAsync(context, signedHeaders);
        var payloadHash = await CalculatePayloadHashAsync(context);
        return $"{method}\n{canonicalUri}\n{canonicalQueryString}\n{canonicalHeaders}\n{signedHeaders.ToLowerInvariant()}\n{payloadHash}";
    }

    private static string BuildCanonicalUri(PathString path)
    {
        var p = path.HasValue ? path.Value! : "/";
        if (string.IsNullOrEmpty(p))
        {
            p = "/";
        }
        // Normalize each segment per RFC 3986
        var segments = p.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var encoded = segments.Select(s => Uri.EscapeDataString(s).Replace("%7E", "~"));
        return "/" + string.Join('/', encoded);
    }

    private static string BuildCanonicalQueryString(IQueryCollection query)
    {
        var items = new List<(string Key, string Value)>();
        foreach (var kvp in query)
        {
            var key = Uri.EscapeDataString(kvp.Key).Replace("%7E", "~");
            items.AddRange(kvp.Value
                              .OrderBy(v => v, StringComparer.Ordinal)
                              .Select(v => Uri.EscapeDataString(v ?? string.Empty)
                                              .Replace("%7E", "~")).Select(val => (key, val)));
        }
        return string.Join("&", items.OrderBy(i => i.Key, StringComparer.Ordinal).ThenBy(i => i.Value, StringComparer.Ordinal).Select(i => $"{i.Key}={i.Value}"));
    }

    private static async Task<string> BuildCanonicalHeadersAsync(HttpContext context, string signedHeaders)
    {
        var headerNames = signedHeaders.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(h => h.Trim().ToLowerInvariant()).OrderBy(h => h, StringComparer.Ordinal).ToList();
        var headers = new StringBuilder();

        // Ensure host header availability
        var hostValue = context.Request.Headers.Host.ToString();
        if (string.IsNullOrEmpty(hostValue))
        {
            hostValue = context.Request.Host.Value;
        }
        var headerLookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrEmpty(hostValue))
        {
            headerLookup["host"] = hostValue;
        }
        foreach (var h in context.Request.Headers)
        {
            headerLookup[h.Key.ToLowerInvariant()] = string.Join(",", h.Value.Where(v => v != null));
        }
        foreach (var name in headerNames)
        {
            if (!headerLookup.TryGetValue(name, out var rawValue))
            {
                // If a signed header is missing, canonical request will not match
                continue;
            }
            var value = NormalizeHeaderValue(rawValue);
            headers.Append($"{name}:{value}\n");
        }

        // Some frameworks need body buffering to compute hash; ensure it's enabled when requested later
        if (!context.Request.Body.CanSeek)
        {
            context.Request.EnableBuffering();
        }
        context.Request.Body.Position = 0;
        await Task.Yield();
        return headers.ToString();
    }

    private static string NormalizeHeaderValue(string value)
    {
        // Trim and collapse sequential spaces per AWS spec
        var trimmed = value.Trim();
        return WhitespaceRegex().Replace(trimmed, " ");
    }

    private static async Task<string> CalculatePayloadHashAsync(HttpContext context)
    {
        var headerValue = context.Request.Headers["x-amz-content-sha256"].ToString();
        if (!string.IsNullOrEmpty(headerValue))
        {
            // Support UNSIGNED-PAYLOAD and streaming payloads
            if (string.Equals(headerValue, "UNSIGNED-PAYLOAD", StringComparison.Ordinal) || headerValue.StartsWith("STREAMING-AWS4-HMAC-SHA256", StringComparison.Ordinal))
            {
                return headerValue;
            }
            return headerValue.ToLowerInvariant();
        }

        // Fallback: hash the actual body (enable buffering to avoid consuming the stream)
        if (!context.Request.Body.CanSeek)
        {
            context.Request.EnableBuffering();
        }
        context.Request.Body.Position = 0;
        using var sha = SHA256.Create();
        var hash = await sha.ComputeHashAsync(context.Request.Body);
        context.Request.Body.Position = 0;
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string BuildStringToSign(string canonicalRequest, string amzDate, string dateScope, string region, string service)
    {
        var canonicalRequestHash = SHA256.HashData(Encoding.UTF8.GetBytes(canonicalRequest));
        var canonicalRequestHashString = Convert.ToHexString(canonicalRequestHash).ToLowerInvariant();
        return $"AWS4-HMAC-SHA256\n{amzDate}\n{dateScope}/{region}/{service}/aws4_request\n{canonicalRequestHashString}";
    }

    private static string CalculateSignature(string stringToSign, string secretKey, string date, string region, string service)
    {
        var kSecret = Encoding.UTF8.GetBytes($"AWS4{secretKey}");
        var kDate = HmacSha256(kSecret, Encoding.UTF8.GetBytes(date));
        var kRegion = HmacSha256(kDate, Encoding.UTF8.GetBytes(region));
        var kService = HmacSha256(kRegion, Encoding.UTF8.GetBytes(service));
        var kSigning = HmacSha256(kService, "aws4_request"u8.ToArray());
        var signature = HmacSha256(kSigning, Encoding.UTF8.GetBytes(stringToSign));
        return Convert.ToHexString(signature).ToLowerInvariant();
    }

    private static byte[] HmacSha256(byte[] key, byte[] data)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(data);
    }

    [GeneratedRegex("\\s+")]
    private static partial Regex WhitespaceRegex();
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
        var opts = new S3AuthenticationOptions();
        configureOptions(opts);
        // Get IAM manager from services
        var iamManager = builder.ApplicationServices.GetRequiredService<MongoS3IamPolicyManager>();
        return builder.UseMiddleware<S3AuthenticationMiddleware>(Options.Create(opts), iamManager);
    }
}