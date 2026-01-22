# SRC - LIBRARY DEVELOPMENT

## OVERVIEW

10 NuGet packages. Central package management. Multi-target `net8.0;net9.0;net10.0`.

## STRUCTURE

```
src/
├── Directory.Build.props     # TFMs, Release signing
├── Directory.Build.targets   # Blocks individual TFM settings
├── Directory.Packages.props  # Central package versions
├── EasilyNET.snk            # Strong-name key (Release only)
│
├── Core/                     # EasilyNET.Core + EasilyNET.WebCore
├── Framework/                # AutoDI, RabbitBus, Security
└── Mongo/                    # MongoDB suite (3 packages)
```

## WHERE TO LOOK

| Task | Location |
|------|----------|
| Add package reference | `Directory.Packages.props` |
| Change TFMs | `Directory.Build.props` (NOT individual csproj) |
| Core extensions | `EasilyNET.Core/Misc/` |
| Threading utilities | `EasilyNET.Core/Threading/` |
| WebSocket client | `EasilyNET.Core/WebSocket/` |
| WebSocket server | `EasilyNET.WebCore/WebSocket/` |
| JSON converters | `EasilyNET.WebCore/JsonConverters/` |

## CONVENTIONS

- Every package has `README.md` at root - keep updated
- XML docs required for all `public` members
- Use `internal` for implementation details
- Prefer `file`-scoped types for helpers
- Each package should be independently usable

## ANTI-PATTERNS

- Adding `<TargetFramework>` to any csproj - build enforcer will fail
- Cross-package dependencies without explicit reason
- Breaking backward compatibility without version bump
