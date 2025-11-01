# GitHub Copilot Instructions for EasilyNET Project

## Project Overview

EasilyNET is a .NET library collection focusing on MongoDB integration, RabbitMQ messaging, dependency injection, and web utilities.

## Code Review Guidelines

### Response Language

- Always respond in both **English** and **Chinese (中文)** for code reviews
- Provide English explanation first, followed by Chinese translation

### Review Focus Areas

#### 1. Readability & Maintainability

- Check for clear variable and method naming
- Verify appropriate code comments and XML documentation
- Ensure consistent code formatting and style
- Review code complexity and suggest simplifications

#### 2. .NET Best Practices

- Follow C# coding conventions and naming standards
- Use proper async/await patterns (avoid `.Result` or `.Wait()`)
- Implement IDisposable correctly for resource management
- Utilize nullable reference types appropriately
- Prefer dependency injection over static dependencies
- Use appropriate collection types (List, IEnumerable, ICollection)

#### 3. Performance Considerations

- Identify potential memory leaks or excessive allocations
- Review LINQ usage for performance issues (avoid multiple enumerations)
- Check for proper use of `StringBuilder` for string concatenation
- Suggest object pooling where applicable
- Review database query efficiency (N+1 queries, proper indexing)

#### 4. Security Best Practices

- Validate input parameters
- Check for SQL injection risks (though using MongoDB)
- Review authentication and authorization implementations
- Ensure sensitive data is not logged
- Check for proper exception handling without exposing sensitive information

#### 5. MongoDB Specific

- Verify proper index usage
- Check for efficient query patterns
- Review projection usage to limit data transfer
- Ensure proper connection string management
- Validate schema design and document structure

#### 6. RabbitMQ Messaging

- Review message contract design
- Check for proper acknowledgment handling
- Verify retry and error handling strategies
- Ensure message idempotency where needed
- Review queue and exchange configurations

#### 7. Dependency Injection

- Verify proper service lifetime (Singleton, Scoped, Transient)
- Check for circular dependencies
- Review service registration patterns
- Ensure interface-based programming

#### 8. Testing

- Suggest unit test coverage for new code
- Review test naming conventions
- Check for proper use of mocking frameworks
- Ensure tests are isolated and deterministic

## Code Generation Guidelines

### General Principles

- Generate clean, maintainable, and well-documented code
- Follow SOLID principles
- Implement proper error handling and logging
- Use modern C# features appropriately (pattern matching, records, etc.)
- Include XML documentation comments for public APIs

### Project-Specific Patterns

- Follow the modular architecture pattern used in this project
- Implement `AppModule` pattern for dependency registration
- Use the AutoDependencyInjection framework when appropriate
- Follow existing patterns for MongoDB repositories and services
- Maintain consistency with existing code style

### Documentation

- Generate comprehensive XML comments for public APIs
- Include usage examples in documentation
- Document complex algorithms and business logic
- Provide meaningful commit messages

### NuGet Package Development

- Update version numbers appropriately
- Maintain backward compatibility when possible
- Document breaking changes clearly
- Include proper package metadata (description, tags, license)

## Common Tasks

### When Adding New Features

1. Check if similar functionality exists to maintain consistency
2. Consider impact on existing code and backward compatibility
3. Add appropriate unit tests
4. Update relevant documentation
5. Follow the existing project structure

### When Fixing Bugs

1. Understand the root cause before proposing fixes
2. Consider edge cases and potential side effects
3. Add regression tests
4. Document the fix in commit messages

### When Refactoring

1. Ensure functionality remains unchanged
2. Improve code quality without changing behavior
3. Run existing tests to verify nothing breaks
4. Update documentation if method signatures change

## Response Format for Code Reviews

When reviewing code, provide feedback in this structure:

```
**English:**
[Detailed feedback in English]

**中文:**
[详细的中文反馈]
```

## Additional Notes

- Prioritize code quality over quick fixes
- Suggest improvements but respect existing architectural decisions
- Be constructive and provide actionable feedback
- Consider the context and purpose of the code being reviewed
