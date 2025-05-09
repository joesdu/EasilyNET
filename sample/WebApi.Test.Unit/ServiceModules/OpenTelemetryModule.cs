using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Modules;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
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
    public override async Task ConfigureServices(ConfigureServicesContext context)
    {
        var otel = context.ServiceProvider.GetConfiguration().GetSection("OpenTelemetry");
        var env = context.ServiceProvider.GetRequiredService<IWebHostEnvironment>() ?? throw new("获取服务出错");
        context.Services.AddOpenTelemetry()
               .ConfigureResource(c => c.AddService(Constant.InstanceName))
               .WithMetrics(c =>
               {
                   c.AddRuntimeInstrumentation();
                   c.AddProcessInstrumentation();
                   c.AddMeter("Microsoft.AspNetCore.Hosting", "Microsoft.AspNetCore.Server.Kestrel", "System.Net.Http", "WebApi.Test.Unit");
                   c.AddOtlpExporter();
               })
               .WithLogging(c => c.AddOtlpExporter())
               .WithTracing(c =>
               {
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
            c.Endpoint = new(otel["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://localhost:4317");
            c.Protocol = OtlpExportProtocol.Grpc;
        });
        context.Services.AddHealthChecks().AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);
        context.Services.ConfigureHttpClientDefaults(c => c.AddStandardResilienceHandler());
        context.Services.AddMetrics();
        await Task.CompletedTask;
    }

    public override async Task ApplicationInitialization(ApplicationContext context)
    {
        var app = context.GetApplicationHost() as WebApplication;
        // 配置健康检查端点
        app?.MapHealthChecks("/health");
        app?.MapHealthChecks("/alive", new()
        {
            Predicate = r => r.Tags.Contains("live")
        });
        await Task.CompletedTask;
    }
}

file static class OpenTelemetryExtensions
{
    public static void AddMongoDBInstrumentation(this TracerProviderBuilder builder) => builder.AddSource("EasilyNET.Mongo.ConsoleDebug");
}