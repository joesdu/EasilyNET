# EASILYNET.RABBITBUS.ASPNETCORE - EVENT BUS

## OVERVIEW

RabbitMQ event bus with publisher confirms, Channel<T> retry queue, Polly resilience, dead letter store, consumer
middleware pipeline, and OpenTelemetry tracing. Depends on `EasilyNET.RabbitBus.Core`. Key dep: RabbitMQ.Client 7.2.1,
Polly (via Microsoft.Extensions.Resilience).

## STRUCTURE

```
EasilyNET.RabbitBus.AspNetCore/
├── Abstractions/   # IDeadLetterStore (public), internal interfaces
├── Builder/        # RabbitBusBuilder (710 lines, fluent config API)
├── Configs/        # Event/handler configuration models
├── Health/         # Health check integration
├── Manager/        # EventBus, EventPublisher, EventHandlerInvoker, PersistentConnection
├── Metrics/        # System.Diagnostics.Metrics instrumentation (meter: EasilyNET.RabbitBus)
├── Serializer/     # IBusSerializer, JSON default
├── Services/       # MessageConfirmService (background retry)
├── Stores/         # InMemoryDeadLetterStore
└── Utilities/      # Helpers
```

Sibling `EasilyNET.RabbitBus.Core` provides: `Event` base class, `IEventHandler<T>`, `IBus`, `IEvent`, enums (`EModel`).

## WHERE TO LOOK

| Task                     | Location                                                             |
|--------------------------|----------------------------------------------------------------------|
| Add event configuration  | `AddEvent<T>()` in `RabbitServiceExtension.cs`                       |
| Modify connection logic  | `Manager/PersistentConnection.cs` (528 lines, complexity hotspot)    |
| Change retry behavior    | `Manager/EventPublisher.cs` (625 lines, complexity hotspot)          |
| Handler execution        | `Manager/EventHandlerInvoker.cs` (534 lines, complexity hotspot)     |
| Custom serializer        | Implement `IBusSerializer`, use `WithSerializer<T>()`                |
| Custom dead letter store | Implement `IDeadLetterStore`, register as singleton                  |
| Consumer middleware      | Implement `IEventMiddleware<T>`, use `WithMiddleware<T>()`           |
| Fallback handler         | Implement `IEventFallbackHandler<T>`, use `WithFallbackHandler<T>()` |
| Per-event resilience     | `WithHandlerResilience(builder => ...)`                              |

## COMPLEXITY HOTSPOTS

The `Manager/` directory contains the 3 most complex files in this package:

- `EventPublisher.cs` — publish queue, confirms tracking, timeout monitoring, retry channel, backpressure, metrics
- `EventHandlerInvoker.cs` — deserialization, middleware pipeline, handler chain (sequential/concurrent), fallback,
  ack/nack, tracing
- `PersistentConnection.cs` — connection/channel lifecycle, reconnect orchestration, consumer channel management

## CONVENTIONS

- Events inherit `Event` base class (in RabbitBus.Core)
- Handlers implement `IEventHandler<TEvent>`
- Use fluent builder: `AddEvent<T>().WithHandler<H>().And()`
- Handler/middleware/fallback are Scoped lifetime (per-message DI scope)
- Handler execution modes: concurrent (default) or sequential (`SequentialHandlerExecution = true`)
- Handler ordering via `WithHandler<T>(order: N)` — lower values execute first
- `Channel<T>` for retry queue (not `ConcurrentQueue`)
- Polly pipelines for resilience: `PublishPipeline`, `ConnectionPipeline`, `HandlerPipeline`
- OpenTelemetry: ActivitySource `EasilyNET.RabbitBus`, auto trace context propagation via message headers
- Delayed message exchange plugin support removed (RabbitMQ deprecated it)

## ANTI-PATTERNS

- Blocking in event handlers
- Long-running sync operations in `HandleAsync`
- Not configuring `RetryCount` for critical events
- Using deprecated attribute-based configuration (use fluent builder)
- Relying on Singleton semantics in handlers (they are Scoped now)
