# Contributing to Glide

Thank you for your interest in contributing to Glide! This document outlines the process and guidelines for contributing.

## Getting Started

### Prerequisites

- .NET 10.0 SDK
- Git
- Your preferred IDE (VS Code, Visual Studio, Rider)

### Local Development Setup

**Option 1: Using the setup script (recommended)**

```bash
./scripts/setup-dev.sh
```

**Option 2: Manual setup**

```bash
# Copy environment template
cp .env.example .env

# Edit .env with your OAuth credentials
nano .env

# Restore dependencies
dotnet restore

# Run the application
dotnet run --project src/Glide.Web/Glide.Web.csproj
```

The application will be available at `http://localhost:5258`.

## Development Workflow

### Building

```bash
dotnet build
```

### Testing

All tests **must pass** and maintain ≥80% code coverage:

```bash
# Run all tests
dotnet test

# Run with coverage report
dotnet test --coverage --coverage-output-format cobertura
```

### Code Style

- Follow standard C# conventions (PascalCase for public members, camelCase for local variables)
- Enable nullable reference types (`#nullable enable`) in new files
- Use async/await for all I/O operations
- Write self-documenting code; only comment non-obvious logic

The repository enforces EditorConfig rules automatically.

## Architecture

Glide follows a three-layer architecture:

```text
Web Layer (Controllers, Actions, Razor Components)
    ↓
Data Layer (Repositories, Models)
    ↓
SQLite Database
```

### Making Changes

1. **Data Layer Changes**: Add/modify repositories in `src/Glide.Data/`
   - Update migrations in `src/Glide.Data/Migrations/` if schema changes
   - Write tests in `test/Glide.Data.Unit/`
   - All repository methods must be async

2. **Business Logic**: Add actions in `src/Glide.Web/[Feature]/`
   - Example: `src/Glide.Web/Boards/BoardAction.cs`
   - Use `Result<T>` for error handling
   - Include authorization checks

3. **Web/UI Changes**: Update controllers or Razor Components
   - Controllers in `src/Glide.Web/[Feature]/`
   - Components in `src/Glide.Web/App/Components/`

## Testing

- **Data Layer**: Use `RepositoryTestBase` for isolated database testing
- **Integration Tests**: Test end-to-end workflows with full database
- **Minimum Coverage**: 80% line coverage (enforced by CI)

Example test:

```csharp
[Test]
public async Task CreateBoard_WithValidData_ReturnsBoard()
{
    // Arrange
    var board = new Board { ... };
    
    // Act
    var result = await _boardRepository.CreateAsync(board);
    
    // Assert
    Assert.That(result.Id, Is.Not.EqualTo(Guid.Empty));
}
```

## Database Migrations

To add a database migration:

1. Create a new file in `src/Glide.Data/Migrations/` inheriting from `FluentMigrator.Migration`
2. Annotate with `[Migration(UnixTimestampHere)]`
3. Implement `Up()` and `Down()` methods
4. Test with: `dotnet test test/Glide.Data.Unit/`

Example:

```csharp
[Migration(1704067200000)]
public class AddBoardDescription : Migration
{
    public override void Up()
    {
        Alter.Table("boards").AddColumn("description").AsString(1000).Nullable();
    }

    public override void Down()
    {
        Delete.Column("description").FromTable("boards");
    }
}
```

## Submitting Changes

1. **Fork** the repository
2. **Create a feature branch**: `git checkout -b feature/your-feature`
3. **Make your changes** and ensure all tests pass
4. **Push** to your fork
5. **Open a Pull Request** with:
   - Clear description of changes
   - Reference any related issues
   - Evidence that tests pass

### PR Requirements

- ✅ All tests pass
- ✅ Code coverage ≥80%
- ✅ No linting errors
- ✅ No security vulnerabilities

## Troubleshooting

See [TROUBLESHOOTING.md](./docs/TROUBLESHOOTING.md) for common setup issues.

## License

By contributing, you agree that your contributions will be licensed under the same license as the project (see [LICENSE](./LICENSE)).
