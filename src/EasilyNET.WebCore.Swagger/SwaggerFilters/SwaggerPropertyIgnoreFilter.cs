using EasilyNET.WebCore.Swagger.Attributes;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

// ReSharper disable ClassNeverInstantiated.Global

namespace EasilyNET.WebCore.Swagger.SwaggerFilters;

/// <summary>
/// 忽略Swagger Property参数
/// </summary>
public sealed class SwaggerPropertyIgnoreFilter : ISchemaFilter
{
    /// <summary>
    /// Apply
    /// </summary>
    /// <param name="schema"></param>
    /// <param name="context"></param>
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema.Properties is null) return;
        var ignoreDataMemberProperties = context.Type.GetProperties().Where(t => t.GetCustomAttribute<SwaggerIgnoreAttribute>() is not null);
        foreach (var ignoreDataMemberProperty in ignoreDataMemberProperties)
        {
            var propertyToHide = schema.Properties.Keys.SingleOrDefault(x => string.Equals(x, ignoreDataMemberProperty.Name, StringComparison.CurrentCultureIgnoreCase));
            if (propertyToHide is not null)
            {
                schema.Properties.Remove(propertyToHide);
            }
        }
    }
}