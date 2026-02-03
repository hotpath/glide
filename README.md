# Glide

A simple Kanban board management system built with ASP.NET Core 10.0, featuring server-side Razor Components with HTMX for dynamic interactions, SQLite for data storage, and GitHub OAuth for authentication.

## Features

- **Kanban Boards** - Create and manage multiple boards with customizable columns
- **User Management** - Add team members as owners or regular members with email-based discovery
- **Real-time Updates** - HTMX-powered dynamic UI without page refreshes
- **OAuth Authentication** - Secure GitHub-based user authentication
- **Dark Theme** - Modern dark-first UI with responsive design

## Quick Start

### Prerequisites

- .NET 10.0 SDK or Docker/Podman
- GitHub OAuth application credentials (if running locally)

### Using Docker Compose

```bash
docker compose up
```

The application will be available at `http://localhost:8080`

### Using Podman

```bash
podman-compose up
```

Or build and run manually:

```bash
podman build -f Containerfile -t glide:latest .
podman run -p 8080:8080 \
  -e GLIDE_DATABASE_PATH=/data/glide.db \
  -e GITHUB_CLIENT_ID=your-client-id \
  -e GITHUB_CLIENT_SECRET=your-client-secret \
  -e GITHUB_BASE_URI=https://github.com \
  -e GITHUB_REDIRECT_URI=http://localhost:8080/auth/callback \
  -v glide-data:/data \
  glide:latest
```

### Local Development

```bash
# Restore dependencies
dotnet restore

# Run the application
dotnet run --project src/Glide.Web/Glide.Web.csproj
```

The application will be available at `http://localhost:5258`

## Configuration

Environment variables:

- `GLIDE_DATABASE_PATH` - SQLite database file location (default: `data/glide.db`)
- `GLIDE_PORT` - Server port (default: 8080 in Docker, 5258 locally)
- `GITHUB_CLIENT_ID` - OAuth client ID
- `GITHUB_CLIENT_SECRET` - OAuth client secret
- `GITHUB_BASE_URI` - GitHub instance URL (default: `https://github.com`)
- `GITHUB_REDIRECT_URI` - OAuth redirect URI (e.g., `http://localhost:8080/auth/callback`)
- `SESSION_DURATION_HOURS` - User session lifetime in hours (default: 720 = 30 days)

## Development

### Build

```bash
dotnet build
```

### Tests

```bash
# Run all tests
dotnet test

# Run with coverage report
dotnet test --coverage --coverage-output-format cobertura
```

Code coverage must be ≥80% line coverage (enforced in CI).

### Architecture

- **Data Layer** (`src/Glide.Data`) - Repositories, migrations, domain models
- **Web Layer** (`src/Glide.Web`) - Controllers, actions, Razor components
- **Database** - SQLite with automatic migrations on startup

## Technology Stack

- **Framework** - ASP.NET Core 10.0
- **Frontend** - Razor Components + HTMX 2.0
- **Database** - SQLite
- **ORM** - Dapper
- **Migrations** - FluentMigrator
- **Testing** - TUnit
- **Container** - Podman/Docker

## License

See [LICENSE](./LICENSE)