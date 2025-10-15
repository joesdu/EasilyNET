using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

// ReSharper disable UnusedType.Global

namespace WebApi.Test.Unit.Swaggers;

/// <summary>
///     <para xml:lang="en">Add 🔒 to interfaces that require Authorize in Swagger documentation</para>
///     <para xml:lang="zh">在Swagger文档中给需要Authorize的接口添加🔒</para>
/// </summary>
// ReSharper disable once UnusedMember.Global
// ReSharper disable once ClassNeverInstantiated.Global
public sealed class SwaggerAuthorizeFilter : IOperationFilter
{
    /// <inheritdoc />
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // 获取方法和类的特性
        var methodAttributes = context.MethodInfo.GetCustomAttributes(true);
        var classAttributes = context.MethodInfo.DeclaringType?.GetCustomAttributes(true) ?? [];
        var authAttributes = methodAttributes.Union(classAttributes).OfType<AuthorizeAttribute>();
        var allowAnonymousAttributes = methodAttributes.Union(classAttributes).OfType<AllowAnonymousAttribute>();

        // 如果存在AllowAnonymous特性，不添加安全要求
        if (allowAnonymousAttributes.Any())
        {
            return;
        }
        // 如果存在Authorize特性，添加安全要求
        if (!authAttributes.Any())
        {
            return;
        }
        operation.Security = new List<OpenApiSecurityRequirement>
        {
            new()
            {
                {
                    new("test")
                    {
                        Reference = new()
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = JwtBearerDefaults.AuthenticationScheme // "Bearer"
                        }
                    },
                    [] // 空字符串表示无特定作用域要求
                }
            }
        };
        // 添加401和403响应
        // 确保Responses不为空并添加401和403
        operation.Responses ??= new();
        if (!operation.Responses.ContainsKey("401"))
        {
            operation.Responses.Add("401", new OpenApiResponse { Description = "Unauthorized" });
        }
        if (!operation.Responses.ContainsKey("403"))
        {
            operation.Responses.Add("403", new OpenApiResponse { Description = "Forbidden" });
        }
    }
}