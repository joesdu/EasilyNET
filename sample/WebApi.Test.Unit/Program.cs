using System.Diagnostics;
using System.Runtime.InteropServices;
using EasilyNET.Core.Misc;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
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
    var startupTime = Stopwatch.GetElapsedTime(appInitial, appComplete);

    // 获取服务器监听地址
    var serverAddressesFeature = app.Services.GetRequiredService<IServer>()
                                    .Features.Get<IServerAddressesFeature>();
    var addresses = serverAddressesFeature?.Addresses ?? [];
    var listeningOn = addresses.Count > 0 ? string.Join(", ", addresses) : "未知地址";
    
    // 检查是否支持UTF-8字符
    var supportsEmoji = TextWriterExtensions.IsUtf8Supported() && !Console.IsOutputRedirected;
    
    // 根据支持情况选择字符
    var (rocket, computer, lightning, globe, network, calendar, wrench, house, checkmark, party) = supportsEmoji 
        ? ("🚀", "🖥️", "⚡", "🌍", "🌐", "📅", "🔧", "🏠", "✅", "🎉")
        : (">", "[PC]", "*", "[ENV]", "[NET]", "[TIME]", "[.NET]", "[PID]", "[OK]", "!");
    
    var borderChar = supportsEmoji ? "─" : "-";
    var verticalChar = supportsEmoji ? "│" : "|";
    var topLeft = supportsEmoji ? "┌" : "+";
    var topRight = supportsEmoji ? "┐" : "+";
    var bottomLeft = supportsEmoji ? "└" : "+";
    var bottomRight = supportsEmoji ? "┘" : "+";
    var leftTee = supportsEmoji ? "├" : "+";
    var rightTee = supportsEmoji ? "┤" : "+";
    
    // 动态计算最大内容宽度
    var infoLines = new[]
    {
        $" {rocket} {Constant.InstanceName} Application Started Successfully!",
        $" {computer}  Operating System: {RuntimeInformation.OSDescription.Trim()}",
        $" {lightning} Startup Time: {startupTime.TotalMilliseconds:F2} ms",
        $" {globe} Environment: {app.Environment.EnvironmentName}",
        $" {network} Listening On: {listeningOn}",
        $" {calendar} Started At: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
        $" {wrench} .NET Version: {RuntimeInformation.FrameworkDescription}",
        $" {house} Process ID: {Environment.ProcessId}"
    };
    
    // 计算显示宽度(考虑emoji占用2个字符位置的情况)
    static int GetDisplayWidth(string text, bool hasEmoji)
    {
        if (!hasEmoji) return text.Length;
        
        // 简单的emoji宽度估算：大多数emoji占用2个字符位置
        var emojiCount = 0;
        foreach (var c in text)
        {
            if (c > 127 && char.IsSymbol(c)) // 简单的emoji检测
                emojiCount++;
        }
        return text.Length + emojiCount; // emoji额外占用1个字符位置
    }
    
    var maxContentWidth = infoLines.Max(line => GetDisplayWidth(line, supportsEmoji));
    var totalWidth = Math.Max(maxContentWidth + 2, 65); // 最小宽度65，+2为左右边框
    var borderLength = totalWidth - 2; // 减去左右边框字符
    
    var topBorder = topLeft + new string(borderChar[0], borderLength) + topRight;
    var middleBorder = leftTee + new string(borderChar[0], borderLength) + rightTee;
    var bottomBorder = bottomLeft + new string(borderChar[0], borderLength) + bottomRight;
    
    // 输出格式化的信息
    Log.Information(topBorder);
    
    foreach (var line in infoLines)
    {
        var displayWidth = GetDisplayWidth(line, supportsEmoji);
        var padding = new string(' ', Math.Max(0, borderLength - displayWidth));
        Log.Information($"{verticalChar}{line}{padding} {verticalChar}");
        
        if (line == infoLines[0]) // 第一行后添加分隔线
        {
            Log.Information(middleBorder);
        }
    }
    
    Log.Information(bottomBorder);
    Log.Information("{Checkmark} {ApplicationName} is ready to serve requests! {Party}", checkmark, Constant.InstanceName, party);
}

void OnShutdown()
{
    var appShutdown = Stopwatch.GetTimestamp();
    var appUptime = Stopwatch.GetElapsedTime(appInitial, appShutdown);
    
    // 检查是否支持UTF-8字符
    var supportsEmoji = TextWriterExtensions.IsUtf8Supported() && !Console.IsOutputRedirected;
    
    // 根据支持情况选择字符
    var (stop, calendar, clock, wave, sparkle) = supportsEmoji 
        ? ("🛑", "📅", "⏱️", "👋", "💫")
        : ("[STOP]", "[TIME]", "[UPTIME]", "[BYE]", "*");
    
    var borderChar = supportsEmoji ? "─" : "-";
    var verticalChar = supportsEmoji ? "│" : "|";
    var topLeft = supportsEmoji ? "┌" : "+";
    var topRight = supportsEmoji ? "┐" : "+";
    var bottomLeft = supportsEmoji ? "└" : "+";
    var bottomRight = supportsEmoji ? "┘" : "+";
    var leftTee = supportsEmoji ? "├" : "+";
    var rightTee = supportsEmoji ? "┤" : "+";
    
    // 动态计算最大内容宽度
    var infoLines = new[]
    {
        $" {stop} {Constant.InstanceName} Application Shutdown Initiated",
        $" {calendar} Shutdown Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
        $" {clock} Total Uptime: {appUptime.TotalSeconds:F2} seconds"
    };
    
    // 计算显示宽度(考虑emoji占用2个字符位置的情况)
    static int GetDisplayWidth(string text, bool hasEmoji)
    {
        if (!hasEmoji) return text.Length;
        
        // 简单的emoji宽度估算：大部分emoji占用2个字符位置
        var emojiCount = 0;
        foreach (var c in text)
        {
            if (c > 127 && char.IsSymbol(c)) // 简单的emoji检测
                emojiCount++;
        }
        return text.Length + emojiCount; // emoji额外占用1个字符位置
    }
    
    var maxContentWidth = infoLines.Max(line => GetDisplayWidth(line, supportsEmoji));
    var totalWidth = Math.Max(maxContentWidth + 2, 65); // 最小宽度65，+2为左右边框
    var borderLength = totalWidth - 2; // 减去左右边框字符
    
    var topBorder = topLeft + new string(borderChar[0], borderLength) + topRight;
    var middleBorder = leftTee + new string(borderChar[0], borderLength) + rightTee;
    var bottomBorder = bottomLeft + new string(borderChar[0], borderLength) + bottomRight;
    
    // 输出格式化的信息
    Log.Information(topBorder);
    
    foreach (var line in infoLines)
    {
        var displayWidth = GetDisplayWidth(line, supportsEmoji);
        var padding = new string(' ', Math.Max(0, borderLength - displayWidth));
        Log.Information($"{verticalChar}{line}{padding} {verticalChar}");
        
        if (line == infoLines[0]) // 第一行后添加分隔线
        {
            Log.Information(middleBorder);
        }
    }
    
    Log.Information(bottomBorder);
    Log.Information("{Wave} {ApplicationName} shutdown completed gracefully! Goodbye! {Sparkle}", wave, Constant.InstanceName, sparkle);
}