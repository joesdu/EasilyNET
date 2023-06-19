using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net;

// ReSharper disable UnusedType.Global

namespace EasilyNET.WebCore.Filters;

/// <summary>
/// Action过滤器,主要用于统一格式化返回数据结构.
/// </summary>
/// <example>
///     <code>
/// <![CDATA[
///  builder.Services.AddControllers(x => x.Filters.Add<ActionExecuteFilter>());
///  ]]>
///  </code>
/// </example>
// ReSharper disable once ClassNeverInstantiated.Global
public sealed class ActionExecuteFilter : ActionFilterAttribute
{
    /// <summary>
    /// 当方法执行完成
    /// </summary>
    /// <param name="context"></param>
    public override void OnActionExecuted(ActionExecutedContext context)
    {
        switch (context.Result)
        {
            case ObjectResult {Value: not null} result when result.Value.GetType().IsSubclassOf(typeof(Stream)): break;
            case ObjectResult result:
                context.Result = new ObjectResult(new ResultObject {StatusCode = HttpStatusCode.OK, Msg = "success", Data = result.Value});
                break;
            case EmptyResult:
                context.Result = new ObjectResult(new ResultObject {StatusCode = HttpStatusCode.OK, Msg = "success", Data = default});
                break;
        }
        base.OnActionExecuted(context);
    }
}