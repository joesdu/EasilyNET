# EASILYNET.AUTODEPENDENCYINJECTION - MODULE SYSTEM

## OVERVIEW

Modular DI system with `DependsOn` ordering, sync/async lifecycle hooks, and `IResolver` dynamic resolution. Depends on
`EasilyNET.Core` (for `TypeExtensions` scanning) and `EasilyNET.AutoDependencyInjection.Core` (for DI attributes).

## STRUCTURE

```
EasilyNET.AutoDependencyInjection/
├── Abstractions/   # IAppModule, IResolver, IIndex, IModuleDiagnostics interfaces
├── Attributes/     # DependsOnAttribute (module ordering)
├── Contexts/       # ConfigureServicesContext, ApplicationContext
├── Factories/      # Service factories
├── Modules/        # AppModule, DependencyAppModule, ModuleDiagnostics
├── Registry/       # ServiceRegistry, NamedServiceDescriptor, NamedServiceFactory
├── Resolver/       # Resolver, Parameter, Owned, OwnedFactory, KeyedServiceIndex, extensions
└── ObjectAccessor.cs
```

Note: `DependencyInjectionAttribute` and `IgnoreDependencyAttribute` live in the sibling
`EasilyNET.AutoDependencyInjection.Core` package (attributes-only, minimal deps).

## WHERE TO LOOK

| Task                    | Location                                                                              |
|-------------------------|---------------------------------------------------------------------------------------|
| Create new module       | Inherit `AppModule`, override `ConfigureServices`                                     |
| Module ordering example | `sample/WebApi.Test.Unit/AppWebModule.cs`                                             |
| Auto-registration logic | `Modules/DependencyAppModule.cs` (uses Core's `TypeExtensions`)                       |
| Keyed services          | `DependencyInjectionAttribute.ServiceKey` (in EasilyNET.AutoDependencyInjection.Core) |
| Dynamic resolution      | `Resolver/Resolver.cs`, `Resolver/ServiceProviderExtension.cs`                        |
| Parameter overrides     | `Resolver/Parameter.cs` (Named/Typed/Positional/Resolved)                             |
| Owned lifetime          | `Resolver/Owned.cs`, `Resolver/OwnedFactory.cs`                                       |
| Keyed index             | `Resolver/KeyedServiceIndex.cs`                                                       |
| Parameterized factories | `Resolver/ParameterizedFactoryExtensions.cs`                                          |
| Service registry        | `Registry/ServiceRegistry.cs`                                                         |
| Config-driven toggle    | Override `GetEnable()` method                                                         |
| Module diagnostics      | `IModuleDiagnostics` interface                                                        |

## CONVENTIONS

- Root module declares `[DependsOn(...)]` for all dependencies
- Module execution order = topological sort (dependencies first), not declaration order
- `ConfigureServices(context)` is **synchronous** — for DI registration only
- `ConfigureServicesAsync(context, ct)` is **async** — for rare async init scenarios
- `ApplicationInitializationSync(context)` is **synchronous** — called before the async version
- `ApplicationInitialization(context)` is **async** — for middleware/app configuration
- `ApplicationShutdown(context)` is **async** — called in reverse module order on app stop
- Use `context.Configuration` to access `IConfiguration` in ConfigureServices
- Use `context.GetApplicationHost()` and cast to `IApplicationBuilder` or `IHost`
- This project uses **attribute-driven DI**, NOT marker interfaces (`ITransientDependency` etc. do not exist)

## MODULE LIFECYCLE

```
1. ConfigureServices (sync, per module, dependency order)
2. ConfigureServicesAsync (async, per module)
3. Build ServiceProvider
4. ApplicationInitializationSync (sync, per module)
5. ApplicationInitialization (async, per module)
6. [app runs]
7. ApplicationShutdown (async, reverse order)
```

## ANTI-PATTERNS

- Circular module dependencies (throws `InvalidOperationException`)
- Using async operations in `ConfigureServices` (use `ConfigureServicesAsync` instead)
- Forgetting `DependencyAppModule` in root DependsOn (enables auto-registration)
- Using `context.GetApplicationBuilder()` (deprecated — use `GetApplicationHost()`)
- Blocking async code in `ApplicationInitialization`
