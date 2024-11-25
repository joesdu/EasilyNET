using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using WebApi.Test.Unit.Common;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable SuggestBaseTypeForParameterInConstructor
// ReSharper disable ClassNeverInstantiated.Global

namespace WebApi.Test.Unit.Middleware;

/// <summary>
/// 全局异常中间件(使用自定义返回格式)
/// </summary>
/// <param name="next"></param>
/// <param name="logger"></param>
internal sealed class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
{
    private static readonly JsonSerializerOptions options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        IncludeFields = false,                                               // 不包含字段，只序列化属性
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,                    // 字典键命名策略为驼峰命名
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),                // 使用默认的类型信息解析器
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,        // 忽略值为null的属性
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals, // 允许命名的浮点数文字
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,               // 使用不安全的放松JSON转义
        Converters = { new JsonStringEnumConverter() }                       // 添加枚举转换器，枚举值序列化为原始名称字符串
    };

    /// <summary>
    /// Invoke
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task Invoke(HttpContext context /* other dependencies */)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        logger.LogError("发生未处理异常: {Ex}", ex.ToString());
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        options.Converters.Add(new JsonStringEnumConverter());
        return context.Response.WriteAsync(JsonSerializer.Serialize(new ResultObject
        {
            StatusCode = HttpStatusCode.InternalServerError,
            Msg = ex.Message,
            Data = default
        }, options));
    }
}

internal static class ErrorHandleExtension
{
    /// <summary>
    /// 使用全局异常中间件(使用自定义返回格式)
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    internal static IApplicationBuilder UseErrorHandling(this IApplicationBuilder builder) => builder.UseMiddleware<ErrorHandlingMiddleware>();
}