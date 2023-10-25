# EasilyNET.WebCore

一些.Net 6+ 的 WebApi 常用中间件和一些 Filter,以及部分数据类型到 Json 的转换

# EasilyNET.WebCore Filter 使用?

目前支持异常处理和返回数据格式化

- 使用 Nuget 安装 EasilyNET.WebCore
- 然后在 Program.cs 中添加如下内容

- Net 6 +

```csharp
// Add services to the container.
builder.Services.AddControllers(c =>
{
    c.Filters.Add<ExceptionFilter>(); // 异常处理Filter
    c.Filters.Add<ActionExecuteFilter>(); // 返回数据格式化Filter
});
```

# EasilyNET.WebCore JsonConverter 使用?

- 该库目前补充的 Converter 有: DateTimeConverter, DateTimeNullConverter, TimeSpanJsonConverter, TimeOnly, DateOnly
- 其中 TimeOnly 和 DateOnly 仅支持.Net6+ API 内部使用,传入和传出 Json 仅支持固定格式字符串
- 如: **`DateOnly👉"2021-11-11"`**, **`TimeOnly👉"23:59:25"`**

- 使用 Nuget 安装 EasilyNET.WebCore
- 然后在上述 Program.cs 中添加如下内容

- .Net 6 +

```csharp
// Add services to the container.
builder.Services.AddControllers(c =>
{
    c.Filters.Add<ExceptionFilter>(); // 异常处理Filter
    c.Filters.Add<ActionExecuteFilter>(); // 返回数据格式化Filter
}).AddJsonOptions(c =>
{
    c.JsonSerializerOptions.Converters.Add(new SystemTextJsonConvert.DateTimeConverter());
    c.JsonSerializerOptions.Converters.Add(new SystemTextJsonConvert.DateTimeNullConverter());
});
```

# EasilyNET.WebCore 中间件使用?

目前支持全局 API 执行时间中间件

- 新增限流中间件(防抖),用于避免短时间内,重复请求
- 使用 Nuget 安装 # EasilyNET.WebCore
- 然后在 Program.cs 中添加如下内容

- .Net 6 +

```csharp
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) app.UseDeveloperExceptionPage();

app.UseHoyoResponseTime(); // 全局Action执行时间
...
app.Run();
```

# .Net 6 中使用 3 种库的方法集合

- Program.cs 文件

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers(c =>
{
    c.Filters.Add<ExceptionFilter>(); // 异常处理Filter
    c.Filters.Add<ActionExecuteFilter>(); // 返回数据格式化Filter
}).AddJsonOptions(c =>
{
    c.JsonSerializerOptions.Converters.Add(new SystemTextJsonConvert.DateTimeConverter());
    c.JsonSerializerOptions.Converters.Add(new SystemTextJsonConvert.DateTimeNullConverter());
});
...

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) app.UseDeveloperExceptionPage();

app.UseHoyoResponseTime();
...
```

- API 响应结果示例

```json
{
  "statusCode": 200,
  "msg": "success",
  "data": [
    {
      "date": "2021-10-10 17:38:16",
      "temperatureC": 6,
      "temperatureF": 42,
      "summary": "Freezing"
    },
    {
      "date": "2021-10-11 17:38:16",
      "temperatureC": 18,
      "temperatureF": 64,
      "summary": "Warm"
    }
  ]
}
```

- Response headers

```
hoyo-response-time: 5 ms
```

# EasilyNET.WebCore 当前用户 使用?

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddCurrentUser();
var app = builder.Build();
var currentUser=app.Services.GetService<ICurrentUser>();
currentUser.GetUserId<long>();
或者
currentUser.GetUserId<long>("sub");
```