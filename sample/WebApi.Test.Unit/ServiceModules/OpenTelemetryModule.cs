using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Modules;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using WebApi.Test.Unit.Common;

namespace WebApi.Test.Unit.ServiceModules;

/// <summary>
/// OpenTelemetry相关内容
/// </summary>
internal sealed class OpenTelemetryModule : AppModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ConfigureServicesContext context)
    {
        var otel = context.Services.GetConfiguration().GetSection("OpenTelemetry");
        var env = context.ServiceProvider?.GetRequiredService<IWebHostEnvironment>() ?? throw new("获取服务出错");
        var resourceBuilder = ResourceBuilder.CreateDefault().AddService(Constant.InstanceName);
        context.Services.AddOpenTelemetry()
               .WithMetrics(c =>
               {
                   c.SetResourceBuilder(resourceBuilder);
                   c.AddRuntimeInstrumentation();
                   c.AddProcessInstrumentation();
                   c.AddMeter("Microsoft.AspNetCore.Hosting", "Microsoft.AspNetCore.Server.Kestrel", "System.Net.Http", "WebApi.Test.Unit");
                   c.AddOtlpExporter();
               })
               .WithTracing(c =>
               {
                   c.SetResourceBuilder(resourceBuilder);
                   if (env.IsDevelopment())
                   {
                       c.SetSampler<AlwaysOnSampler>();
                   }
                   c.AddAspNetCoreInstrumentation();
                   c.AddHttpClientInstrumentation();
                   c.AddGrpcClientInstrumentation();
                   c.AddRedisInstrumentation();
                   c.AddRabbitMQInstrumentation();
                   c.AddMongoDBInstrumentation();
                   c.AddOtlpExporter();
               });
        context.Services.Configure<OtlpExporterOptions>(c =>
        {
            if (!string.IsNullOrWhiteSpace(otel["DASHBOARD_OTLP_PRIMARYAPIKEY"]))
            {
                c.Headers = $"x-otlp-api-key={otel["DASHBOARD_OTLP_PRIMARYAPIKEY"]}";
            }
        });
        context.Services.AddHealthChecks().AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);
        context.Services.ConfigureHttpClientDefaults(c => c.AddStandardResilienceHandler());
        context.Services.AddMetrics();
    }

    public override void ApplicationInitialization(ApplicationContext context)
    {
        var app = context.GetApplicationHost() as WebApplication;
        // 配置健康检查端点
        app?.MapHealthChecks("/health");
        app?.MapHealthChecks("/alive", new()
        {
            Predicate = r => r.Tags.Contains("live")
        });
    }
}

static file class OpenTelemetryExtensions
{
    public static void AddMongoDBInstrumentation(this TracerProviderBuilder builder) => builder.AddSource("EasilyNET.Mongo.ConsoleDebug");
}