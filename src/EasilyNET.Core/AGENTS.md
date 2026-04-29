# EASILYNET.CORE - PRIMITIVES & UTILITIES

## OVERVIEW

High-performance .NET primitives library. Zero external dependencies. Performance-sensitive. Root of the dependency
graph — 5 other src/ projects depend on this.

## STRUCTURE

```
EasilyNET.Core/
├── Aggregator/     # Simple event pub/sub (SimpleEventAggregator)
├── Attributes/     # AttributeBase (shared base for custom attributes)
├── Coordinate/     # GPS coordinate conversion (BD09/GCJ02/WGS84)
├── DeepCopy/       # Expression-tree deep copy engine
├── Enums/          # Common enums (Gender, Nation, Zodiac, CamelCase)
├── Essentials/     # Core types (Ulid, ObjectIdCompat, BusinessException, PooledMemoryStream, SharedDateTime)
├── IDCard/         # China ID card validation (15/18 digit)
├── InfoItems/      # Business DTOs (IdNameItem, Operator, ReferenceItem)
├── IO/             # Compression helpers (zip/deflate)
├── Language/       # C# syntax sugar (Range operators via `..`)
├── Misc/           # Extension methods (16 files) — most cross-project shared code lives here
├── Numerics/       # BigNumber (rational arithmetic)
├── PageResult/     # Pagination helpers (PageResult<T>, PageInfo)
├── Threading/      # AsyncLock, AsyncBarrier, AsyncReaderWriterLock
└── WebSocket/      # ManagedWebSocketClient (auto-reconnect, heartbeat, send queue)
```

## WHERE TO LOOK

| Task                    | Location                                                         |
|-------------------------|------------------------------------------------------------------|
| Add extension method    | `Misc/` - find matching `*Extensions.cs`                         |
| DateTime utilities      | `Misc/DateTimeExtensions.cs`                                     |
| String manipulation     | `Misc/StringExtensions.cs` (776 lines)                           |
| Collection helpers      | `Misc/IEnumerableExtensions.cs` (949 lines)                      |
| Type/reflection helpers | `Misc/TypeExtensions.cs` (used by AutoDI for scanning)           |
| Deep copy               | `Misc/ObjectExtensions.cs` → `DeepCopy/` (expression tree based) |
| Assembly scanning       | `Misc/AssemblyHelper.cs` (766 lines, reflection + caching)       |
| Console output helpers  | `Misc/TextWriterExtensions.cs` (860 lines, ANSI/progress)        |
| Async primitives        | `Threading/`                                                     |
| WebSocket client        | `WebSocket/ManagedWebSocketClient.cs` (1087 lines)               |
| Pooled stream           | `Essentials/PooledMemoryStream.cs` (729 lines)                   |
| ULID generation         | `Essentials/Ulid.cs` (1186 lines)                                |

## COMPLEXITY HOTSPOTS

These files have genuine concurrency/state-machine complexity (not just long):

- `WebSocket/ManagedWebSocketClient.cs` — connect/reconnect state machine, send queue, heartbeat, disposal
- `Threading/AsyncReaderWriterLock.cs` — packed state bits, wait queues, cancellation, fairness
- `Misc/AssemblyHelper.cs` — reflection + caching + parallel scanning + configurable filtering

## CONVENTIONS

- Extension methods in `Misc/` folder, grouped by target type
- All types in `EasilyNET.Core` namespace (flat, no sub-namespaces)
- Prefer `Span<T>` and `Memory<T>` for hot paths
- Use `PooledMemoryStream` instead of `MemoryStream`
- Accept `CancellationToken` for async methods
- `TypeExtensions.IsBaseOn` / `IsDeriveClassFrom` are used by AutoDI for type scanning

## ANTI-PATTERNS

- Adding external package dependencies (MUST stay zero-dep)
- Blocking async code (`Task.Result`, `.Wait()`)
- Large allocations in extension methods
- Using `MemoryStream` where `PooledMemoryStream` fits
