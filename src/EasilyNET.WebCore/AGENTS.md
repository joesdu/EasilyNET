# EASILYNET.WEBCORE - JSON CONVERTERS & WEBSOCKET SERVER

## OVERVIEW

ASP.NET Core helpers: JSON serialization, WebSocket server, exception handling, response time middleware. Depends on `EasilyNET.Core` (uses `BusinessException`, `WebSocket` client infrastructure).

## STRUCTURE

```
EasilyNET.WebCore/
├── JsonConverters/   # System.Text.Json converters (DateTime, DateOnly, TimeOnly, Decimal, Int, Bool)
├── Middleware/       # WebSocketMiddleware, ResponseTimeMiddleware (X-Response-Time header)
├── Handlers/         # BusinessExceptionHandler (IExceptionHandler, .NET 8+, outputs ProblemDetails)
├── WebSocket/        # Server-side WebSocket: IWebSocketSessionManager, WebSocketHandler, registry
├── WebCoreServiceExtensions.cs   # IServiceCollection extensions
└── WebCoreBuilderExtensions.cs   # IApplicationBuilder extensions (UseResponseTime, MapWebSocketHandler)
```

## WHERE TO LOOK

| Task | Location |
|------|----------|
| Add JSON converter | `JsonConverters/` — register via `AddJsonOptions()` in controller setup |
| WebSocket server | `WebSocket/` — `AddWebSocketSessionManager()` + `MapWebSocketHandler<T>()` |
| Exception handler | `Handlers/BusinessExceptionHandler.cs` — handles `BusinessException` from Core |
| Response time | `Middleware/ResponseTimeMiddleware.cs` — `app.UseResponseTime()` |

## CONVENTIONS

- All extension methods in `Microsoft.Extensions.DependencyInjection` namespace
- JSON converters: one per type globally, register in `AddJsonOptions()`
- `BusinessExceptionHandler` only catches `BusinessException` (from Core), not all exceptions
- WebSocket handler is an abstract base class — inherit and implement message handling

## ANTI-PATTERNS

- Multiple converters for same type (conflict)
- Blocking async WebSocket operations
- Placing `UseResponseTime()` too late in the pipeline (should be early for accurate timing)
