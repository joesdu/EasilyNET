using EasilyNET.Core.Misc;
using EasilyNET.WebCore.Swagger.Attributes;
using EasilyNET.WebCore.Swagger.SwaggerFilters;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;
using System.Reflection;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.WebCore.Swagger;

/// <summary>
/// Swagger扩展
/// </summary>
public static class SwaggerExtension
{
    private static readonly Dictionary<string, string> docsDic = new();
    private static readonly Dictionary<string, string> endPointDic = new();

    /// <summary>
    /// 添加预定于的Swagger配置
    /// </summary>
    /// <param name="op"></param>
    /// <param name="defaultDocName">默认文档名称</param>
    public static void EasilySwaggerGenOptions(this SwaggerGenOptions op, string defaultDocName)
    {
        var controllers = AssemblyHelper.FindTypesByAttribute<ApiGroupAttribute>();
        foreach (var ctrl in controllers)
        {
            var attr = ctrl.GetCustomAttribute<ApiGroupAttribute>();
            if (attr is null) continue;
            if (docsDic.ContainsKey(attr.Name)) continue;
            _ = docsDic.TryAdd(attr.Name, attr.Description);
            op.SwaggerDoc(attr.Name, new()
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
            if (actionList.Count != 0)
            {
                return actionList.FirstOrDefault() is ApiGroupAttribute attr && attr.Name == docName;
            }
            var not = apiDescription.ActionDescriptor.EndpointMetadata.Where(x => x is not ApiGroupAttribute).ToList();
            return not.Count != 0 && docName == defaultDocName;
            //判断是否包含这个分组
        });
        var files = Directory.GetFiles(AppContext.BaseDirectory, "*.xml");
        foreach (var file in files)
        {
            op.IncludeXmlComments(file, true);
        }
        op.DocumentFilter<SwaggerHiddenApiFilter>();
        op.OperationFilter<SwaggerAuthorizeFilter>();
        op.OperationFilter<SwaggerParamIgnoreFilter>();
        op.SchemaFilter<SwaggerSchemaFilter>();
        op.SchemaFilter<SwaggerPropertyIgnoreFilter>();
    }

    /// <summary>
    /// 添加预定义的SwaggerUI配置
    /// </summary>
    /// <param name="op"></param>
    public static void EasilySwaggerUIOptions(this SwaggerUIOptions op)
    {
        var controllers = AssemblyHelper.FindTypesByAttribute<ApiGroupAttribute>();
        foreach (var ctrl in controllers)
        {
            var attr = ctrl.GetCustomAttribute<ApiGroupAttribute>();
            if (attr is null) continue;
            if (endPointDic.ContainsKey(attr.Name)) continue;
            _ = endPointDic.TryAdd(attr.Name, attr.Description);
            op.SwaggerEndpoint($"/swagger/{attr.Name}/swagger.json", $"{attr.Title} {attr.Version}");
        }
    }
}