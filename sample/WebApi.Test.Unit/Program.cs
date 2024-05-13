using EasilyNET.Core.Misc;
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
              c.Endpoint = hbc.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://localhost:4317";
              c.Headers = new Dictionary<string, string>
              {
                  ["x-otlp-api-key"] = hbc.Configuration["DASHBOARD_OTLP_PRIMARYAPIKEY"] ?? string.Empty
              };
              c.ResourceAttributes = new Dictionary<string, object>
              {
                  ["service.name"] = hbc.Configuration["OTEL_SERVICE_NAME"] ?? "EasilyNET",
                  ["service.version"] = "1.0.0"
              };
          });
      });
});

// 自动注入服务模块
builder.Services.AddApplication<AppWebModule>();
var app = builder.Build();
if (app.Environment.IsDevelopment()) app.UseDeveloperExceptionPage();
app.Use(async (c, next) =>
{
    c.Response.Headers.AddRange([
        new("Strict-Transport-Security", "max-age=63072000; includeSubdomains; preload"),
        new("X-Content-Type-Options", "nosniff"),
        new("X-XSS-Protection", "1; mode=block"),
        new("X-Frame-Options", "sameorigin"),
        new("Referrer-Policy", "strict-origin-when-cross-origin"),
        new("X-Download-Options", "noopen"),
        new("X-Permitted-Cross-Domain-Policies", "none"),
        new("Cache-control", "max-age=1, no-cache, no-store, must-revalidate, private")
    ]);
    await next();
});

// 添加自动化注入的一些中间件.
app.InitializeApplication();
// 配置健康检查端点
app.MapHealthChecks("/health");
app.MapHealthChecks("/alive", new()
{
    Predicate = r => r.Tags.Contains("live")
});
app.MapControllers();
app.Run();