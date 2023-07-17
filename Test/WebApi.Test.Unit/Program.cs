using EasilyNET.AutoDependencyInjection.Extensions;
using EasilyNET.Core.Misc;
using Serilog;
using Serilog.Events;
using WebApi.Test.Unit;

Console.Title = "EasilyNET";
AssemblyHelper.AddExcludeLibs("Npgsql.");
var builder = WebApplication.CreateBuilder(args);

// 配置Kestrel支持HTTP1,2,3
//builder.WebHost.ConfigureKestrel((_, op) =>
//{
//    // 配置监听端口和IP
//    op.ListenAnyIP(443, lo =>
//    {
//#if NET6_0
//#pragma warning disable CA2252 // 此 API 需要选择加入预览功能
//#endif
//        lo.Protocols = HttpProtocols.Http1AndHttp2AndHttp3;
//#if NET6_0
//#pragma warning disable CA2252 // 此 API 需要选择加入预览功能
//#endif
//        lo.UseHttps();
//    });
//    op.ListenAnyIP(80, lo => lo.Protocols = HttpProtocols.Http1);
//});

// 自动注入服务模块
builder.Services.AddApplication<AppWebModule>();

//添加Serilog配置
_ = builder.Host.UseSerilog((hbc, lc) =>
{
    const LogEventLevel logLevel = LogEventLevel.Information;
    _ = lc.ReadFrom.Configuration(hbc.Configuration)
          .MinimumLevel.Override("Microsoft", logLevel)
          .MinimumLevel.Override("System", logLevel)
          .Enrich.FromLogContext()
          .WriteTo.Async(wt =>
          {
              wt.Console();
              wt.Debug();
              var mongo = builder.Services.GetService<DbContext>()?.Database;
              if (mongo is not null)
              {
                  wt.MongoDBBson(c =>
                  {
                      // 使用链接字符串
                      //var connectionString = hbc.Configuration["CONNECTIONSTRINGS_MONGO"];
                      //if (string.IsNullOrWhiteSpace(connectionString)) connectionString = hbc.Configuration.GetConnectionString("Mongo");
                      //if (string.IsNullOrWhiteSpace(connectionString)) throw new("链接字符串不能为空");
                      //var mongoUrl = new MongoUrl(connectionString);
                      //var mongoDbInstance = new MongoClient(mongoUrl).GetDatabase("serilog");

                      // 设置别的数据库作为日志库
                      // var mongo = builder.Services.GetService<IMongoDatabase>()?.Client.GetDatabase("serilog") ?? throw new("无法从Ioc容器中获取到Mongo服务");
                      // sink will use the IMongoDatabase instance provided
                      c.SetMongoDatabase(mongo);
                      c.SetCollectionName("serilog");
                  });
              }
          });
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) _ = app.UseDeveloperExceptionPage();

// 添加自动化注入的一些中间件.
app.InitializeApplication();
app.MapControllers();
app.Run();