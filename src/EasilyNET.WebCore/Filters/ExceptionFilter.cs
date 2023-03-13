using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System.Net;

// ReSharper disable UnusedType.Global

namespace EasilyNET.WebCore.Filters;

/// <summary>
/// 全局异常过滤器
/// </summary>
public sealed class ExceptionFilter : ExceptionFilterAttribute
{
    private readonly ILogger<ExceptionFilter> _logger;

    public ExceptionFilter(ILogger<ExceptionFilter> logger)
    {
        _logger = logger;
    }

    public override Task OnExceptionAsync(ExceptionContext context)
    {
        _logger.LogError("{stacktrace}", context.Exception.ToString());
        context.ExceptionHandled = true;
        context.Result = new ObjectResult(new
        {
            StatusCode = HttpStatusCode.InternalServerError,
            Msg = context.Exception.Message,
            Data = default(object)
        });
        return base.OnExceptionAsync(context);
    }
}