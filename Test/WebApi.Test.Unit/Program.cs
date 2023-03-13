using EasilyNET.AutoDependencyInjection.Extensions;
using Serilog;
using Serilog.Events;
using WebApi.Test.Unit;

var builder = WebApplication.CreateBuilder(args);

//添加Serilog配置
_ = builder.Host.UseSerilog((hbc, lc) =>
{
    const LogEventLevel logLevel = LogEventLevel.Information;
    _ = lc.ReadFrom.Configuration(hbc.Configuration)
          .MinimumLevel.Override("Microsoft", logLevel)
          .MinimumLevel.Override("System", logLevel)
          .Enrich.FromLogContext();
    _ = lc.WriteTo.Async(wt => wt.Console( /*new ElasticsearchJsonFormatter()*/));
    _ = lc.WriteTo.Debug();
    //_ = lc.WriteTo.MongoDB(hbc.Configuration["Logging:DataBase:Mongo"]);
    // 不建议将日志写入文件,会造成日志文件越来越大,服务器可能因此宕机.
    // 若是需要分文件写入需要引入包 Serilog.Sinks.Map
    //_ = lc.WriteTo.Map(le =>
    //{
    //    static (DateTime time, LogEventLevel level) MapData(LogEvent @event) => (@event.Timestamp.LocalDateTime, @event.Level);
    //    return MapData(le);
    //}, (key, log) =>
    //{
    //    log.Async(o => o.File(Path.Combine("logs", @$"{key.time:yyyyMMdd}{Path.DirectorySeparatorChar}{key.level.ToString().ToLower()}.log"), logEventLevel));
    //});
});

// 自动注入服务模块
builder.Services.AddApplication<AppWebModule>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) _ = app.UseDeveloperExceptionPage();

// 添加自动化注入的一些中间件.
app.InitializeApplication();
app.MapControllers();
app.Run();