# TEST - UNIT TESTS & BENCHMARKS

## OVERVIEW

xUnit test projects and benchmarks for EasilyNET libraries.

## STRUCTURE

```
test/
├── EasilyNET.Test.Unit/       # Unit tests
│   ├── AutoDependencyInjection/
│   ├── DeepCopy/
│   ├── Security ...
└/
│   └──── EasilyNET.Core.Benchmark/  # Benchmarks (BenchmarkDotNet)
    ├── UlidBenchmark.cs
    ├── PooledMemoryStreamBenchmark.cs
    └── ...
```

## WHERE TO LOOK

| Task           | Location                    |
| -------------- | --------------------------- |
| DI tests       | `AutoDependencyInjection/`  |
| DeepCopy tests | `DeepCopy/`                 |
| Security tests | `Security/`                 |
| Benchmarks     | `EasilyNET.Core.Benchmark/` |

## CONVENTIONS

- Tests use xUnit with FluentAssertions
- Test classes: `*Tests.cs` or `*Test.cs`
- BenchmarkDotNet for performance testing
- Tests mirror src/ module structure

## ANTI-PATTERNS

- Deleting failing tests to "pass"
- Empty test methods
- Testing internal implementation details
