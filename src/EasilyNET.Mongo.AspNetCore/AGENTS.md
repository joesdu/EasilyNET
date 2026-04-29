# EASILYNET.MONGO.ASPNETCORE - MONGODB INTEGRATION

## OVERVIEW

MongoDB driver wrapper for ASP.NET Core with auto-mapping, custom serializers, attribute-based indexing, change streams,
GridFS, health checks, Atlas Search/Vector Search, resilience options, and time series/capped collection support.
Depends on `EasilyNET.Core` and `EasilyNET.Mongo.Core`. Key dep: MongoDB.Driver 3.7.1.

## STRUCTURE

```
EasilyNET.Mongo.AspNetCore/
├── ChangeStreams/       # MongoChangeStreamHandler<T> base class (background service)
├── Common/             # Shared utilities (Constant)
├── Conventions/        # BSON mapping conventions
├── Extensions/         # All IServiceCollection/IApplicationBuilder extension methods
│   ├── CappedCollectionExtensions.cs     # Auto-create capped collections
│   ├── ChangeStreamServiceExtensions.cs  # Register change stream handlers
│   ├── CollectionIndexExtensions.cs      # Attribute-based index creation
│   ├── GridFSServiceExtensions.cs        # GridFS bucket registration (keyed services)
│   ├── MongoHealthCheckExtensions.cs     # MongoDB health check
│   ├── MongoServiceExtensions.cs         # AddMongoContext registration (3 overloads)
│   ├── SearchIndexExtensions.cs          # Atlas Search/Vector index creation
│   ├── SerializersCollectionExtensions.cs # Serializer registration helpers
│   └── TimeSeriesCollectionExtensions.cs # Time series collection support
├── HealthChecks/       # MongoDB health check implementation (ping-based)
├── Helpers/            # Service extension internal helpers
├── Indexing/           # Index definition, factory, field collector, manager
├── JsonConverters/     # System.Text.Json converters (BsonDocument)
├── Options/            # ClientOptions, MongoResilienceOptions, GridFSOptions, ChangeStreamHandlerOptions, MongoConventionOptions
├── SearchIndex/        # Search index definition factory and manager
└── Serializers/        # DateOnly, TimeOnly, JsonNode, JsonObject, EnumKeyDictionary serializers
```

Sibling `EasilyNET.Mongo.Core` provides: `MongoContext` base class, index/collection/search attributes, enums, geo
queries, bulk write extensions.

## WHERE TO LOOK

| Task                    | Location                                                                                                   |
|-------------------------|------------------------------------------------------------------------------------------------------------|
| Register MongoContext   | `Extensions/MongoServiceExtensions.cs` (3 overloads: IConfiguration, string, MongoClientSettings)          |
| Add serializer          | `Serializers/`, register via `Extensions/SerializersCollectionExtensions.cs`                               |
| Configure client        | `Options/ClientOptions.cs`, `Options/BasicClientOptions.cs`                                                |
| Configure conventions   | `Options/MongoConventionOptions.cs` via `ConfigureMongoConventions()` — must call before `AddMongoContext` |
| Resilience settings     | `Options/MongoResilienceOptions.cs`                                                                        |
| Custom conventions      | `Conventions/`                                                                                             |
| Index creation          | `Extensions/CollectionIndexExtensions.cs` + `Indexing/`                                                    |
| Time series collections | `Extensions/TimeSeriesCollectionExtensions.cs`                                                             |
| Capped collections      | `Extensions/CappedCollectionExtensions.cs`                                                                 |
| Change streams          | `ChangeStreams/MongoChangeStreamHandler.cs` + `Extensions/ChangeStreamServiceExtensions.cs`                |
| GridFS                  | `Extensions/GridFSServiceExtensions.cs` (supports keyed services for multi-bucket)                         |
| Health checks           | `Extensions/MongoHealthCheckExtensions.cs`, `HealthChecks/MongoHealthCheck.cs`                             |
| Atlas Search indexes    | `Extensions/SearchIndexExtensions.cs`, `SearchIndex/`                                                      |

## CONVENTIONS

- Use `AddMongoContext<T>()` for registration
- Default conventions (camelCase, Id mapping, enum-as-string, ignore extra elements, DateTime local, Decimal128) applied
  automatically unless `ConfigureMongoConventions` is called
- `ConfigureMongoConventions` replaces defaults — only user-added conventions via `AddConvention()` are registered
- Convention configuration is global and idempotent — at most once, before any `AddMongoContext` call
- Resilience options: `c.Resilience.Enable = true` for recommended defaults
- Serializers registered globally: one per type (DateOnly/TimeOnly support String or Ticks, not both)
- All extension methods are in `Microsoft.Extensions.DependencyInjection` namespace
- `Use*` methods are `IApplicationBuilder` extensions called in `Program.cs` after `Build()`
- `IMongoClient`/`IMongoDatabase` are NOT registered in DI — access via `MongoContext.Client`/`.Database`
- Index management: safe by default (create-only), opt-in `DropUnmanagedIndexes` with `ProtectedIndexPrefixes`

## ANTI-PATTERNS

- Multiple serializers for same type (conflicts)
- Setting `MinConnectionPoolSize` too high (connection pool exhaustion)
- Missing `directConnection=true` for single-node/proxy access
- Not setting timeouts in connection string
- Using change streams without replica set or sharded cluster
- Injecting `IMongoClient`/`IMongoDatabase` directly (removed — use MongoContext)
