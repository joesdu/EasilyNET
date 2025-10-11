using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

// ReSharper disable UnusedType.Global

namespace WebApi.Test.Unit.Swaggers;

/// <summary>
///     <para xml:lang="en">Add 🔒 to interfaces that require Authorize in Swagger documentation</para>
///     <para xml:lang="zh">在Swagger文档中给需要Authorize的接口添加🔒</para>
/// </summary>
// ReSharper disable once UnusedMember.Global
// ReSharper disable once ClassNeverInstantiated.Global
public sealed class SwaggerAuthorizeFilter : IOperationAsyncFilter
{
    /// <inheritdoc />
    public async Task ApplyAsync(OpenApiOperation operation, OperationFilterContext context, CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            var methodAttributes = context.MethodInfo.GetCustomAttributes(true);
            var classAttributes = context.MethodInfo.DeclaringType?.GetCustomAttributes(true) ?? [];
            var authAttributes = methodAttributes.Union(classAttributes).OfType<AuthorizeAttribute>();
            var allowAnonymousAttributes = methodAttributes.Union(classAttributes).OfType<AllowAnonymousAttribute>();
            if (allowAnonymousAttributes.Any())
            {
                return; // 如果存在AllowAnonymous特性，则不添加锁图标
            }
            if (!authAttributes.Any())
            {
                return; // 如果不存在Authorize特性，也不添加锁图标
            }
            operation.Security =
            [
                new()
                {
                    {
                        new()
                        {
                            Reference = new()
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            },
                            Scheme = "oauth2",
                            Name = "Bearer",
                            In = ParameterLocation.Header
                        },
                        Array.Empty<string>()
                    }
                }
            ];
            operation.Responses.Add("401", new() { Description = "Unauthorized" });
        }, cancellationToken);
    }
}