# Contributing Guide

## Development Setup

1. Install .NET 8 SDK
2. Install PostgreSQL
3. Clone repository
4. Run `dotnet restore` in solution directory
5. Update connection string in `appsettings.Development.json`
6. Run `dotnet ef database update` to create database
7. Run `dotnet test` to verify setup

## Code Style

- Follow Microsoft's C# coding conventions
- Use meaningful variable and method names
- Add XML documentation comments to public APIs
- Keep methods focused and under 50 lines when possible

## Adding New Features

1. **Create a branch:** `git checkout -b feature/my-feature`
2. **Write tests first** (TDD approach)
3. **Implement feature** following existing patterns
4. **Update documentation** (README, DESIGN.md)
5. **Run all tests:** `dotnet test`
6. **Build in Release:** `dotnet build --configuration Release`
7. **Commit with clear message:** `git commit -m "Add feature X"`

## Project Structure Conventions

### Controllers
- Handle HTTP concerns only
- Delegate business logic to services
- Return appropriate HTTP status codes

### Services
- Contain business logic
- Throw domain-specific exceptions
- Log important operations
- Use async/await for I/O operations

### Repositories
- Abstract database operations
- Use EF Core best practices
- Log errors and warnings
- Return domain entities

### Tests
- **Unit tests:** Test single component in isolation
- **Integration tests:** Test full request/response pipeline
- Follow AAA pattern: Arrange, Act, Assert
- Use descriptive test names: `MethodName_Scenario_ExpectedBehavior`

## Running Tests

```bash
# All tests
dotnet test

# Specific category
dotnet test --filter "FullyQualifiedName~Unit"
dotnet test --filter "FullyQualifiedName~Integration"

# With coverage (requires coverlet)
dotnet test /p:CollectCoverage=true
```

## Database Migrations

After modifying entities:

```bash
# Create migration
dotnet ef migrations add MigrationName

# Apply migration
dotnet ef database update

# Rollback
dotnet ef database update PreviousMigrationName

# Remove last migration
dotnet ef migrations remove
```

## Common Tasks

### Add a New Endpoint

1. Add method to `IBoardService` interface
2. Implement in `BoardService`
3. Add action to `BoardsController`
4. Add request/response DTOs if needed
5. Add validation if needed
6. Write unit tests for service logic
7. Write integration tests for endpoint

### Add Configuration Setting

1. Add property to `GameConfiguration`
2. Update `appsettings.json` with default value
3. Use via `IOptions<GameConfiguration>` injection

### Add Validation Rule

1. Update or create validator in `Validators/`
2. Add test cases in test project
3. Register validator in `Program.cs` if new

## Debugging

### Visual Studio Code
1. Install C# extension
2. Open project folder
3. F5 to start debugging

### Command Line
```bash
dotnet run --project ConwaysGameOfLifeApi
```

### Attach to running process
Use VS Code "Attach to Process" option

## Performance Profiling

### Using dotnet-trace
```bash
dotnet tool install --global dotnet-trace
dotnet trace collect --process-id <PID>
```

### Using BenchmarkDotNet
Add benchmark project and reference BenchmarkDotNet package

## Before Submitting PR

- [ ] All tests pass
- [ ] Code follows style guidelines
- [ ] New features have tests
- [ ] Documentation updated
- [ ] No compiler warnings
- [ ] Commit messages are clear

## Questions?

Open an issue or reach out to the maintainers.
