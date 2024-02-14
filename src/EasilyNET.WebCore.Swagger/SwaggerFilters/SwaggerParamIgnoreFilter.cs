using EasilyNET.WebCore.Swagger.Attributes;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.WebCore.Swagger.SwaggerFilters;

/// <summary>
/// 忽略Swagger参数
/// </summary>
public sealed class SwaggerParamIgnoreFilter : IOperationFilter
{
    /// <summary>
    /// Apply
    /// </summary>
    /// <param name="operation"></param>
    /// <param name="context"></param>
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.ApiDescription?.ParameterDescriptions is null) return;
        var hideParam = context.ApiDescription.ParameterDescriptions.Where(ParameterHasIgnoreAttribute).ToList();
        if (hideParam.Count is 0) return;
        foreach (var parameter in hideParam.Select(hp => operation.Parameters.FirstOrDefault(param => string.Equals(param.Name, hp.Name, StringComparison.Ordinal))).Where(p => p is not null))
        {
            operation.Parameters.Remove(parameter);
        }
    }

    private static bool ParameterHasIgnoreAttribute(ApiParameterDescription apd)
    {
        return apd.ModelMetadata is DefaultModelMetadata { Attributes.ParameterAttributes: not null } md && md.Attributes.ParameterAttributes.Any(c => c is SwaggerIgnoreAttribute);
    }
}
