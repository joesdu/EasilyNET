# EASILYNET.CORE - PRIMITIVES & UTILITIES

## OVERVIEW

High-performance .NET primitives library. Zero external dependencies. Performance-sensitive.

## STRUCTURE

```
EasilyNET.Core/
├── Aggregator/     # Simple event pub/sub
├── Coordinate/     # GPS coordinate conversion (BD09/GCJ02/WGS84)
├── Enums/          # Common enums (Gender, Nation, Zodiac)
├── Essentials/     # Core types (Ulid, ObjectIdCompat, BusinessException)
├── IDCard/         # China ID card validation
├── InfoItems/      # Business DTOs (IdNameItem, Operator)
├── IO/             # Compression helpers
├── Language/       # C# syntax sugar (Range operators)
├── Misc/           # Extension methods (16 files)
├── Numerics/       # BigNumber operations
├── PageResult/     # Pagination helpers
├── Threading/      # AsyncLock, AsyncBarrier
└── WebSocket/      # Managed WebSocket client
```

## WHERE TO LOOK

| Task | Location |
|------|----------|
| Add extension method | `Misc/` - find matching `*Extensions.cs` |
| DateTime utilities | `Misc/DateTimeExtensions.cs` |
| String manipulation | `Misc/StringExtensions.cs` |
| Collection helpers | `Misc/IEnumerableExtensions.cs` |
| Deep copy | `Misc/ObjectExtensions.cs` (expression tree based) |
| Async primitives | `Threading/` |

## CONVENTIONS

- Extension methods in `Misc/` folder, grouped by type
- All types in `EasilyNET.Core` namespace (flat)
- Prefer `Span<T>` and `Memory<T>` for hot paths
- Use `PooledMemoryStream` instead of `MemoryStream`
- Accept `CancellationToken` for async methods

## ANTI-PATTERNS

- Adding external package dependencies (keep it minimal)
- Blocking async code (`Task.Result`, `.Wait()`)
- Large allocations in extension methods
