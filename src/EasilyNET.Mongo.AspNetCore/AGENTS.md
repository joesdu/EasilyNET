# EASILYNET.MONGO.ASPNETCORE - MONGODB INTEGRATION

## OVERVIEW

MongoDB driver wrapper with auto-mapping, custom serializers, index attributes, GridFS, and resilience options.

## STRUCTURE

```
EasilyNET.Mongo.AspNetCore/
├── Abstraction/        # IMongoContext interface
├── BackgroundServices/ # Index creation, session cleanup
├── Common/             # Shared utilities
├── Controllers/        # GridFS REST API controller
├── Conventions/        # BSON mapping conventions
├── Converters/         # Type converters
├── Factories/          # Client/context factories
├── Helpers/            # GridFS utilities
├── JsonConverters/     # System.Text.Json converters
├── Models/             # GridFS models
├── Options/            # ClientOptions, MongoResilienceOptions
├── Serializers/        # DateOnly, TimeOnly, JsonNode, Dynamic serializers
└── CollectionIndexExtensions.cs  # Attribute-based indexing (48k lines)
```

## WHERE TO LOOK

| Task | Location |
|------|----------|
| Add serializer | `Serializers/`, register via `RegisterSerializer()` |
| Configure client | `Options/ClientOptions.cs` |
| Resilience settings | `Options/MongoResilienceOptions.cs` |
| GridFS upload/download | `Controllers/GridFsController.cs` |
| Custom conventions | `Conventions/` |
| Index creation | `CollectionIndexExtensions.cs` |

## CONVENTIONS

- Use `AddMongoContext<T>()` for registration
- `DefaultConventionRegistry = true` enables camelCase, Id mapping, enum-as-string
- Resilience options: `c.Resilience.Enable = true` for recommended defaults
- Serializers registered globally: one per type
- GridFS: 255KB chunk size default, optimized for streaming

## ANTI-PATTERNS

- Multiple serializers for same type (conflicts)
- Setting `MinConnectionPoolSize` too high (connection pool exhaustion)
- Missing `directConnection=true` for single-node/proxy access
- Not setting timeouts in connection string
