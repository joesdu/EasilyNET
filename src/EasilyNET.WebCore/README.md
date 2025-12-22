# EasilyNET.WebCore

一些.Net 6+ 的 WebApi 一些中间件以及部分数据类型到 Json 的转换

### Changelog

- 新增 BusinessExceptionHandler 用于适应.NET8 新增的全局异常处理
- 移除 ResultObject.cs 因为这种统一返回格式属于特殊的要求,正常的 HTTP 请求应该直接返回数据,而请求是否成功以及产生的异常应该由
  HTTP 状态码反应.
- 同时将涉及到该类的 Filter 和中间件移动到 Api 例子中.

### EasilyNET.WebCore JsonConverter 使用?

- 该库目前补充的 Converter 有: DateTimeConverter, DateTimeNullConverter, TimeSpanJsonConverter, TimeOnly, DateOnly
- 其中 TimeOnly 和 DateOnly 仅支持.Net6+ API 内部使用,传入和传出 Json 仅支持固定格式字符串
- 如: **`DateOnly👉"2021-11-11"`**, **`TimeOnly👉"23:59:25"`**

- 使用 Nuget 安装 EasilyNET.WebCore
- 然后在上述 Program.cs 中添加如下内容

- .Net 6 +

```csharp
// Add services to the container.
builder.Services.AddControllers().AddJsonOptions(c =>
{
    c.JsonSerializerOptions.Converters.Add(new SystemTextJsonConvert.DateTimeConverter());
    c.JsonSerializerOptions.Converters.Add(new SystemTextJsonConvert.DateTimeNullConverter());
});
```

### EasilyNET.WebCore 中间件使用?

目前支持全局 API 执行时间中间件

- 新增限流中间件(防抖),用于避免短时间内,重复请求
- 使用 Nuget 安装 # EasilyNET.WebCore
- 然后在 Program.cs 中添加如下内容

- .Net 6 +

```csharp
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) app.UseDeveloperExceptionPage();

app.UseResponseTime(); // 全局Action执行时间
...
app.Run();
```

### WebSocket Server

高性能 WebSocket 服务端支持。

- [使用文档](.\WebSocket\README.md)
