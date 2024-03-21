using EasilyNET.Core.System;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace EasilyNET.WebCore.Handlers;

/// <summary>
///     <inheritdoc cref="IExceptionHandler" />
/// </summary>
/// <param name="environment"></param>
public sealed class BusinessExceptionHandler(IHostEnvironment environment) : IExceptionHandler
{
    /// <inheritdoc cref="IExceptionHandler.TryHandleAsync" />
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not BusinessException business) return false;
        var details = new ProblemDetails
        {
            Status = (int?)business.Code,
            Title = business.Message
        };
        if (environment.IsDevelopment())
        {
            details.Detail = $"""
                              {business.Source}
                              {business.StackTrace}
                              """;
        }
        httpContext.Response.StatusCode = details.Status.Value;
        await httpContext.Response.WriteAsJsonAsync(details, cancellationToken);
        return true;
    }
}