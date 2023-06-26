using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System.Net;

// ReSharper disable UnusedType.Global

namespace EasilyNET.WebCore.Filters;

/// <summary>
/// 全局异常过滤器
/// </summary>
/// <example>
///     <code>
/// <![CDATA[
///  builder.Services.AddControllers(x => x.Filters.Add<ExceptionFilter>());
///  ]]>
///  </code>
/// </example>
// ReSharper disable once ClassNeverInstantiated.Global
public sealed class ExceptionFilter(ILogger<ExceptionFilter> logger) : ExceptionFilterAttribute
{
    private readonly ILogger<ExceptionFilter> _logger = logger;

    /// <summary>
    /// 当出现异常的时候.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public override Task OnExceptionAsync(ExceptionContext context)
    {
        _logger.LogError("{Stacktrace}", context.Exception.ToString());
        context.ExceptionHandled = true;
        context.Result = new ObjectResult(new ResultObject { StatusCode = HttpStatusCode.InternalServerError, Msg = context.Exception.Message, Data = default });
        return base.OnExceptionAsync(context);
    }
}