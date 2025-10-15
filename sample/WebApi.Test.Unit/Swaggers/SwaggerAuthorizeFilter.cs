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
        // OpenAPI 3.x ：
        // 必须传入 hostDocument 参数，这样 OpenApiSecuritySchemeReference 才能解析 Target
        // Target 不为 null 时，序列化才会正常工作
        var schemeReference = new OpenApiSecuritySchemeReference(JwtBearerDefaults.AuthenticationScheme,
            context.Document // 传入文档实例，让引用能够解析到实际的安全方案
        );
        var requirement = new OpenApiSecurityRequirement
        {
            { schemeReference, [] }
        };

        // 初始化 Security 集合（如果为 null）
        operation.Security ??= [];
        operation.Security.Add(requirement);

        // 添加401和403响应
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