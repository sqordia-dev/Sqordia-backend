# Developer Setup Guide

This guide explains how to set up and run the Sqordia API locally for development.

## Prerequisites

### Required
- **.NET 8 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Docker Desktop** - [Download](https://www.docker.com/products/docker-desktop)
- **Git** - [Download](https://git-scm.com/downloads)

### Optional (for database management)
- **pgAdmin** or **DBeaver** - For PostgreSQL database management

## Quick Start

### Option 1: Full Docker Setup (Recommended)

Runs both the API and PostgreSQL in Docker containers.

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd Sqordia
   ```

2. **Start all services**
   ```bash
   docker-compose -f docker-compose.dev.yml up --build
   ```

3. **Access the application**
   - **API**: http://localhost:5241
   - **Swagger UI**: http://localhost:5241/swagger
   - **Health Check**: http://localhost:5241/health

The setup includes:
- PostgreSQL 16 container with automatic migrations
- API container that waits for database to be ready
- Persistent database storage (data survives container restarts)

### Option 2: Local Development (API + Docker PostgreSQL)

Run the API locally with .NET while using PostgreSQL in Docker.

1. **Start PostgreSQL container only**
   ```bash
   docker-compose -f docker-compose.dev.yml up sqordia-db -d
   ```

2. **Verify database is running**
   ```bash
   docker ps
   # Should see sqordia-db-dev container running
   ```

3. **Restore dependencies**
   ```bash
   dotnet restore
   ```

4. **Run database migrations**
   ```bash
   dotnet ef database update --project src/Infrastructure/Sqordia.Persistence
   ```

5. **Run the application**
   ```bash
   dotnet run --project src/WebAPI
   ```

6. **Access the application**
   - **API**: http://localhost:5241 (or the port shown in console)
   - **Swagger UI**: http://localhost:5241/swagger

## Configuration

### PostgreSQL Connection

The application is configured to use a local PostgreSQL container:

**Connection Details:**
- **Host**: `localhost` (from host) or `sqordia-db` (from Docker network)
- **Port**: `5432`
- **Database**: `SqordiaDb`
- **Username**: `postgres`
- **Password**: `postgres` (default, configure via POSTGRES_PASSWORD env var)

**Connection String:**
```
Host=localhost;Port=5432;Database=SqordiaDb;Username=postgres;Password=postgres;
```

This is already configured in `appsettings.Development.json` for local development.

### Environment Variables (Optional)

You can override configuration using environment variables:

```bash
# JWT Settings
export JWT_SECRET=your-secret-key-here

# SendGrid (for email features)
export SENDGRID_API_KEY=your-sendgrid-key
export SENDGRID_FROM_EMAIL=your-email@domain.com

# AI Providers (optional)
export OPENAI_API_KEY=your-openai-key
export CLAUDE_API_KEY=your-claude-key
export GEMINI_API_KEY=your-gemini-key
```

## Database Management

### Connect with pgAdmin or DBeaver

1. **Connection Details:**
   - Host: `localhost`
   - Port: `5432`
   - Database: `SqordiaDb`
   - Username: `postgres`
   - Password: `postgres` (default)

2. **Connect via Command Line**
   ```bash
   docker exec -it sqordia-db-dev psql -U postgres -d SqordiaDb
   ```

### Run Migrations Manually

If migrations don't run automatically:

```bash
# From project root
dotnet ef database update --project src/Infrastructure/Sqordia.Persistence
```

Or from within the API container:
```bash
docker exec -it sqordia-api-dev dotnet ef database update
```

## Common Commands

### Docker Compose Commands

```bash
# Start all services
docker-compose -f docker-compose.dev.yml up --build

# Start in background
docker-compose -f docker-compose.dev.yml up -d

# Start only database
docker-compose -f docker-compose.dev.yml up sqordia-db -d

# Stop all services
docker-compose -f docker-compose.dev.yml down

# Stop and remove volumes (fresh start)
docker-compose -f docker-compose.dev.yml down -v

# View logs
docker-compose -f docker-compose.dev.yml logs -f

# View specific service logs
docker-compose -f docker-compose.dev.yml logs -f sqordia-webapi
docker-compose -f docker-compose.dev.yml logs -f sqordia-db

# Rebuild API after code changes
docker-compose -f docker-compose.dev.yml up -d --build sqordia-webapi
```

### Database Commands

```bash
# Check database status
docker ps | grep sqordia-db

# Connect to database shell
docker exec -it sqordia-db-dev bash

# Run SQL query
docker exec -it sqordia-db-dev /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P SqordiaDev123! \
  -Q "SELECT name FROM sys.databases"
```

### .NET Commands

```bash
# Restore packages
dotnet restore

# Build solution
dotnet build

# Run tests
dotnet test

# Run API
dotnet run --project src/WebAPI

# Run with hot reload
dotnet watch run --project src/WebAPI
```

## Development Workflow

### Using Docker Compose

1. Make code changes
2. Rebuild API: `docker-compose -f docker-compose.dev.yml up -d --build sqordia-webapi`
3. View logs: `docker-compose -f docker-compose.dev.yml logs -f sqordia-webapi`
4. Test at http://localhost:5241/swagger

### Using Local .NET Runtime

1. Start database: `docker-compose -f docker-compose.dev.yml up sqordia-db -d`
2. Make code changes
3. Restart API: Press `Ctrl+C` and run `dotnet run` again
4. Or use watch mode: `dotnet watch run --project src/WebAPI`

## Testing the API

### Using Swagger UI

1. Navigate to http://localhost:5241/swagger
2. Click "Authorize" button
3. Enter JWT token (get from `/api/v1/auth/login` endpoint)
4. Test endpoints interactively

### Using curl

```bash
# Health check
curl http://localhost:5241/health

# Login
curl -X POST "http://localhost:5241/api/v1/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email": "user@example.com", "password": "password"}'

# Use returned token for authenticated requests
curl -X GET "http://localhost:5241/api/v1/auth/me" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

### Using Postman

Import the Postman collection from `postman/Sqordia_API_Collection.json` for comprehensive API testing.

## Troubleshooting

### Port Already in Use

**Port 5241 (API)**
```bash
# Find process using port
lsof -ti:5241

# Kill process
lsof -ti:5241 | xargs kill -9
```

**Port 5432 (PostgreSQL)**
```bash
# Check if port is in use
lsof -ti:1433

# If needed, change port in docker-compose.dev.yml
ports:
  - "1434:1433"  # Use 1434 instead
```

### Database Container Not Starting

```bash
# Check container logs
docker logs sqordia-db-dev

# Check container status
docker ps -a | grep sqordia-db

# Restart database
docker-compose -f docker-compose.dev.yml restart sqordia-db

# Remove and recreate (fresh start)
docker-compose -f docker-compose.dev.yml down -v
docker-compose -f docker-compose.dev.yml up sqordia-db -d
```

### API Can't Connect to Database

1. **Verify database is running:**
   ```bash
   docker ps | grep sqordia-db
   ```

2. **Check connection string in appsettings.Development.json:**
   - Should be: `Server=localhost,1433;...`
   - Not: `Server=sqordia-db,1433;...` (only works from within Docker)

3. **Test database connection:**
   ```bash
   docker exec -it sqordia-db-dev /opt/mssql-tools/bin/sqlcmd \
     -S localhost -U sa -P SqordiaDev123! \
     -Q "SELECT 1"
   ```

4. **Check API logs:**
   ```bash
   docker-compose -f docker-compose.dev.yml logs sqordia-webapi
   ```

### Migrations Not Running

```bash
# Run migrations manually
dotnet ef database update --project src/Infrastructure/Sqordia.Persistence

# Or from Docker container
docker exec -it sqordia-api-dev dotnet ef database update
```

### Application Build Errors

```bash
# Clean and rebuild
dotnet clean
dotnet restore
dotnet build

# For Docker
docker-compose -f docker-compose.dev.yml build --no-cache
```

### Database Password Issues

If you change the password in `docker-compose.dev.yml`, update:
1. `SA_PASSWORD` environment variable in docker-compose.dev.yml
2. Connection string in `appsettings.Development.json`
3. Health check password in docker-compose.dev.yml

## Project Structure

```
Sqordia/
├── src/
│   ├── Core/
│   │   ├── Sqordia.Domain/          # Entities & Domain Logic
│   │   └── Sqordia.Application/     # Business Logic & Services
│   ├── Infrastructure/
│   │   ├── Sqordia.Infrastructure/  # External Services
│   │   └── Sqordia.Persistence/     # Data Access & EF Core
│   └── WebAPI/                      # API Controllers & Configuration
├── tests/                           # Test Projects
├── docker-compose.dev.yml           # Docker Compose for local development
└── docs/                            # Documentation
```

## Additional Resources

- **[Secrets Setup](SECRETS_SETUP.md)** - Configuration and secrets management
- **[Postman Collection](../postman/README.md)** - API testing guide
- **[Swagger Documentation](../docs/swagger.json)** - OpenAPI specification
- **[GitHub Actions Secrets](GITHUB_ACTIONS_SECRETS.md)** - CI/CD secrets configuration

## Next Steps

1. ✅ Database is running and connected
2. ✅ API is running and accessible
3. 📝 Review API endpoints in Swagger UI
4. 🧪 Test authentication flow
5. 📚 Explore the codebase structure
6. 🚀 Start developing!

---

**Need Help?** Check the logs, verify Docker is running, and ensure all required ports are available.
