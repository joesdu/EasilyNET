# EASILYNET PROJECT KNOWLEDGE BASE

**Generated:** 2026-01-23  
**Commit:** e1212271  
**Branch:** dev

## OVERVIEW

Multi-target .NET library collection (`net8.0`/`net9.0`/`net10.0`) providing Core utilities, AutoDI modules, MongoDB/RabbitMQ integrations, and cryptography. Uses C# preview features, central package management, strong-name signing.

## STRUCTURE

```
EasilyNET/
├── src/                      # NuGet packages (10 projects)
│   ├── EasilyNET.Core/       # Primitives, extensions, threading
│   ├── EasilyNET.WebCore/    # JSON converters, middleware
│   ├── EasilyNET.AutoDependencyInjection[.Core]/  # Module system
│   ├── EasilyNET.RabbitBus.[Core|AspNetCore]/     # Event bus
│   ├── EasilyNET.Mongo.[Core|AspNetCore|ConsoleDebug]/  # MongoDB
│   └── EasilyNET.Security/   # Crypto (AES/SM2-4/RSA/RIPEMD)
├── sample/WebApi.Test.Unit/  # Integration sample (DI module orchestration)
├── test/                     # Unit tests + benchmarks
├── docs/                     # Additional documentation
└── .github/workflows/        # CI: build_test.yml, releaser.yml
```

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| Add new library | `src/` | Follow existing project structure |
| DI module example | `sample/WebApi.Test.Unit/AppWebModule.cs` | DependsOn ordering |
| JSON converters | `src/EasilyNET.WebCore/JsonConverters/` | DateOnly/TimeOnly/DateTime |
| RabbitMQ event handling | `src/EasilyNET.RabbitBus.AspNetCore/Manager/` | EventBus, EventPublisher |
| MongoDB serializers | `src/EasilyNET.Mongo.AspNetCore/Serializers/` | Custom BSON serializers |
| Crypto algorithms | `src/EasilyNET.Security/` | SM2/SM3/SM4, AES, RSA, RIPEMD |
| CI configuration | `.github/workflows/build_test.yml` | PR validation |
| Package versions | `src/Directory.Packages.props` | Central package management |

## CONVENTIONS

### Build System
- **DO NOT** set `TargetFramework`/`TargetFrameworks` in individual `.csproj` - enforced by `src/Directory.Build.targets`
- TFMs centralized in `src/Directory.Build.props`: `net8.0;net9.0;net10.0`
- Version derived from `EASILYNET_VERSION` env var or auto-generated timestamp
- Release builds: strong-name signed with `src/EasilyNET.snk`

### Coding
- C# `LangVersion: preview` - use primary constructors, collection expressions
- `Nullable: enable` - avoid `!` operator
- All public APIs require XML doc comments
- Naming: `PascalCase` public, `camelCase` params, `_camelCase` fields
- Async-first for I/O; accept `CancellationToken`; use `ConfigureAwait(false)`
- Configuration via `IOptions<T>` pattern

### Git
- Conventional Commits with emoji (see `gitemoji.md`)
- Format: `feat: ✨ ...`, `fix: 🐛 ...`, `refactor: ♻️ ...`
- Bilingual docs (中文 primary, English in `<details>`)

## ANTI-PATTERNS (THIS PROJECT)

- Setting TFM in individual csproj (build will fail)
- Using `Task.Run` for async I/O
- Empty catch blocks
- Suppressing nullability with `!`
- Committing without conventional commit format

## COMMANDS

```bash
# Build
dotnet build

# Test
dotnet test -c Debug --no-build
# OR
./Test.ps1

# Pack libraries
./Pack.ps1

# Sample API
dotnet watch run --project sample/WebApi.Test.Unit/WebApi.Test.Unit.csproj

# Infrastructure
docker compose -f docker-compose.basic.service.yml up -d  # Garnet, RabbitMQ, Aspire
docker compose -f docker-compose.mongo.rs.yml up -d       # MongoDB replica set
```

## NOTES

- Requires latest .NET SDK (preview features)
- `sample/WebApi.Test.Unit` demonstrates module orchestration via `DependsOn` attribute
- Each `src/` project has its own `README.md` with usage details
- Serilog + OpenTelemetry pipeline in sample for observability
