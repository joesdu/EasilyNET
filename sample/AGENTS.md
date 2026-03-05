# SAMPLE - INTEGRATION EXAMPLE

## OVERVIEW

Full-stack ASP.NET Core sample demonstrating DI modules, MongoDB, RabbitBus, WebSocket, and middleware orchestration.

## STRUCTURE

```
WebApi.Test.Unit/
├── AppWebModule.cs           # Root module with DependsOn ordering
├── DbContext.cs              # MongoDB context
├── Controllers/              # API controllers
├── Domain/                   # Entity models
├── Events/                   # RabbitBus events
├── EventHandlers/            # RabbitBus handlers
├── Middleware/               # Custom middleware
└── Common/                   # Shared utilities
```

## WHERE TO LOOK

| Task                 | Location                                           |
| -------------------- | -------------------------------------------------- |
| Module orchestration | `AppWebModule.cs` - DependsOn ordering             |
| MongoDB integration  | `DbContext.cs`, Controllers/MongoTestController.cs |
| RabbitBus events     | `Events/`, EventHandlers/                          |
| DI resolution        | Controllers/ResolveController.cs                   |

## CONVENTIONS

- Modules use `[DependsOn(...)]` for explicit ordering
- Controllers inject services via constructor
- Events inherit `Event` base class
- Uses Serilog + OpenTelemetry for observability

## COMMANDS

```bash
dotnet watch run --project sample/WebApi.Test.Unit/WebApi.Test.Unit.csproj
```
