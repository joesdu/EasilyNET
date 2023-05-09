using EasilyNET.AutoDependencyInjection.Extensions;
using EasilyNET.Core.Misc;
using EasilyNET.Core.PinYin;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Serilog;
using Serilog.Events;
using WebApi.Test.Unit;

AssemblyHelper.AddExcludeLibs("Npgsql.");
var builder = WebApplication.CreateBuilder(args);
// 汉字转拼音
Console.WriteLine("微软爸爸就是牛逼");
Console.WriteLine(PyTools.GetPinYin("微软爸爸就是牛逼"));
Console.WriteLine(PyTools.GetInitials("微软爸爸就是牛逼"));
// 配置Kestrel支持HTTP1,2,3
builder.WebHost.ConfigureKestrel((_, op) =>
{
    // 配置监听端口和IP
    op.ListenAnyIP(443, lo =>
    {
        lo.Protocols = HttpProtocols.Http1AndHttp2AndHttp3;
        lo.UseHttps();
    });
    op.ListenAnyIP(80, lo => lo.Protocols = HttpProtocols.Http1);
    // 当需要上传文件的时候配置这个东西,防止默认值太小影响大文件上传
    op.Limits.MaxRequestBodySize = null;
});
// 配置IIS上传文件大小限制.
builder.Services.Configure<IISServerOptions>(c => c.MaxRequestBodySize = null);

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