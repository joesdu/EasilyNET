# EASILYNET.AUTODEPENDENCYINJECTION - MODULE SYSTEM

## OVERVIEW

Modular DI system with `DependsOn` ordering, async lifecycle hooks, and `IResolver` dynamic resolution.

## STRUCTURE

```
EasilyNET.AutoDependencyInjection/
├── Abstractions/   # IAppModule, IResolver interfaces
├── Attributes/     # DependencyInjectionAttribute, DependsOnAttribute
├── Contexts/       # ConfigureServicesContext, ApplicationContext
├── Factories/      # Service factories
├── Modules/        # AppModule, DependencyAppModule base classes
├── Resolver.cs     # Dynamic service resolver (Autofac-like)
└── ServiceRegistry.cs  # Auto-registration logic
```

## WHERE TO LOOK

| Task | Location |
|------|----------|
| Create new module | Inherit `AppModule`, override `ConfigureServices` |
| Module ordering example | `sample/WebApi.Test.Unit/AppWebModule.cs` |
| Keyed services | `DependencyInjectionAttribute.ServiceKey` |
| Dynamic resolution | `Resolver.cs`, `ServiceProviderExtension.cs` |
| Config-driven toggle | Override `GetEnable()` method |

## CONVENTIONS

- Root module declares `[DependsOn(...)]` for all dependencies
- Module execution order = DependsOn declaration order
- `ConfigureServices` for DI registration
- `ApplicationInitialization` for middleware/app configuration
- Both methods are `async Task`
- Use `context.GetApplicationHost()` and cast to `IApplicationBuilder` or `IHost`

## ANTI-PATTERNS

- Circular module dependencies
- Synchronous blocking in async hooks
- Forgetting `DependencyAppModule` in root DependsOn (enables auto-registration)
- Using `context.GetApplicationBuilder()` (deprecated)
