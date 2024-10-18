using System.Collections.Concurrent;
using System.Reflection;
using EasilyNET.Core.Misc;
using EasilyNET.WebCore.Swagger.Attributes;
using EasilyNET.WebCore.Swagger.SwaggerFilters;
using Microsoft.AspNetCore.Builder;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Swagger扩展
/// </summary>
public static class SwaggerGenOptionsExtensions
{
    private static readonly ConcurrentDictionary<string, string> docsDic = [];
    private static readonly ConcurrentDictionary<string, string> endPointDic = [];
    private static readonly IEnumerable<ApiGroupAttribute> _attributes;

    static SwaggerGenOptionsExtensions()
    {
        _attributes = AssemblyHelper.FindTypesByAttribute<ApiGroupAttribute>()
                                    .Select(ctrl => ctrl.GetCustomAttribute<ApiGroupAttribute>())
                                    .OfType<ApiGroupAttribute>()
                                    .Where(attr => !docsDic.ContainsKey(attr.Title));
    }

    /// <summary>
    /// 添加预定于的Swagger配置
    /// </summary>
    /// <param name="op"></param>
    /// <param name="defaultName">默认文档名称</param>
    public static void EasilySwaggerGenOptions(this SwaggerGenOptions op, string defaultName)
    {
        foreach (var attr in _attributes)
        {
            _ = docsDic.TryAdd(attr.Title, attr.Description);
            op.SwaggerDoc(attr.Title, new()
            {
                Title = attr.Title,
                Version = attr.Version,
                Description = attr.Description
            });
        }
        op.DocInclusionPredicate((docName, apiDescription) =>
        {
            //反射拿到值
            var actionList = apiDescription.ActionDescriptor.EndpointMetadata.Where(x => x is ApiGroupAttribute).ToList();
            if (actionList.Count is not 0)
            {
                return actionList.FirstOrDefault() is ApiGroupAttribute attr && attr.Title == docName;
            }
            var not = apiDescription.ActionDescriptor.EndpointMetadata.Where(x => x is not ApiGroupAttribute).ToList();
            return not.Count is not 0 && docName == defaultName;
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
    }

    /// <summary>
    /// 添加预定义的SwaggerUI配置
    /// </summary>
    /// <param name="op"></param>
    public static void EasilySwaggerUIOptions(this SwaggerUIOptions op)
    {
        foreach (var attr in _attributes)
        {
            _ = endPointDic.TryAdd(attr.Title, attr.Description);
            op.SwaggerEndpoint($"/swagger/{attr.Title}/swagger.json", $"{attr.Title}");
        }
    }
}