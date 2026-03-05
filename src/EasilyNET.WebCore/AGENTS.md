# EASILYNET.WEBCORE - JSON CONVERTERS & WEBSOCKET SERVER

## OVERVIEW

ASP.NET Core helpers: JSON serialization, WebSocket server, exception handling, response time middleware.

## STRUCTURE

```
EasilyNET.WebCore/
├── JsonConverters/   # System.Text.Json converters (DateOnly, TimeOnly, Decimal, etc.)
├── Middleware/       # WebSocketMiddleware, ResponseTimeMiddleware
├── Handlers/         # BusinessExceptionHandler
└── WebSocket/        # Server-side WebSocket: session manager, handler, registry
```

## WHERE TO LOOK

| Task               | Location                                                   |
| ------------------ | ---------------------------------------------------------- |
| Add JSON converter | `JsonConverters/` - register in `WebCoreServiceExtensions` |
| WebSocket server   | `WebSocket/` - IWebSocketSessionManager, WebSocketHandler  |
| Exception handler  | `Handlers/BusinessExceptionHandler.cs`                     |

## CONVENTIONS

- All extension methods in `Microsoft.Extensions.DependencyInjection` namespace
- Use `AddWebSocketSessionManager()` for WebSocket-related registration
- JSON converters: one serializer per type globally

## ANTI-PATTERNS

- Multiple converters for same type (conflict)
- Blocking async WebSocket operations
