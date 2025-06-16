using System.Net;
using System.Text.Json.Serialization;
using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Modules;
using EasilyNET.WebCore.JsonConverters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WebApi.Test.Unit.Common;

// ReSharper disable UnusedType.Local
// ReSharper disable ClassNeverInstantiated.Global

namespace WebApi.Test.Unit.ServiceModules;

/// <summary>
/// 注册一些控制器的基本内容
/// </summary>
internal sealed class ControllersModule : AppModule
{
    /// <inheritdoc />
    public override async Task ConfigureServices(ConfigureServicesContext context)
    {
        context.Services.AddControllers()
               .AddJsonOptions(c =>
               {
                   c.JsonSerializerOptions.Converters.Add(new BoolJsonConverter());
                   c.JsonSerializerOptions.Converters.Add(new DateOnlyJsonConverter());
                   c.JsonSerializerOptions.Converters.Add(new DateTimeJsonConverter());
                   c.JsonSerializerOptions.Converters.Add(new DecimalJsonConverter());
                   c.JsonSerializerOptions.Converters.Add(new IntJsonConverter());
                   c.JsonSerializerOptions.Converters.Add(new TimeOnlyJsonConverter());
                   c.JsonSerializerOptions.Converters.Add(new BsonDocumentJsonConverter());
                   c.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
               });
        context.Services.AddEndpointsApiExplorer();
        await Task.CompletedTask;
    }
}

/// <summary>
/// Action过滤器,主要用于统一格式化返回数据结构.以及记录一些和请求相关的日志等,需要按需调整.
/// </summary>
/// <example>
///     <code>
/// <![CDATA[
///  builder.Services.AddControllers(x => x.Filters.Add<ActionExecuteFilter>());
///  ]]>
///  </code>
/// </example>
file sealed class ActionExecuteFilter : ActionFilterAttribute
{
    /// <summary>
    /// 当方法执行完成
    /// </summary>
    /// <param name="context"></param>
    public override void OnActionExecuted(ActionExecutedContext context)
    {
        switch (context.Result)
        {
            case ObjectResult { Value: not null } result when result.Value.GetType().IsSubclassOf(typeof(Stream)):
                break;
            case ObjectResult result:
                context.Result = new ObjectResult(new ResultObject { StatusCode = HttpStatusCode.OK, Msg = "success", Data = result.Value });
                break;
            case EmptyResult:
                context.Result = new ObjectResult(new ResultObject { StatusCode = HttpStatusCode.OK, Msg = "success", Data = null });
                break;
        }
        base.OnActionExecuted(context);
    }
}

/// <summary>
/// 异常过滤器
/// </summary>
/// <example>
///     <code>
/// <![CDATA[
///  builder.Services.AddControllers(x => x.Filters.Add<ExceptionFilter>());
///  ]]>
///  </code>
/// </example>
file sealed class ExceptionFilter(ILogger<ExceptionFilter> logger) : ExceptionFilterAttribute
{
    /// <summary>
    /// 当出现异常的时候.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public override Task OnExceptionAsync(ExceptionContext context)
    {
        if (logger.IsEnabled(LogLevel.Error))
        {
            logger.LogError("{Stacktrace}", context.Exception.ToString());
        }
        context.ExceptionHandled = true;
        context.Result = new ObjectResult(new ResultObject { StatusCode = HttpStatusCode.InternalServerError, Msg = context.Exception.Message, Data = null });
        return base.OnExceptionAsync(context);
    }
}