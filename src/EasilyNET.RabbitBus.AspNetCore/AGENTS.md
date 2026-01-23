# EASILYNET.RABBITBUS.ASPNETCORE - EVENT BUS

## OVERVIEW

RabbitMQ event bus with publisher confirms, Channel<T> retry queue, Polly resilience, and dead letter store.

## STRUCTURE

```
EasilyNET.RabbitBus.AspNetCore/
├── Abstractions/   # IDeadLetterStore (public), internal interfaces
├── Builder/        # Fluent configuration API
├── Configs/        # Event/handler configuration models
├── Health/         # Health check integration
├── Manager/        # EventBus, EventPublisher, EventHandlerInvoker, PersistentConnection
├── Metrics/        # System.Diagnostics.Metrics instrumentation
├── Serializer/     # IBusSerializer, JSON default
├── Services/       # MessageConfirmService (background retry)
├── Stores/         # InMemoryDeadLetterStore
└── Utilities/      # Helpers
```

## WHERE TO LOOK

| Task | Location |
|------|----------|
| Add event configuration | `AddEvent<T>()` in `RabbitServiceExtension.cs` |
| Modify connection logic | `Manager/PersistentConnection.cs` |
| Change retry behavior | `Manager/EventPublisher.cs` (Channel<T> queue) |
| Handler execution | `Manager/EventHandlerInvoker.cs` |
| Custom serializer | Implement `IBusSerializer`, use `WithSerializer<T>()` |
| Custom dead letter store | Implement `IDeadLetterStore`, register as singleton |

## CONVENTIONS

- Events inherit `Event` base class
- Handlers implement `IEventHandler<TEvent>`
- Use fluent builder: `AddEvent<T>().WithHandler<H>().And()`
- Handler execution modes: concurrent (default) or sequential (`SequentialHandlerExecution = true`)
- `Channel<T>` for retry queue (not `ConcurrentQueue`)
- Polly pipelines for resilience: `PublishPipeline`, `ConnectionPipeline`, `HandlerPipeline`

## ANTI-PATTERNS

- Blocking in event handlers
- Long-running sync operations in `HandleAsync`
- Not configuring `RetryCount` for critical events
- Using deprecated attribute-based configuration
