using EasilyNET.Core.Misc;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using System.Security.Claims;
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
    const LogEventLevel logLevel = LogEventLevel.Information;
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
          wt.Console(theme: new AnsiConsoleTheme(new Dictionary<ConsoleThemeStyle, string>
          {
              [ConsoleThemeStyle.Text] = "\x1b[38;5;151m",
              [ConsoleThemeStyle.SecondaryText] = "\x1b[38;5;245m",
              [ConsoleThemeStyle.TertiaryText] = "\x1b[38;5;244m",
              [ConsoleThemeStyle.Invalid] = "\x1b[38;5;214m",
              [ConsoleThemeStyle.Null] = "\x1b[38;5;248m",
              [ConsoleThemeStyle.Name] = "\x1b[38;5;141m",
              [ConsoleThemeStyle.String] = "\x1b[38;5;168m",
              [ConsoleThemeStyle.Number] = "\x1b[38;5;141m",
              [ConsoleThemeStyle.Boolean] = "\x1b[38;5;248m",
              [ConsoleThemeStyle.Scalar] = "\x1b[38;5;119m",
              [ConsoleThemeStyle.LevelVerbose] = "\x1b[37m",
              [ConsoleThemeStyle.LevelDebug] = "\x1b[37m",
              [ConsoleThemeStyle.LevelInformation] = "\x1b[37;1m",
              [ConsoleThemeStyle.LevelWarning] = "\x1b[38;5;208m",
              [ConsoleThemeStyle.LevelError] = "\x1b[38;5;197m\x1b[48;5;238m",
              [ConsoleThemeStyle.LevelFatal] = "\x1b[38;5;197m\x1b[48;5;238m"
          }));
          //var mongo = builder.Services.GetService<DbContext>()?.Database;
          //if (mongo is not null)
          //{
          //    wt.MongoDBBson(c =>
          //    {
          //        // 使用链接字符串
          //        //var connectionString = hbc.Configuration["CONNECTIONSTRINGS_MONGO"];
          //        //if (string.IsNullOrWhiteSpace(connectionString)) connectionString = hbc.Configuration.GetConnectionString("Mongo");
          //        //if (string.IsNullOrWhiteSpace(connectionString)) throw new("链接字符串不能为空");
          //        //var mongoUrl = new MongoUrl(connectionString);
          //        //var mongoDbInstance = new MongoClient(mongoUrl).GetDatabase("serilog");

          //        // 设置别的数据库作为日志库
          //        // var mongo = builder.Services.GetService<IMongoDatabase>()?.Client.GetDatabase("serilog") ?? throw new("无法从Ioc容器中获取到Mongo服务");
          //        // sink will use the IMongoDatabase instance provided
          //        c.SetMongoDatabase(mongo);
          //        c.SetCollectionName("serilog");
          //    });
          //}
      });
});

// 自动注入服务模块
builder.Services.AddApplication<AppWebModule>();
builder.Services.AddCurrentUser();
var app = builder.Build();
app.Use((context, next) =>
{
    // 在处理请求之前执行一些自定义逻辑
    // 这里可以对请求进行修改、记录日志、验证身份等操作
    // ReSharper disable once ArrangeObjectCreationWhenTypeNotEvident
    context.User.AddIdentity(new([new Claim(ClaimTypes.NameIdentifier, "帅气的大黄瓜")]));
    return next.Invoke();
    // 在处理请求之后执行一些自定义逻辑
    // 这里可以处理响应、记录日志、执行清理操作等
});
if (app.Environment.IsDevelopment()) _ = app.UseDeveloperExceptionPage();

// 添加自动化注入的一些中间件.
app.InitializeApplication();
app.UseRepeatSubmit();
app.MapControllers();
app.Run();
app.Run();
