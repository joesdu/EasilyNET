using EasilyNET.Core.Essentials;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace EasilyNET.WebCore.Handlers;

/// <summary>
///     <inheritdoc cref="IExceptionHandler" />
/// </summary>
/// <param name="env"></param>
public sealed class BusinessExceptionHandler(IHostEnvironment env) : IExceptionHandler
{
    /// <inheritdoc cref="IExceptionHandler.TryHandleAsync" />
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not BusinessException business)
        {
            return false;
        }
        var details = new ProblemDetails
        {
            Status = (int?)business.Code,
            Title = business.Message
        };
        if (env.IsDevelopment())
        {
            details.Detail = $"""
                              {business.Source}
                              {business.StackTrace}
                              """;
        }
        httpContext.Response.StatusCode = details.Status.Value;
        // RFC 7807: ProblemDetails responses should use the application/problem+json media type.
        await httpContext.Response.WriteAsJsonAsync(details, options: null, contentType: "application/problem+json", cancellationToken);
        return true;
    }
}