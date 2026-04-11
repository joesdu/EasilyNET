# SAMPLE - INTEGRATION EXAMPLE

## OVERVIEW

Full-stack ASP.NET Core sample demonstrating DI module orchestration, MongoDB (dual context), RabbitBus events, WebSocket, Garnet cache, and observability pipeline.

## STRUCTURE

```
WebApi.Test.Unit/
├── AppWebModule.cs           # Root module with DependsOn ordering (shows full module chain)
├── DbContext.cs              # MongoDB context (primary)
├── DbContext2.cs             # MongoDB context (secondary — demonstrates multi-context)
├── Controllers/              # API controllers (7 files)
├── Domain/                   # Entity models
├── Events/                   # RabbitBus events
├── EventHandlers/            # RabbitBus handlers
├── Middleware/               # Custom middleware
├── ServiceModules/           # Feature modules (9 files — Mongo, Rabbit, CORS, Swagger, etc.)
└── Common/                   # Shared utilities (Constant)
```

## WHERE TO LOOK

| Task | Location |
|------|----------|
| Module orchestration | `AppWebModule.cs` — DependsOn ordering, exception handler, auth middleware |
| Feature module examples | `ServiceModules/` — MongoModule, RabbitModule, CorsModule, etc. |
| MongoDB integration | `DbContext.cs`, `DbContext2.cs`, `ServiceModules/MongoModule.cs` |
| RabbitBus events | `Events/`, `EventHandlers/`, `ServiceModules/RabbitModule.cs` |
| DI resolution | `Controllers/ResolveController.cs` |
| Distributed cache | `ServiceModules/GarnetDistributedCacheModule.cs` |

## CONVENTIONS

- Modules use `[DependsOn(...)]` for explicit ordering — dependencies first
- `DependencyAppModule` must be in root DependsOn for auto-registration to work
- Controllers inject services via primary constructor
- Events inherit `Event` base class
- Uses Serilog + OpenTelemetry for observability (configured in modules)
- Two MongoContexts demonstrate multi-database pattern

## COMMANDS

```bash
# Requires infrastructure: docker compose -f docker-compose.basic.service.yml up -d
dotnet watch run --project sample/WebApi.Test.Unit/WebApi.Test.Unit.csproj
```
