# Contributing to EasilyNET

Thank you for your interest in contributing to EasilyNET! We welcome contributions from the community. This document provides guidelines and information to help you get started.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Environment Setup](#development-environment-setup)
- [Project Structure](#project-structure)
- [Coding Standards](#coding-standards)
- [Testing](#testing)
- [Submitting Changes](#submitting-changes)
- [Reporting Issues](#reporting-issues)
- [License](#license)

## Code of Conduct

This project adheres to the [Contributor Covenant Code of Conduct](CODE_OF_CONDUCT.md). By participating, you are expected to uphold this code. Please report unacceptable behavior to [dygood@outlook.com](mailto:dygood@outlook.com).

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet) (latest version recommended)
- [Git](https://git-scm.com/)
- Optional: [Docker](https://www.docker.com/) for running services like MongoDB, RabbitMQ, etc.

### Fork and Clone

1. Fork the repository on GitHub.
2. Clone your fork locally:
   ```bash
   git clone https://github.com/YOUR_USERNAME/EasilyNET.git
   cd EasilyNET
   ```
3. Add the upstream remote:
   ```bash
   git remote add upstream https://github.com/joesdu/EasilyNET.git
   ```

### Branching

- Create a new branch for your changes:
  ```bash
  git checkout -b feature/your-feature-name
  ```
- Use descriptive branch names (e.g., `feature/add-new-encryption-method`, `fix/bug-in-mongo-driver`).

## Development Environment Setup

1. Ensure you have the latest .NET 10 SDK installed.
2. Restore dependencies:
   ```bash
   dotnet restore
   ```
3. Build the solution:
   ```bash
   dotnet build
   ```
4. For testing with external services, start the required containers:

   ```bash
   # For MongoDB replica set
   docker compose -f docker-compose.mongo.rs.yml up -d

   # For basic services (Garnet, RabbitMQ, Aspire Dashboard)
   docker compose -f docker-compose.basic.service.yml up -d
   ```

### Running the Sample Application

The sample application is located in `sample/WebApi.Test.Unit/`. To run it:

```bash
cd sample/WebApi.Test.Unit
dotnet run
```

## Project Structure

- `src/`: Source code for all packages
  - `EasilyNET.Core/`: Core utilities and extensions
  - `EasilyNET.WebCore/`: Web API related components
  - `EasilyNET.AutoDependencyInjection/`: Dependency injection framework
  - `EasilyNET.RabbitBus.AspNetCore/`: RabbitMQ message bus for ASP.NET Core
  - `EasilyNET.Mongo.AspNetCore/`: MongoDB integration for ASP.NET Core
  - `EasilyNET.Security/`: Security and encryption utilities
- `sample/`: Sample applications and usage examples
- `test/`: Unit tests and benchmarks
- `docs/`: Documentation for individual packages

## Coding Standards

### General Guidelines

- Follow the existing code style in the project.
- Use meaningful variable and method names.
- Add XML documentation comments for public APIs.
- Keep methods small and focused on a single responsibility.

### C# Specific

- Use the latest C# features where appropriate (project targets .NET 10 and C# preview features).
- Prefer `async/await` for asynchronous operations.
- Use pattern matching and modern language features.
- Follow the [Microsoft C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/inside-a-csharp/coding-conventions).

### Commit Messages

- Use clear, descriptive commit messages.
- Start with a verb (e.g., "Add", "Fix", "Update", "Refactor").
- Keep the first line under 50 characters.
- Add more details in the body if needed.

Example:

```
Add support for DateOnly and TimeOnly in MongoDB serialization

- Serialize DateOnly as string in ISO format
- Serialize TimeOnly as long (ticks)
- Add unit tests for new serialization logic
```

## Testing

### Running Tests

Run all tests:

```bash
dotnet test
```

Run tests for a specific project:

```bash
dotnet test test/EasilyNET.Test.Unit/EasilyNET.Test.Unit.csproj
```

### Writing Tests

- Write unit tests for new features and bug fixes.
- Use xUnit as the testing framework (follow existing patterns).
- Aim for good test coverage.
- Place tests in the appropriate `test/` directory.

### Benchmarks

For performance-critical code, add benchmarks in `test/EasilyNET.Core.Benchmark/`.

## Submitting Changes

1. Ensure your code builds and all tests pass:

   ```bash
   dotnet build
   dotnet test
   ```

2. Update documentation if needed (README files in `src/` directories).

3. Commit your changes:

   ```bash
   git add .
   git commit -m "Your descriptive commit message"
   ```

4. Push to your fork:

   ```bash
   git push origin feature/your-feature-name
   ```

5. Create a Pull Request:
   - Go to the original repository on GitHub.
   - Click "New Pull Request".
   - Select your branch and provide a clear description of your changes.
   - Reference any related issues.

### Pull Request Guidelines

- Provide a clear title and description.
- Include screenshots or examples for UI changes.
- Keep PRs focused on a single feature or fix.
- Ensure CI checks pass.
- Be responsive to feedback and make requested changes.

## Reporting Issues

- Use GitHub Issues to report bugs or request features.
- Provide detailed steps to reproduce bugs.
- Include relevant environment information (.NET version, OS, etc.).
- Check existing issues before creating new ones.

## License

By contributing to this project, you agree that your contributions will be licensed under the same [MIT License](LICENSE) that covers the project.

---

Thank you for contributing to EasilyNET! Your efforts help make this project better for everyone.
