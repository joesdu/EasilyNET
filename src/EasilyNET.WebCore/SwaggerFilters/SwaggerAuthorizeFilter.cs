using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

// ReSharper disable UnusedType.Global

namespace EasilyNET.WebCore.SwaggerFilters;

/// <summary>
/// 在Swagger文档中给需要Authorize的接口添加🔒
/// </summary>
// ReSharper disable once UnusedMember.Global
public sealed class SwaggerAuthorizeFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var authAttributes = context.MethodInfo.DeclaringType?.GetCustomAttributes(true)
                                    .Union(context.MethodInfo.GetCustomAttributes(true))
                                    .OfType<AuthorizeAttribute>();
        if (!authAttributes!.Any()) return;
        operation.Security = new List<OpenApiSecurityRequirement>
        {
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
                    new List<string>()
                }
            }
        };
        operation.Responses.Add("401", new() { Description = "Unauthorized" });
    }
}