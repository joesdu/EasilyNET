using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

// ReSharper disable ClassNeverInstantiated.Global

namespace EasilyNET.WebCore.Middleware;

/// <summary>
/// </summary>
public class ErrorHandlingMiddleware
{
    private readonly ILogger<ErrorHandlingMiddleware> _logger;
    private readonly RequestDelegate next;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="next"></param>
    /// <param name="logger"></param>
    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        this.next = next;
        _logger = logger;
    }

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
        _logger.LogError("发生未处理异常: {ex}", ex.ToString());
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return context.Response.WriteAsync(JsonSerializer.Serialize(new ResultObject
        {
            StatusCode = HttpStatusCode.InternalServerError,
            Msg = ex.Message,
            Data = default
        }, options));
    }
}