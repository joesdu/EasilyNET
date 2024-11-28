using System.Diagnostics;
using System.Management;
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
    var AppComplete = Stopwatch.GetTimestamp();
    Log.Information("Application started in {Elapsed} ms", Stopwatch.GetElapsedTime(AppInitial, AppComplete).TotalMilliseconds);
    var osDescription = RuntimeInformation.OSDescription;
    var osVersion = GetOSVersion();
    var cpuModel = GetCpuModel();
    var memorySize = GetTotalPhysicalMemory();
    Log.Information("Operating System: {OS} {Version}", osDescription, osVersion);
    Log.Information("CPU Model: {CPU}", cpuModel);
    Log.Information("Total Physical Memory: {Memory} GB", Math.Round(memorySize / (1024 * 1024 * 1024d), 2, MidpointRounding.AwayFromZero));
    var cpuSerialNumber = GetCpuSerialNumber();
    var memorySerialNumber = GetMemorySerialNumber();
    Log.Information($"CPU Serial Number: {cpuSerialNumber}");
    Log.Information($"Memory Serial Number: {memorySerialNumber}");
});
app.Run();
return;

string GetOSVersion()
{
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        using var searcher = new ManagementObjectSearcher("SELECT Version FROM Win32_OperatingSystem");
        foreach (var os in searcher.Get())
        {
            return os["Version"]?.ToString() ?? "Unknown";
        }
    }
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
    {
        return File.ReadAllText("/proc/version");
    }
    return "Unknown";
}

string GetCpuModel()
{
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor");
        foreach (var cpu in searcher.Get())
        {
            return cpu["Name"]?.ToString() ?? "Unknown CPU Model";
        }
    }
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    {
        return File.ReadAllText("/proc/cpuinfo").Split('\n').FirstOrDefault(line => line.StartsWith("model name"))?.Split(':')[1].Trim() ?? "Unknown CPU Model";
    }
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
    {
        return File.ReadAllText("/proc/cpuinfo").Split('\n').FirstOrDefault(line => line.StartsWith("machdep.cpu.brand_string"))?.Split(':')[1].Trim() ?? "Unknown CPU Model";
    }
    return "Unknown CPU Model";
}

long GetTotalPhysicalMemory()
{
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        using var searcher = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize FROM Win32_OperatingSystem");
        foreach (var os in searcher.Get())
        {
            return long.Parse(os["TotalVisibleMemorySize"]?.ToString() ?? "0") * 1024;
        }
    }
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
    {
        var info = new ProcessStartInfo("sh", "-c \"grep MemTotal /proc/meminfo\"")
        {
            RedirectStandardOutput = true
        };
        using var process = Process.Start(info);
        using var reader = process!.StandardOutput;
        var result = reader.ReadToEnd();
        return long.Parse(result.Split(':')[1].Trim().Split(' ')[0]) * 1024;
    }
    return 0;
}

string GetCpuSerialNumber()
{
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        using var searcher = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor");
        foreach (var cpu in searcher.Get())
        {
            return cpu["ProcessorId"]?.ToString() ?? "Unknown CPU Serial Number";
        }
    }
    return "Unknown CPU Serial Number";
}

string GetMemorySerialNumber()
{
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        using var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_PhysicalMemory");
        foreach (var memory in searcher.Get())
        {
            return memory["SerialNumber"]?.ToString() ?? "Unknown Memory Serial Number";
        }
    }
    return "Unknown Memory Serial Number";
}