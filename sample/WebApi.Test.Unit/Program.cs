using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using EasilyNET.Core.Misc;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.OpenTelemetry;
using WebApi.Test.Unit;
using WebApi.Test.Unit.Common;
using WebApi.Test.Unit.Controllers;
using WebApi.Test.Unit.Decorators;

#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释

// App init start time
var appInitial = Stopwatch.GetTimestamp();
AssemblyHelper.Configure(o =>
{
    o.AllowDirectoryProbe = true;
    o.ScanAllRuntimeLibraries = false;
});
// Enable UTF-8 support in console
_ = TextWriterExtensions.IsUtf8Supported();
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
          wt.Console(theme: DraculaConsoleTheme.Dark);
          if (hbc.HostingEnvironment.IsDevelopment())
          {
              wt.Debug();
          }
          if (hbc.HostingEnvironment.IsProduction())
          {
              wt.Map(le => (le.Timestamp.DateTime, le.Level), (key, log) =>
                  log.Async(o => o.File(Path.Combine(AppContext.BaseDirectory, "logs", key.Level.ToString(), ".log"),
                      shared: true, formatProvider: CultureInfo.CurrentCulture, retainedFileTimeLimit: TimeSpan.FromDays(7),
                      outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}",
                      rollingInterval: RollingInterval.Day)));
          }
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
// Register Decorators
builder.Services.Decorate<IFooService, TestNoDefaultDecorator>();
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

// .NET 启动
app.Run();
return;

void OnStarted()
{
    var appComplete = Stopwatch.GetTimestamp();
    var startupTime = Stopwatch.GetElapsedTime(appInitial, appComplete);
    Log.Information("Operating System: {OS}", RuntimeInformation.OSDescription);
    Log.Information("Started in {Elapsed} ms", startupTime.TotalMilliseconds);
    Log.Information(".NET version: {FrameworkDescription}", RuntimeInformation.FrameworkDescription);
    Log.Information("Process ID: {ProcessId}", Environment.ProcessId);
    Log.Information("Application is ready to serve requests! 🎉");
}

static void OnShutdown()
{
    Log.Information("👋 {InstanceName} shutdown completed gracefully! Goodbye! 💫", Constant.InstanceName);
}