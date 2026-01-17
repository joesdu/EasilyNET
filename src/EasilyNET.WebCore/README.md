# EasilyNET.WebCore

面向 .NET 6+ 的 WebAPI 组件集合：中间件、异常处理与 JSON 转换器。

## 功能概览

- **JSON 转换器**：DateTime/DateOnly/TimeOnly/Decimal/Int/Bool
- **异常处理**：`BusinessExceptionHandler`（基于 .NET 8 `IExceptionHandler`）
- **中间件**：`UseResponseTime()` 输出 `X-Response-Time`
- **WebSocket 服务端**：高性能会话与处理器模型

## JSON Converter 使用

当前提供的 Converter：

- `DateTimeJsonConverter`（格式：`yyyy-MM-dd HH:mm:ss`）
- `DateOnlyJsonConverter`（格式：`yyyy-MM-dd`）
- `TimeOnlyJsonConverter`（格式：`HH:mm:ss`）
- `DecimalJsonConverter`（读数值/字符串，写出为字符串）
- `IntJsonConverter`（读数值/字符串，写出为数值）
- `BoolJsonConverter`（读 true/false/"true"/"false"/数值）

```csharp
using EasilyNET.WebCore.JsonConverters;

builder.Services.AddControllers().AddJsonOptions(c =>
{
    c.JsonSerializerOptions.Converters.Add(new DateTimeJsonConverter());
    c.JsonSerializerOptions.Converters.Add(new DateOnlyJsonConverter());
    c.JsonSerializerOptions.Converters.Add(new TimeOnlyJsonConverter());
    c.JsonSerializerOptions.Converters.Add(new DecimalJsonConverter());
    c.JsonSerializerOptions.Converters.Add(new IntJsonConverter());
    c.JsonSerializerOptions.Converters.Add(new BoolJsonConverter());
});
```

## 业务异常处理（.NET 8+）

`BusinessExceptionHandler` 仅处理 `BusinessException`，并输出 `ProblemDetails`。

```csharp
using EasilyNET.WebCore.Handlers;

builder.Services.AddExceptionHandler<BusinessExceptionHandler>();

var app = builder.Build();
app.UseExceptionHandler();
```

## 中间件

### Response Time

在响应头添加 `X-Response-Time`，建议尽量靠前放置：

```csharp
app.UseResponseTime();
```

## WebSocket Server

高性能 WebSocket 服务端支持：

- [使用文档](./WebSocket/README.md)

快速接入：

```csharp
using EasilyNET.WebCore.WebSocket;

builder.Services.AddSingleton<ChatHandler>();

var app = builder.Build();
app.UseWebSockets();
app.MapWebSocketHandler<ChatHandler>("/ws");
```
