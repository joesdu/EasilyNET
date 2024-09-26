using EasilyNET.Core.Misc;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.ComponentModel;
using System.Reflection;

// ReSharper disable UnusedType.Global

namespace EasilyNET.WebCore.Swagger.SwaggerFilters;

/// <summary>
/// 添加默认值显示
/// </summary>
// ReSharper disable once UnusedMember.Global
// ReSharper disable once ClassNeverInstantiated.Global
public sealed class SwaggerDefaultValueFilter : ISchemaFilter
{
    /// <inheritdoc />
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
                if (info.Name.ToLowerCamelCase() != property.Key) continue;
                property.Value.Example = defaultAttribute.Value as IOpenApiAny;
                break;
            }
        }
    }
}