# Copilot Instructions for Glide

Glide is an ASP.NET Core 10.0 Kanban board application with server-side Razor Components, HTMX, SQLite, and Forgejo OAuth.

## Quick Commands

### Build & Run
```bash
dotnet restore                                      # Restore dependencies
dotnet build                                        # Build solution
dotnet run --project src/Glide.Web/Glide.Web.csproj  # Start dev server (http://localhost:5258)
docker compose up                                   # Run with Docker
```

### Testing
```bash
dotnet test                                         # Run all tests
dotnet test test/Glide.Data.Unit/Glide.Data.Unit.csproj  # Run data layer tests only
dotnet test --coverage --coverage-output-format cobertura  # Generate coverage report
```

**Requirement:** Code coverage must be ≥80% line coverage (enforced by CI).

### Database Migrations
Migrations run automatically on app startup. To create a new migration:
1. Add a class in `src/Glide.Data/Migrations/` inheriting from `FluentMigrator.Migration`
2. Annotate with `[Migration(UnixTimestampHere)]`
3. Implement `Up()` and `Down()` methods

## Architecture

### Three-Layer Design
```
Glide.Web (Presentation)
  ├─ Controllers (HTTP endpoints)
  ├─ Actions (BoardAction, CardAction, ColumnAction - business logic)
  ├─ Razor Components (server-side rendering)
  └─ Auth (OAuth, session middleware)
       ↓
Glide.Data (Data Access)
  ├─ Repositories (async CRUD: GetAsync, CreateAsync, UpdateAsync, DeleteAsync)
  ├─ Models (immutable records: Board, Card, Column, User, Session)
  └─ Migrations (FluentMigrator schema definitions)
       ↓
SQLite Database
```

### Key Architectural Patterns

**Data Layer:**
- Dapper micro-ORM with snake_case column mapping
- All repository methods are async: `Task<T> GetAsync()`, `Task CreateAsync()`, etc.
- IDs are sortable UUIDs: `Guid.CreateVersion7()`
- Database tables: `users`, `boards`, `boards_users`, `columns`, `cards`, `sessions`, `labels`, `task_labels`

**Business Logic (Actions):**
- `BoardAction`, `CardAction`, `ColumnAction` encapsulate domain operations
- Authorization checks use `ClaimsPrincipal` for board ownership
- Methods return `Result<T>` wrapper for error handling

**Web Layer:**
- HTMX integration: server renders HTML fragments, HTMX swaps DOM elements
- Razor Components return `RazorComponentResult` for partial page updates
- Custom session validation (not ASP.NET Identity)
- Sessions stored in SQLite with `glide_session` cookie

**Authentication Flow:**
1. Login redirects to Forgejo OAuth authorize endpoint (with CSRF state)
2. OAuth callback exchanges code for token
3. Fetch user info from Forgejo API, upsert to database
4. Create session record (30-day default)
5. Set `glide_session` cookie
6. `SessionValidationMiddleware` validates on subsequent requests

### Repository Pattern
- Injected via DI (Singleton lifetime)
- All repositories use `IDbConnectionFactory` for connections
- Example methods:
  ```csharp
  Task<Board?> GetByIdAsync(Guid id);
  Task<IEnumerable<Board>> GetAsync();
  Task<Board> CreateAsync(Board board);
  Task UpdateAsync(Board board);
  Task DeleteAsync(Guid id);
  ```

## Code Conventions

### Naming Conventions
- **Database columns:** snake_case (`board_id`, `created_at`, `oauth_provider`)
- **C# code:** PascalCase (classes, methods, properties)
- **Local variables/parameters:** camelCase
- **Private fields:** `_camelCase`
- **Private static fields:** `s_camelCase`
- **Timestamps:** Unix milliseconds in database

### Configuration (Environment Variables)
- `GLIDE_DATABASE_PATH` – SQLite file location (default: `data/glide.db`)
- `GLIDE_PORT` – Server port (default: 8080 in Docker, 5258 locally)
- `FORGEJO_CLIENT_ID`, `FORGEJO_CLIENT_SECRET` – OAuth credentials
- `FORGEJO_BASE_URI`, `FORGEJO_REDIRECT_URI` – OAuth endpoints
- `SESSION_DURATION_HOURS` – Session lifetime (default: 720 = 30 days)

### Testing Patterns
- **Framework:** TUnit 1.12.90 (async-first)
- **Base Class:** `RepositoryTestBase` creates isolated SQLite database per test
- Each test runs full migration stack on temporary `glide_test_{guid}.db`
- Cleanup is automatic

### Code Style
- EditorConfig enforced (`.editorconfig`)
- File-scoped namespaces preferred: `namespace Glide.Data;`
- Primary constructors preferred where applicable
- Expression-bodied members for simple properties/accessors
- Organize usings: system directives first, separated from other imports

## Domain Model

**Core Entities:**
- `User` – OAuth identity (provider, provider_user_id, email, display_name)
- `Board` – Kanban board with collection of BoardUsers
- `Column` – Board column (position determines order)
- `Card` – Task/card (belongs to column, has position)
- `Session` – User session with expiry timestamp
- `Label` – Tags for cards (many-to-many via `task_labels`)

**Key Relationships:**
- `boards_users` – Many-to-many with ownership flag
- Columns → Cards (one-to-many)
- Cards → Labels (many-to-many)

## Technology Stack

- **.NET:** 10.0.102 SDK, ASP.NET Core 10.0
- **ORM:** Dapper 2.1.66
- **Database:** SQLite (Microsoft.Data.Sqlite 10.0.2)
- **Migrations:** FluentMigrator 8.0.1
- **Frontend:** Razor Components, HTMX 2.0.8
- **Other:** Markdig 0.44.0 (Markdown), highlight.js 11.9.0 (syntax highlighting)

## Important Files

- `src/Glide.Web/Program.cs` – App startup and DI configuration
- `src/Glide.Data/Migrations/CreateInitialSchema.cs` – Database schema
- `src/Glide.Web/Auth/SessionValidationMiddleware.cs` – Session validation logic
- `test/Glide.Data.Unit/RepositoryTestBase.cs` – Test base class
- `.github/workflows/test.yml` – CI test pipeline (enforces ≥80% coverage)
- `.github/workflows/build.yml` – Container image builds
- `Directory.Build.props` – Shared project version (currently 0.2.0)
- `.editorconfig` – Code style rules

## CI/CD

- **GitHub Actions** runs tests and builds container images
- `test.yml` – Executes tests with coverage validation on push/PR
- `build.yml` – Builds multi-stage Dockerfile for main/develop/release branches
- Runtime stage uses ASP.NET 10.0 distroless image (security-focused)
- Published to Codeberg Container Registry
