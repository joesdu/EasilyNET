# EASILYNET.AUTODEPENDENCYINJECTION - MODULE SYSTEM

## OVERVIEW

Modular DI system with `DependsOn` ordering, sync/async lifecycle hooks, and `IResolver` dynamic resolution.

## STRUCTURE

```
EasilyNET.AutoDependencyInjection/
├── Abstractions/   # IAppModule, IResolver, IIndex, IModuleDiagnostics interfaces
├── Attributes/     # DependencyInjectionAttribute, DependsOnAttribute
├── Contexts/       # ConfigureServicesContext, ApplicationContext
├── Factories/      # Service factories
├── Modules/        # AppModule, DependencyAppModule, ModuleDiagnostics
├── Registry/       # ServiceRegistry, NamedServiceDescriptor, NamedServiceFactory
├── Resolver/       # Resolver, Parameter, Owned, OwnedFactory, KeyedServiceIndex, extensions
└── ObjectAccessor.cs
```

## WHERE TO LOOK

| Task | Location |
|------|----------|
| Create new module | Inherit `AppModule`, override `ConfigureServices` |
| Module ordering example | `sample/WebApi.Test.Unit/AppWebModule.cs` |
| Keyed services | `DependencyInjectionAttribute.ServiceKey` |
| Dynamic resolution | `Resolver/Resolver.cs`, `Resolver/ServiceProviderExtension.cs` |
| Parameter overrides | `Resolver/Parameter.cs` (Named/Typed/Positional/Resolved) |
| Owned lifetime | `Resolver/Owned.cs`, `Resolver/OwnedFactory.cs` |
| Keyed index | `Resolver/KeyedServiceIndex.cs` |
| Parameterized factories | `Resolver/ParameterizedFactoryExtensions.cs` |
| Service registry | `Registry/ServiceRegistry.cs` |
| Config-driven toggle | Override `GetEnable()` method |
| Module diagnostics | `IModuleDiagnostics` interface |

## CONVENTIONS

- Root module declares `[DependsOn(...)]` for all dependencies
- Module execution order = DependsOn declaration order (dependencies first)
- `ConfigureServices(context)` is **synchronous** - for DI registration only
- `ConfigureServicesAsync(context, ct)` is **async** - for rare async init scenarios
- `ApplicationInitializationSync(context)` is **synchronous** - called before the async version
- `ApplicationInitialization(context)` is **async** - for middleware/app configuration
- `ApplicationShutdown(context)` is **async** - called in reverse module order on app stop
- Use `context.Configuration` to access `IConfiguration` in ConfigureServices
- Use `context.GetApplicationHost()` and cast to `IApplicationBuilder` or `IHost`

## API DESIGN

```csharp
public interface IAppModule
{
    // Sync: Service registration (99% of cases)
    void ConfigureServices(ConfigureServicesContext context);
    
    // Async: Rare async initialization during registration phase
    Task ConfigureServicesAsync(ConfigureServicesContext context, CancellationToken ct);
    
    // Sync: Application initialization, called before the async version
    void ApplicationInitializationSync(ApplicationContext context);
    
    // Async: Application initialization (middleware, etc.)
    Task ApplicationInitialization(ApplicationContext context);
    
    // Async: Application shutdown, called in reverse module order
    Task ApplicationShutdown(ApplicationContext context);
    
    // Config-driven enable/disable
    bool GetEnable(ConfigureServicesContext context);
}
```

## ANTI-PATTERNS

- Circular module dependencies (throws `InvalidOperationException`)
- Using async operations in `ConfigureServices` (use `ConfigureServicesAsync` instead)
- Forgetting `DependencyAppModule` in root DependsOn (enables auto-registration)
- Using `context.GetApplicationBuilder()` (deprecated)
- Blocking async code in `ApplicationInitialization`
