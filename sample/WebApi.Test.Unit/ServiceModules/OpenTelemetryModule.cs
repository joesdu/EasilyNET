using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Modules;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace WebApi.Test.Unit;

/// <summary>
/// OpenTelemetry相关内容
/// </summary>
internal sealed class OpenTelemetryModule : AppModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ConfigureServicesContext context)
    {
        var config = context.Services.GetConfiguration();
        var env = context.Services.GetWebHostEnvironment();
        context.Services.AddOpenTelemetry()
               .WithMetrics(c =>
               {
                   c.AddRuntimeInstrumentation();
                   c.AddMeter("Microsoft.AspNetCore.Hosting", "Microsoft.AspNetCore.Server.Kestrel", "System.Net.Http", "WebApi.Test.Unit");
                   c.AddOtlpExporter();
               })
               .WithTracing(c =>
               {
                   if (env.IsDevelopment())
                   {
                       c.SetSampler<AlwaysOnSampler>();
                   }
                   c.AddAspNetCoreInstrumentation();
                   c.AddHttpClientInstrumentation();
                   c.AddGrpcClientInstrumentation();
                   c.AddOtlpExporter();
               });
        context.Services.Configure<OtlpExporterOptions>(c =>
        {
            if (!string.IsNullOrWhiteSpace(config["DASHBOARD_OTLP_PRIMARYAPIKEY"]))
            {
                c.Headers = $"x-otlp-api-key={config["DASHBOARD_OTLP_PRIMARYAPIKEY"]}";
            }
        });
        context.Services.AddHealthChecks().AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);
        context.Services.ConfigureHttpClientDefaults(c => c.AddStandardResilienceHandler());
        context.Services.AddMetrics();
    }

    public override void ApplicationInitialization(ApplicationContext context)
    {
        var app = context.GetApplicationBuilder() as WebApplication ?? throw new("app is null");
        // 配置健康检查端点
        app.MapHealthChecks("/health");
        app.MapHealthChecks("/alive", new()
        {
            Predicate = r => r.Tags.Contains("live")
        });
    }
}