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
    private static readonly Dictionary<string, string> docsDic = [];
    private static readonly Dictionary<string, string> endPointDic = [];

    /// <summary>
    /// 添加预定于的Swagger配置
    /// </summary>
    /// <param name="op"></param>
    /// <param name="defaultDocName">默认文档名称</param>
    public static void EasilySwaggerGenOptions(this SwaggerGenOptions op, string defaultDocName)
    {
        var controllers = AssemblyHelper.FindTypesByAttribute<ApiGroupAttribute>();
        foreach (var attr in controllers.Select(ctrl => ctrl.GetCustomAttribute<ApiGroupAttribute>()).OfType<ApiGroupAttribute>().Where(attr => !docsDic.ContainsKey(attr.Name)))
        {
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
        op.SchemaFilter<SwaggerDefaultValueFilter>();
    }

    /// <summary>
    /// 添加预定义的SwaggerUI配置
    /// </summary>
    /// <param name="op"></param>
    public static void EasilySwaggerUIOptions(this SwaggerUIOptions op)
    {
        var controllers = AssemblyHelper.FindTypesByAttribute<ApiGroupAttribute>();
        foreach (var attr in controllers.Select(ctrl => ctrl.GetCustomAttribute<ApiGroupAttribute>()).OfType<ApiGroupAttribute>().Where(attr => !endPointDic.ContainsKey(attr.Name)))
        {
            _ = endPointDic.TryAdd(attr.Name, attr.Description);
            op.SwaggerEndpoint($"/swagger/{attr.Name}/swagger.json", $"{attr.Title} {attr.Version}");
        }
    }
}