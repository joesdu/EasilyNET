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
    private const string _defaultDescription = "Console.WriteLine(\"🐂🍺\")";
    private static readonly FrozenDictionary<string, OpenApiInfo> attributesDic;
    private static readonly string? _docName = Assembly.GetEntryAssembly()?.GetName().Name ?? string.Empty;

    static SwaggerGenOptionsExtensions()
    {
        var dic = new ConcurrentDictionary<string, OpenApiInfo>();
        var _description = new ConcurrentDictionary<string, HashSet<string>>();
        _description.TryAdd(_docName, [_defaultDescription]);
        var attributes = AssemblyHelper.FindTypesByAttribute<ApiGroupAttribute>()
                                       .Select(ctrl => ctrl.GetCustomAttribute<ApiGroupAttribute>())
                                       .OfType<ApiGroupAttribute>()
                                       .OrderBy(c => c.Title)
                                       .GroupBy(attr => attr.Title);
        Parallel.ForEach(attributes, group =>
        {
            var title = group.Key;
            if (!_description.ContainsKey(title))
            {
                _description.TryAdd(title, []);
            }
            //var des = group.Where(s => !string.IsNullOrWhiteSpace(s.Des)).Select(c => c.Des);
            //_description[title].AddRange(des);
            foreach (var item in group)
            {
                if (!string.IsNullOrWhiteSpace(item.Des))
                {
                    _description[title].Add(item.Des);
                }
            }
        });
        Parallel.ForEach(_description, item =>
        {
            dic.TryAdd(item.Key, new()
            {
                Title = item.Key,
                Description = item.Value.Join()
            });
        });
        attributesDic = GetSortedAttributesDic(dic);
    }

    /// <summary>
    /// 添加预定于的Swagger配置
    /// </summary>
    /// <param name="op"></param>
    public static void EasilySwaggerGenOptions(this SwaggerGenOptions op)
    {
        op.DocInclusionPredicate((doc_name, apiDescription) =>
        {
            //反射拿到值
            var actionList = apiDescription.ActionDescriptor.EndpointMetadata.Where(x => x is ApiGroupAttribute).ToList();
            if (actionList.Count is not 0)
            {
                return actionList.FirstOrDefault() is ApiGroupAttribute attr && attr.Title == doc_name;
            }
            var not = apiDescription.ActionDescriptor.EndpointMetadata.Where(x => x is not ApiGroupAttribute).ToList();
            return not.Count is not 0 && doc_name == _docName;
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
        return dic.OrderBy(kvp => kvp.Key == _docName ? "" : kvp.Key).ToFrozenDictionary();
    }
}