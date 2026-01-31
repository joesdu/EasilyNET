# EASILYNET.MONGO.ASPNETCORE - MONGODB INTEGRATION

## OVERVIEW

MongoDB driver wrapper with auto-mapping, custom serializers, index attributes, and resilience options.

## STRUCTURE

```
EasilyNET.Mongo.AspNetCore/
├── Common/             # Shared utilities (Constant)
├── Conventions/        # BSON mapping conventions
├── Helpers/            # Service extension helpers
├── JsonConverters/     # System.Text.Json converters (BsonDocument)
├── Options/            # ClientOptions, MongoResilienceOptions
├── Serializers/        # DateOnly, TimeOnly, JsonNode, EnumKeyDictionary serializers
├── CollectionIndexExtensions.cs      # Attribute-based indexing
├── MongoServiceExtensions.cs         # AddMongoContext registration
├── SerializersCollectionExtensions.cs # Serializer registration helpers
└── TimeSeriesCollectionExtensions.cs # Time series collection support
```

## WHERE TO LOOK

| Task | Location |
|------|----------|
| Add serializer | `Serializers/`, register via `RegisterSerializer()` |
| Configure client | `Options/ClientOptions.cs` |
| Resilience settings | `Options/MongoResilienceOptions.cs` |
| Custom conventions | `Conventions/` |
| Index creation | `CollectionIndexExtensions.cs` |
| Time series collections | `TimeSeriesCollectionExtensions.cs` |

## CONVENTIONS

- Use `AddMongoContext<T>()` for registration
- `DefaultConventionRegistry = true` enables camelCase, Id mapping, enum-as-string
- Resilience options: `c.Resilience.Enable = true` for recommended defaults
- Serializers registered globally: one per type

## ANTI-PATTERNS

- Multiple serializers for same type (conflicts)
- Setting `MinConnectionPoolSize` too high (connection pool exhaustion)
- Missing `directConnection=true` for single-node/proxy access
- Not setting timeouts in connection string
