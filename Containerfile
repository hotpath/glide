# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Copy Directory.Build.props first (contains version info)
COPY Directory.Build.props .

# Copy csproj files and restore dependencies
COPY src/Glide.Data/Glide.Data.csproj src/Glide.Data/
COPY src/Glide.Web/Glide.Web.csproj src/Glide.Web/
RUN dotnet restore src/Glide.Web/Glide.Web.csproj

# Copy source code and build
COPY src/ src/
RUN dotnet publish src/Glide.Web/Glide.Web.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# Create directory for SQLite database
RUN ["mkdir", "-p", "/app/data"]

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-azurelinux3.0-distroless
WORKDIR /app

# Copy published application
COPY --from=build /app/publish .

# Create directory for SQLite database
COPY --from=build /app/data .

# Set environment variables
ENV GLIDE_PORT=8080
ENV GLIDE_DATABASE_PATH=/app/data/glide.db

# Expose port
EXPOSE 8080

# Run the application
ENTRYPOINT ["dotnet", "Glide.Web.dll"]
