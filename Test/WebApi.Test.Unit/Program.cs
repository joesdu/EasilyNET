using EasilyNET.AutoDependencyInjection.Extensions;
using EasilyNET.Core.PinYin;
using Serilog;
using Serilog.Events;
using WebApi.Test.Unit;

var builder = WebApplication.CreateBuilder(args);
Console.WriteLine("微软爸爸就是牛逼");
Console.WriteLine(PyTools.GetPinYin("微软爸爸就是牛逼"));
Console.WriteLine(PyTools.GetInitials("微软爸爸就是牛逼"));

//添加Serilog配置
_ = builder.Host.UseSerilog((hbc, lc) =>
{
    const LogEventLevel logLevel = LogEventLevel.Information;
    _ = lc.ReadFrom.Configuration(hbc.Configuration)
          .MinimumLevel.Override("Microsoft", logLevel)
          .MinimumLevel.Override("System", logLevel)
          .Enrich.FromLogContext();
    _ = lc.WriteTo.Async(wt => wt.Console());
    _ = lc.WriteTo.Debug();
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