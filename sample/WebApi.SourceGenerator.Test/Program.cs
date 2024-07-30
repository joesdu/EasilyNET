using WebApi.SourceGenerator.Test;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// 自动注入服务模块
builder.Services.AddApplicationModules<AppWebModule>();
var app = builder.Build();
if (app.Environment.IsDevelopment()) app.UseDeveloperExceptionPage();

// 添加自动化注入的一些中间件.
app.InitializeApplication();
app.MapControllers();
app.Run();