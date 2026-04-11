# EASILYNET PROJECT KNOWLEDGE BASE

**Generated:** 2026-04-12  
**Commit:** 93b8d236  
**Branch:** dev

## OVERVIEW

Multi-target .NET library collection (`net10.0`/`net11.0`) providing Core utilities, AutoDI modules, MongoDB/RabbitMQ integrations, and cryptography. Uses C# preview features, central package management, strong-name signing.

## STRUCTURE

```
EasilyNET/
├── src/                      # NuGet packages (10 projects)
│   ├── EasilyNET.Core/       # Primitives, extensions, threading (zero deps)
│   ├── EasilyNET.WebCore/    # JSON converters, middleware, WebSocket server
│   ├── EasilyNET.AutoDependencyInjection/      # Module system + IResolver
│   ├── EasilyNET.AutoDependencyInjection.Core/  # DI attributes only
│   ├── EasilyNET.RabbitBus.Core/               # Event bus abstractions
│   ├── EasilyNET.RabbitBus.AspNetCore/          # RabbitMQ event bus impl
│   ├── EasilyNET.Mongo.Core/                   # MongoContext, attributes, enums
│   ├── EasilyNET.Mongo.AspNetCore/             # MongoDB driver wrapper + serializers
│   ├── EasilyNET.Mongo.ConsoleDebug/           # Mongo diagnostics (Spectre.Console)
│   └── EasilyNET.Security/   # Crypto (AES/SM2-4/RSA/RIPEMD/DES/RC4)
├── sample/WebApi.Test.Unit/  # Integration sample (DI module orchestration)
├── test/                     # Unit tests (MSTest, single project)
│   └── EasilyNET.Test.Unit/  # Covers Core, Security, AutoDI
├── docs/                     # Design docs (GridFS, PooledMemoryStream, EventAggregator)
└── .github/workflows/        # CI: build_test.yml, releaser.yml, PSScriptAnalyzer.yml
```

## DEPENDENCY GRAPH

```
EasilyNET.Core (root - zero external deps)
├── EasilyNET.WebCore
├── EasilyNET.AutoDependencyInjection (+ AutoDependencyInjection.Core)
├── EasilyNET.RabbitBus.Core → EasilyNET.RabbitBus.AspNetCore
├── EasilyNET.Mongo.Core → EasilyNET.Mongo.AspNetCore
└── (no dep) EasilyNET.Security, EasilyNET.Mongo.ConsoleDebug
```

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| Add new library | `src/` | Follow existing project structure, no TFM in csproj |
| DI module example | `sample/WebApi.Test.Unit/AppWebModule.cs` | DependsOn ordering |
| JSON converters | `src/EasilyNET.WebCore/JsonConverters/` | DateOnly/TimeOnly/DateTime/Decimal/Bool |
| RabbitMQ event handling | `src/EasilyNET.RabbitBus.AspNetCore/Manager/` | EventBus, EventPublisher, EventHandlerInvoker |
| MongoDB serializers | `src/EasilyNET.Mongo.AspNetCore/Serializers/` | Custom BSON serializers |
| MongoDB indexing | `src/EasilyNET.Mongo.AspNetCore/Indexing/` | Attribute-based index creation |
| Atlas Search/Vector | `src/EasilyNET.Mongo.AspNetCore/SearchIndex/` | Search index definition + manager |
| Crypto algorithms | `src/EasilyNET.Security/` | SM2/SM3/SM4, AES, RSA, RIPEMD, DES, RC4 |
| CI configuration | `.github/workflows/build_test.yml` | PR validation (ubuntu, pwsh) |
| Release pipeline | `.github/workflows/releaser.yml` | Tag-triggered NuGet push |
| Package versions | `src/Directory.Packages.props` | Central package management |
| Build constraints | `src/Directory.Build.targets` | TFM guard (errors if csproj sets TFM) |
| Design documents | `docs/` | GridFS, PooledMemoryStream, EventAggregator |

## CONVENTIONS

### Build System
- **DO NOT** set `TargetFramework`/`TargetFrameworks` in individual `.csproj` — enforced by `src/Directory.Build.targets` (build error)
- TFMs centralized in `src/Directory.Build.props`: `net10.0;net11.0`
- `net11.0` enables `EnablePreviewFeatures=true` and `runtime-async=on`
- Version: `EASILYNET_VERSION` env var → regex-validated → fallback to timestamp `6.YY.MMdd.HHm`
- Release builds: strong-name signed with `src/EasilyNET.snk`
- No `global.json` — relies on latest preview SDK

### Two-Layer Directory.Build.props

| File | Purpose |
|------|---------|
| `Directory.Build.props` (root) | Authors, RepositoryUrl, LangVersion, Version, Nullable, ImplicitUsings, PackageReadmeFile |
| `src/Directory.Build.props` | TargetFrameworks, Release signing, SourceLink, ContinuousIntegrationBuild |

The `src/Directory.Build.props` imports root via:
```xml
<Import Project="$([MSBuild]::GetPathOfFileAbove('Directory.Build.props', '$(MSBuildThisFileDirectory)../'))" />
```

### Coding
- C# `LangVersion: preview` — use primary constructors, collection expressions, file-scoped types
- `Nullable: enable` — avoid `!` operator
- All public APIs in `src/` require XML doc comments (`GenerateDocumentationFile=True`)
- Naming: `PascalCase` public, `camelCase` params, `_camelCase` fields
- Async-first for I/O; accept `CancellationToken`; use `ConfigureAwait(false)`
- Configuration via `IOptions<T>` pattern
- Extension methods in `Microsoft.Extensions.DependencyInjection` namespace

### Testing
- MSTest only (`[TestClass]`, `[TestMethod]`, `[TestInitialize]`)
- Assembly-level parallelization: `[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]`
- Method naming: `Method_WhenCondition_ShouldResult`
- No mocking framework — prefer real DI wiring + in-file fake classes
- Test project covers Core, Security, AutoDI only (Mongo/RabbitBus/WebCore untested in `test/`)

### Git
- Conventional Commits with emoji (see `gitemoji.md`)
- Format: `feat: ✨ ...`, `fix: 🐛 ...`, `refactor: ♻️ ...`
- Bilingual docs (中文 primary, English in `<details>`)

## ANTI-PATTERNS (THIS PROJECT)

- Setting TFM in individual csproj (build error via Directory.Build.targets)
- Using `Task.Run` for async I/O
- Empty catch blocks
- Suppressing nullability with `!`
- Committing without conventional commit format
- Adding external deps to EasilyNET.Core (must stay zero-dep)
- Blocking async code (`Task.Result`, `.Wait()`)

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

- Requires latest .NET SDK (preview features, no global.json pinning)
- CI runs on ubuntu-latest with pwsh shell, installs both .NET 10 and 11 preview SDKs
- Release triggered by git tag `*.*.*` → Pack.ps1 → Push.ps1 to NuGet (NUGET_ENV environment)
- `sample/WebApi.Test.Unit` demonstrates module orchestration via `DependsOn` attribute
- Each `src/` project has its own `README.md` with usage details
- Serilog + OpenTelemetry pipeline in sample for observability
- Key external deps: MongoDB.Driver 3.7.1, RabbitMQ.Client 7.2.1, BouncyCastle 2.7.0-beta, Polly (via Microsoft.Extensions.Resilience)
