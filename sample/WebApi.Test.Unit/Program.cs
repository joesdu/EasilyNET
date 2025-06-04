using System.Diagnostics;
using System.Runtime.InteropServices;
using EasilyNET.Core.Misc;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.OpenTelemetry;
using Serilog.Sinks.SystemConsole.Themes;
using WebApi.Test.Unit;
using WebApi.Test.Unit.Common;

#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释

AssemblyHelper.LoadFromAllDll = false;
// App init start time
var appInitial = Stopwatch.GetTimestamp();
Console.Title = $"❤️ {Constant.InstanceName}";
var builder = WebApplication.CreateBuilder(args);

// 添加Serilog配置
builder.Host.UseSerilog((hbc, lc) =>
{
    var logLevel = hbc.HostingEnvironment.IsDevelopment() ? LogEventLevel.Information : LogEventLevel.Error;
    lc.ReadFrom.Configuration(hbc.Configuration)
      .MinimumLevel.Override("Microsoft", logLevel)
      .MinimumLevel.Override("System", logLevel)
      // 添加下面这行来过滤掉 Microsoft.Extensions.Resilience 的日志
      .MinimumLevel.Override("Polly", LogEventLevel.Warning)
      .MinimumLevel.Override("Microsoft.AspNetCore", logLevel)
      .MinimumLevel.Override("Microsoft.AspNetCore.Cors.Infrastructure.CorsService", logLevel)
      .MinimumLevel.Override("Microsoft.AspNetCore.Mvc", logLevel)
      .MinimumLevel.Override("Microsoft.AspNetCore.Hosting", logLevel)
      .Enrich.FromLogContext()
      .WriteTo.Async(wt =>
      {
          if (hbc.HostingEnvironment.IsDevelopment())
          {
              wt.Debug();
          }
          if (hbc.HostingEnvironment.IsProduction())
          {
              wt.Map(le => (le.Timestamp.DateTime, le.Level), (key, log) =>
                  log.Async(o => o.File($"logs{Path.DirectorySeparatorChar}{key.Level}{Path.DirectorySeparatorChar}.log",
                      shared: true,
                      rollingInterval: RollingInterval.Day)));
          }
          wt.Console(theme: AnsiConsoleTheme.Code);
          var otel = hbc.Configuration.GetSection("OpenTelemetry");
          wt.OpenTelemetry(c =>
          {
              c.Endpoint = new(otel["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://localhost:4317");
              c.Protocol = OtlpProtocol.Grpc;
              c.ResourceAttributes = new Dictionary<string, object>
              {
                  ["service.name"] = Constant.InstanceName
              };
          });
      });
});

// Automatically inject service modules
builder.Services.AddApplicationModules<AppWebModule>();
var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Add middleware for automatic injection
app.InitializeApplication();
app.MapControllers();

// 启动，关闭事件
app.Lifetime.ApplicationStopping.Register(OnShutdown);
app.Lifetime.ApplicationStarted.Register(OnStarted);

// .NET·启动
app.Run();
return;

void OnStarted()
{
    var appComplete = Stopwatch.GetTimestamp();
    Log.Information("Operating System: {OS}", RuntimeInformation.OSDescription);
    Log.Information("Application started in {Elapsed} ms", Stopwatch.GetElapsedTime(appInitial, appComplete).TotalMilliseconds);
}

#pragma warning disable IDE0062
void OnShutdown() => Log.Information("Application shutdown");
#pragma warning restore IDE0062