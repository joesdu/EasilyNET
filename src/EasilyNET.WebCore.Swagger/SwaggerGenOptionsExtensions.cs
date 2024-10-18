using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Reflection;
using EasilyNET.Core.Misc;
using EasilyNET.WebCore.Swagger.Attributes;
using EasilyNET.WebCore.Swagger.SwaggerFilters;
using Microsoft.AspNetCore.Builder;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Swagger扩展
/// </summary>
public static class SwaggerGenOptionsExtensions
{
    private static readonly FrozenDictionary<string, OpenApiInfo> attributesDic;
    private static readonly string _defaultName;

    static SwaggerGenOptionsExtensions()
    {
        var attributes = AssemblyHelper.FindTypesByAttribute<ApiGroupAttribute>()
                                       .Select(ctrl => ctrl.GetCustomAttribute<ApiGroupAttribute>())
                                       .OfType<ApiGroupAttribute>();
        var dic = new ConcurrentDictionary<string, OpenApiInfo>();
        foreach (var item in attributes)
        {
            var exist = dic.ContainsKey(item.Title);
            if (exist) continue;
            dic.TryAdd(item.Title, new()
            {
                Title = item.Title,
                Description = item.Des
            });
        }
        _defaultName = Assembly.GetEntryAssembly()?.GetName().Name ?? string.Empty;
        dic.TryAdd(_defaultName, new(new()
        {
            Title = _defaultName,
            Description = "Console.WriteLine(\"🐂🍺\")"
        }));
        attributesDic = GetSortedAttributesDic(dic);
    }

    /// <summary>
    /// 添加预定于的Swagger配置
    /// </summary>
    /// <param name="op"></param>
    public static void EasilySwaggerGenOptions(this SwaggerGenOptions op)
    {
        op.DocInclusionPredicate((docName, apiDescription) =>
        {
            //反射拿到值
            var actionList = apiDescription.ActionDescriptor.EndpointMetadata.Where(x => x is ApiGroupAttribute).ToList();
            if (actionList.Count is not 0)
            {
                return actionList.FirstOrDefault() is ApiGroupAttribute attr && attr.Title == docName;
            }
            var not = apiDescription.ActionDescriptor.EndpointMetadata.Where(x => x is not ApiGroupAttribute).ToList();
            return not.Count is not 0 && docName == _defaultName;
            //判断是否包含这个分组
        });
        var files = Directory.GetFiles(AppContext.BaseDirectory, "*.xml");
        foreach (var file in files)
        {
            op.IncludeXmlComments(file, true);
        }
        op.DocumentAsyncFilter<SwaggerHiddenApiFilter>();
        op.OperationAsyncFilter<SwaggerAuthorizeFilter>();
        op.SchemaFilter<SwaggerDefaultValueFilter>();
        foreach (var attr in attributesDic)
        {
            op.SwaggerDoc(attr.Key, attr.Value);
        }
    }

    /// <summary>
    /// SwaggerUI配置
    /// </summary>
    /// <param name="app"></param>
    public static void UseEasilySwaggerUI(this IApplicationBuilder app)
    {
        app.UseSwagger().UseSwaggerUI(c =>
        {
            foreach (var item in attributesDic)
            {
                c.SwaggerEndpoint($"/swagger/{item.Key}/swagger.json", item.Key);
            }
        });
    }

    private static FrozenDictionary<string, OpenApiInfo> GetSortedAttributesDic(IEnumerable<KeyValuePair<string, OpenApiInfo>> dic)
    {
        return dic.OrderBy(kvp => kvp.Key == _defaultName ? "" : kvp.Key).ToFrozenDictionary();
    }
}