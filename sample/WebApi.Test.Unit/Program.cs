using EasilyNET.Core.Misc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.OpenTelemetry;
using Serilog.Sinks.SystemConsole.Themes;
using WebApi.Test.Unit;

Console.Title = "❤️ EasilyNET";
AssemblyHelper.AddExcludeLibs("Npgsql.");
var builder = WebApplication.CreateBuilder(args);

// 配置Kestrel支持HTTP1,2,3
//builder.WebHost.ConfigureKestrel((_, op) =>
//{
//    // 配置监听端口和IP
//    op.ListenAnyIP(443, lo =>
//    {
//        lo.Protocols = HttpProtocols.Http1AndHttp2AndHttp3;
//        lo.UseHttps();
//    });
//    op.ListenAnyIP(80, lo => lo.Protocols = HttpProtocols.Http1);
//});

// 添加Serilog配置
builder.Host.UseSerilog((hbc, lc) =>
{
    var logLevel = LogEventLevel.Error;
    if (hbc.HostingEnvironment.IsDevelopment()) logLevel = LogEventLevel.Information;
    lc.ReadFrom.Configuration(hbc.Configuration)
      .MinimumLevel.Override("Microsoft", logLevel)
      .MinimumLevel.Override("System", logLevel)
      .Enrich.FromLogContext()
      .WriteTo.Async(wt =>
      {
          if (hbc.HostingEnvironment.IsDevelopment())
          {
              //wt.SpectreConsole();
              wt.Debug();
          }
          wt.Console(theme: AnsiConsoleTheme.Code);
          wt.OpenTelemetry(c =>
          {
              c.Protocol = OtlpProtocol.Grpc;
              c.Endpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? "http://localhost:4317";
              c.Headers = new Dictionary<string, string>
              {
                  ["x-otlp-api-key"] = Environment.GetEnvironmentVariable("DASHBOARD__OTLP__PRIMARYAPIKEY") ?? string.Empty
              };
              c.ResourceAttributes = new Dictionary<string, object>
              {
                  ["service.name"] = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ?? "EasilyNET",
                  ["service.version"] = "1.0.0"
              };
          });
      });
});

// OpenTelemetry
builder.Services.AddOpenTelemetry()
       .WithMetrics(c =>
       {
           c.AddRuntimeInstrumentation();
           c.AddMeter([
               "Microsoft.AspNetCore.Hosting",
               "Microsoft.AspNetCore.Server.Kestrel",
               "System.Net.Http",
               "WebApi.Test.Unit"
           ]);
           c.AddOtlpExporter();
       })
       .WithTracing(c =>
       {
           if (builder.Environment.IsDevelopment())
           {
               c.SetSampler<AlwaysOnSampler>();
           }
           c.AddAspNetCoreInstrumentation();
           c.AddHttpClientInstrumentation();
           c.AddGrpcClientInstrumentation();
           c.AddOtlpExporter();
       });
builder.Services.Configure<OtlpExporterOptions>(c => c.Headers = $"x-otlp-api-key={Environment.GetEnvironmentVariable("DASHBOARD__OTLP__PRIMARYAPIKEY")}");
builder.Services.AddHealthChecks().AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);
builder.Services.ConfigureHttpClientDefaults(c => c.AddStandardResilienceHandler());
builder.Services.AddMetrics();
// 自动注入服务模块
builder.Services.AddApplication<AppWebModule>();
//
var app = builder.Build();
if (app.Environment.IsDevelopment()) app.UseDeveloperExceptionPage();
// 异常处理中间件
app.UseExceptionHandler();
// 添加自动化注入的一些中间件.
app.InitializeApplication();
app.MapHealthChecks("/health");
app.MapHealthChecks("/alive", new()
{
    Predicate = r => r.Tags.Contains("live")
});
app.MapControllers();
app.Run();