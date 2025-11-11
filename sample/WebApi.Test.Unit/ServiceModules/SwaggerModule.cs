using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Reflection;
using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Modules;
using EasilyNET.Core.Misc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using WebApi.Test.Unit.Swaggers;

namespace WebApi.Test.Unit.ServiceModules;

/// <summary>
/// Swagger文档的配置
/// </summary>
internal sealed class SwaggerModule : AppModule
{
    private const string _defaultDescription = "Console.WriteLine(\"🐂🍺\")";
    private static readonly FrozenDictionary<string, OpenApiInfo> attributesDic;
    private static readonly string _docName = Assembly.GetEntryAssembly()?.GetName().Name ?? string.Empty;

    static SwaggerModule()
    {
        var dic = new ConcurrentDictionary<string, OpenApiInfo>();
        // 添加默认文档(未分组的控制器)
        dic.TryAdd(_docName, new()
        {
            Title = _docName,
            Description = _defaultDescription,
            //Version = "v1",
            //TermsOfService = new Uri("https://cn.bing.com"),
            //Contact = new OpenApiContact()
            //{
            //    Name = "EasilyNET",
            //    Email = "dygood@outlook.com"
            //}
            License = License
        });
        var attributes = AssemblyHelper.FindTypesByAttribute<ApiExplorerSettingsAttribute>()
                                       .Select(ctrl => ctrl.GetCustomAttribute<ApiExplorerSettingsAttribute>())
                                       .OfType<ApiExplorerSettingsAttribute>()
                                       .OrderBy(c => c.GroupName);
        attributes.ForEach(attribute =>
        {
            dic.TryAdd(attribute.GroupName ?? _docName, new()
            {
                Title = attribute.GroupName,
                Description = _defaultDescription,
                License = License
            });
        });
        attributesDic = dic.OrderBy(kvp => kvp.Key == _docName ? string.Empty : kvp.Key).ToFrozenDictionary();
    }

    private static OpenApiLicense License { get; } = new()
    {
        Name = "MIT License",
        Url = new("https://github.com/joesdu/EasilyNET/blob/main/LICENSE")
    };

    /// <inheritdoc />
    public override async Task ConfigureServices(ConfigureServicesContext context)
    {
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        context.Services.AddSwaggerGen(c =>
        {
            // 添加服务器配置以支持多种 Scheme (HTTP/HTTPS)
            //c.AddServer(new()
            //{
            //    Url = "https://localhost:5001",
            //    Description = "HTTPS Endpoint"
            //});
            //c.AddServer(new()
            //{
            //    Url = "http://localhost:5000",
            //    Description = "HTTP Endpoint"
            //});
            // 添加全局安全方案定义
            c.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                Scheme = "bearer",              // 小写，符合OpenAPI 3.x规范
                BearerFormat = "JWT",           // 指示令牌格式
                Type = SecuritySchemeType.Http, // 使用Http方案以支持Bearer
                In = ParameterLocation.Header
            });
            // 注意：不要在这里添加全局 AddSecurityRequirement
            // 让 OperationFilter 来处理每个操作的安全要求
            // 配置文档过滤规则
            c.DocInclusionPredicate((docName, apiDescription) =>
            {
                var metadata = apiDescription.ActionDescriptor.EndpointMetadata.OfType<ApiExplorerSettingsAttribute>().FirstOrDefault();
                // 如果控制器有GroupName，匹配对应文档
                if (metadata is not null && metadata.GroupName.IsNotNullOrWhiteSpace())
                {
                    return metadata.GroupName.Equals(docName, StringComparison.OrdinalIgnoreCase);
                }
                // 未指定GroupName的控制器归入默认文档
                return docName == _docName;
            });
            var files = Directory.GetFiles(AppContext.BaseDirectory, "*.xml", SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                try
                {
                    c.IncludeXmlComments(file, true);
                }
                catch (Exception)
                {
                    // ignore
                }
            }
            // 添加 OperationFilter 来处理授权
            c.OperationFilter<SwaggerAuthorizeFilter>();
            // 动态注册所有文档
            foreach (var (key, value) in attributesDic)
            {
                c.SwaggerDoc(key, value);
            }
        });
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public override async Task ApplicationInitialization(ApplicationContext context)
    {
        var app = context.GetApplicationHost() as IApplicationBuilder;
        app.UseSwagger(c => c.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1);
        app.UseSwaggerUI(c =>
        {
            foreach (var (key, value) in attributesDic)
            {
                c.SwaggerEndpoint($"/swagger/{key}/swagger.json", value.Title);
            }
            c.RoutePrefix = "swagger";
            // 设置 Swagger UI 支持深色模式
            c.InjectStylesheet("/swagger-ui/custom.css");
            // 注入主题切换脚本
            c.InjectJavascript("/swagger-ui/theme-toggle.js");
        });
        await base.ApplicationInitialization(context);
    }
}