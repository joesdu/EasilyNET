using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
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
    private static readonly JsonSerializerOptions options = new() { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };

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
        return context.Response.WriteAsync(JsonSerializer.Serialize(new ResultObject { StatusCode = HttpStatusCode.InternalServerError, Msg = ex.Message, Data = default }, options));
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