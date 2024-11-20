using System.Diagnostics;
using System.Runtime.InteropServices;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.OpenTelemetry;
using Serilog.Sinks.SystemConsole.Themes;
using WebApi.Test.Unit;
using WebApi.Test.Unit.Common;

// App init start time
var AppInitial = Stopwatch.GetTimestamp();
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
      .Enrich.FromLogContext()
      .WriteTo.Async(wt =>
      {
          if (hbc.HostingEnvironment.IsDevelopment())
          {
              //wt.SpectreConsole();
              wt.Debug();
          }
          if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && SysHelper.IsCurrentUserAdmin())
          {
              // 当为Windows系统时,添加事件日志,需要管理员权限才能写入Windows事件查看器,避免日志信息过多,仅将错误日志写入系统事件查看器
              wt.EventLog(Constant.InstanceName, manageEventSource: true, restrictedToMinimumLevel: LogEventLevel.Error);
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
              c.Protocol = OtlpProtocol.Grpc;
              c.Endpoint = otel["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://localhost:4317";
              c.Headers = new Dictionary<string, string>
              {
                  ["x-otlp-api-key"] = otel["DASHBOARD_OTLP_PRIMARYAPIKEY"] ?? string.Empty
              };
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
if (app.Environment.IsDevelopment()) app.UseDeveloperExceptionPage();

// Add middleware for automatic injection
app.InitializeApplication();
app.MapControllers();

// Log application startup time
_ = Task.Run(() =>
{
    // App init complete time
    var AppComplete = Stopwatch.GetTimestamp();
    Log.Information("Application started in {Elapsed} ms", Stopwatch.GetElapsedTime(AppInitial, AppComplete).TotalMilliseconds);
});
app.Run();