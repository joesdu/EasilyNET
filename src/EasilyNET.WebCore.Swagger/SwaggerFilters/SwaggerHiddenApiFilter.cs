using System.Reflection;
using EasilyNET.Core.Attributes;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

// ReSharper disable UnusedType.Global

namespace EasilyNET.WebCore.Swagger.SwaggerFilters;

/// <summary>
///     <para xml:lang="en">Hide APIs in Swagger documentation</para>
///     <para xml:lang="zh">在Swagger文档中隐藏接口</para>
/// </summary>
// ReSharper disable once UnusedMember.Global
// ReSharper disable once ClassNeverInstantiated.Global
public sealed class SwaggerHiddenApiFilter : IDocumentAsyncFilter
{
    /// <inheritdoc />
    public async Task ApplyAsync(OpenApiDocument swaggerDoc, DocumentFilterContext context, CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            foreach (var apiDescription in context.ApiDescriptions)
            {
                if (!apiDescription.TryGetMethodInfo(out var method) || (!method.ReflectedType!.IsDefined(typeof(HiddenApiAttribute)) && !method.IsDefined(typeof(HiddenApiAttribute)))) continue;
                var key = $"/{apiDescription.RelativePath}";
                if (key.Contains('?'))
                {
                    var index = key.IndexOf('?', StringComparison.Ordinal);
                    key = key[..index];
                }
                _ = swaggerDoc.Paths.Remove(key);
            }
        }, cancellationToken);
    }
}