# Troubleshooting Guide

## Common Setup Issues

### Missing `.env` File

**Error**: Application crashes on startup with "GLIDE_DATABASE_PATH not found"

**Solution**:

```bash
cp .env.example .env
# Edit .env with your OAuth credentials
```

### OAuth Credentials Not Working

**Error**: Login fails with "Invalid client_id" or "Unauthorized"

**Solution**:

1. Verify OAuth provider credentials are correct in `.env`
2. Check `OAUTH_REDIRECT_URI` matches your provider's registered redirect URI
3. Ensure `OAUTH_AUTHORIZE_URL`, `OAUTH_TOKEN_URL`, and `OAUTH_USER_INFO_URL` match your provider

**For GitHub OAuth**:

```env
OAUTH_AUTHORIZE_URL=https://github.com/login/oauth/authorize
OAUTH_TOKEN_URL=https://github.com/login/oauth/access_token
OAUTH_USER_INFO_URL=https://api.github.com/user
```

### Database File Permission Denied

**Error**: "SQLite Error 1: 'attempt to open a file that is already locked'"

**Solution**:

1. Ensure the `data/` directory exists and is writable:

   ```bash
   mkdir -p data
   chmod 755 data
   ```

2. Check if another instance of the application is running
3. Delete the corrupted database and restart:

   ```bash
   rm data/glide.db
   dotnet run --project src/Glide.Web/Glide.Web.csproj
   ```

### Port Already in Use

**Error**: "Address already in use" on port 5258 or 8080

**Solution**:
Set a different port in `.env`:

```env
GLIDE_PORT=5259
```

### Tests Failing Locally

**Error**: Test failures that don't occur in CI

**Solution**:

1. Ensure you're using the correct .NET SDK version: `dotnet --version` should be 10.0.102 or later
2. Clean build artifacts: `dotnet clean && dotnet restore && dotnet test`

### Docker Container Won't Start

**Error**: Container exits immediately after starting

**Solution**:

1. Check logs: `docker compose logs glide`
2. Verify OAuth environment variables are set in `.env`
3. Delete the corrupted database and restart: `docker compose down -v && docker compose up`

## Still Having Issues?

See [CONTRIBUTING.md](../CONTRIBUTING.md) for development setup, or open a GitHub issue with:

- Error message and stack trace
- Steps to reproduce
- Your environment (.NET version, OS)
