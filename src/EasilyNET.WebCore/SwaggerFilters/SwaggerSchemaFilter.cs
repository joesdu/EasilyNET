using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.ComponentModel;
using System.Reflection;

// ReSharper disable UnusedType.Global

namespace EasilyNET.WebCore.SwaggerFilters;

/// <summary>
/// 添加默认值显示
/// </summary>
// ReSharper disable once UnusedMember.Global
// ReSharper disable once ClassNeverInstantiated.Global
public sealed class SwaggerSchemaFilter : ISchemaFilter
{
    /// <summary>
    /// Apply
    /// </summary>
    /// <param name="schema"></param>
    /// <param name="context"></param>
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema.Properties is null) return;
        foreach (var info in context.Type.GetProperties())
        {
            // Look for class attributes that have been decorated with "[DefaultAttribute(...)]".
            var defaultAttribute = info.GetCustomAttribute<DefaultValueAttribute>();
            if (defaultAttribute is null) continue;
            foreach (var property in schema.Properties)
            {
                // Only assign default value to the proper element.
                if (ToCamelCase(info.Name) != property.Key) continue;
                property.Value.Example = defaultAttribute.Value as IOpenApiAny;
                break;
            }
        }
    }

    /// <summary>
    /// 转成驼峰形式
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    private static string ToCamelCase(string name) => char.ToLowerInvariant(name[0]) + name[1..];
}