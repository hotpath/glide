# Glide

[![.NET 10.0](https://img.shields.io/badge/.NET-10.0-512BD4?logo=.net)](https://dotnet.microsoft.com/)
[![Tests](https://github.com/hotpath/glide/actions/workflows/test.yml/badge.svg)](https://github.com/hotpath/glide/actions/workflows/test.yml)
[![Coverage](https://img.shields.io/badge/coverage-≥80%25-brightgreen)](#)
[![License](https://img.shields.io/badge/license-MIT-blue)](./LICENSE)

A simple Kanban board management system built with ASP.NET Core 10.0, featuring server-side Razor Components with HTMX for dynamic interactions, SQLite for data storage, and OAuth for authentication (supports GitHub, Forgejo, and other OAuth2 providers).

## Features

- **Kanban Boards** - Create and manage multiple boards with customizable columns
- **User Management** - Add team members as owners or regular members
- **Real-time Updates** - HTMX-powered dynamic UI without page refreshes
- **Flexible OAuth Authentication** - Supports GitHub, Forgejo, and other OAuth2 providers
- **Dark Theme** - Modern dark-first UI with responsive design

## Quick Start

### Prerequisites

- .NET 10.0 SDK or Docker/Podman
- OAuth application credentials (GitHub, Forgejo, or other OAuth2 provider)

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
  -e OAUTH_CLIENT_ID=your-client-id \
  -e OAUTH_CLIENT_SECRET=your-client-secret \
  -e OAUTH_AUTHORIZE_URL=https://github.com/login/oauth/authorize \
  -e OAUTH_TOKEN_URL=https://github.com/login/oauth/access_token \
  -e OAUTH_USER_INFO_URL=https://api.github.com/user \
  -e OAUTH_REDIRECT_URI=http://localhost:8080/auth/callback \
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

Environment variables (create a `.env` file locally, or set them in your deployment):

- `GLIDE_DATABASE_PATH` - SQLite database file location (default: `data/glide.db`)
- `GLIDE_PORT` - Server port (default: 8080 in Docker, 5258 locally)
- `OAUTH_CLIENT_ID` - OAuth client ID from your provider
- `OAUTH_CLIENT_SECRET` - OAuth client secret from your provider
- `OAUTH_AUTHORIZE_URL` - OAuth authorize endpoint (e.g., `https://github.com/login/oauth/authorize`)
- `OAUTH_TOKEN_URL` - OAuth token endpoint (e.g., `https://github.com/login/oauth/access_token`)
- `OAUTH_USER_INFO_URL` - OAuth user info endpoint (e.g., `https://api.github.com/user`)
- `OAUTH_REDIRECT_URI` - OAuth redirect URI (e.g., `http://localhost:8080/auth/callback`)
- `SESSION_DURATION_HOURS` - User session lifetime in hours (default: 720 = 30 days)

See [`.env.example`](./.env.example) for a template.

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

## Contributing

Contributions are welcome! See [CONTRIBUTING.md](./CONTRIBUTING.md) for guidelines on:
- Setting up a local development environment
- Writing and running tests
- Database migration guidelines
- Code style and conventions

## Troubleshooting

Having issues? Check [docs/TROUBLESHOOTING.md](./docs/TROUBLESHOOTING.md) for solutions to common problems.

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