using System.ComponentModel;
using System.Reflection;
using EasilyNET.Core.Misc;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

// ReSharper disable UnusedType.Global

namespace EasilyNET.WebCore.Swagger.SwaggerFilters;

/// <summary>
///     <para xml:lang="en">Add default value display</para>
///     <para xml:lang="zh">添加默认值显示</para>
/// </summary>
// ReSharper disable once UnusedMember.Global
// ReSharper disable once ClassNeverInstantiated.Global
public sealed class SwaggerDefaultValueFilter : ISchemaFilter
{
    /// <inheritdoc />
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema.Properties is null)
        {
            return;
        }
        foreach (var info in context.Type.GetProperties())
        {
            // Look for class attributes that have been decorated with "[DefaultAttribute(...)]".
            // 查找已用 "[DefaultAttribute(...)]" 装饰的类属性。
            var defaultAttribute = info.GetCustomAttribute<DefaultValueAttribute>();
            if (defaultAttribute is null)
            {
                continue;
            }
            foreach (var property in schema.Properties)
            {
                // Only assign default value to the proper element.
                // 仅将默认值分配给适当的元素。
                if (info.Name.ToLowerCamelCase() != property.Key)
                {
                    continue;
                }
                property.Value.Example = defaultAttribute.Value as IOpenApiAny;
                break;
            }
        }
    }
}