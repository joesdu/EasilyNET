using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net;

// ReSharper disable UnusedType.Global

namespace EasilyNET.WebCore.Filters;

/// <summary>
/// Action过滤器,主要用于统一格式化返回数据结构.
/// </summary>
public sealed class ActionExecuteFilter : ActionFilterAttribute
{
    public override void OnActionExecuted(ActionExecutedContext context)
    {
        switch (context.Result)
        {
            case ObjectResult { Value: not null } result when result.Value.GetType().IsSubclassOf(typeof(Stream)):
                break;
            case ObjectResult result:
                context.Result = new ObjectResult(new { StatusCode = HttpStatusCode.OK, Msg = "success", Data = result.Value });
                break;
            case EmptyResult:
                context.Result = new ObjectResult(new { StatusCode = HttpStatusCode.OK, Msg = "success", Data = default(object) });
                break;
        }
        base.OnActionExecuted(context);
    }
}