# Local Development Setup Guide

This guide walks you through setting up the complete Sqordia development environment locally using Docker.

## Prerequisites

Before starting, ensure you have the following installed:

- **Docker Desktop** (or Docker Engine + Docker Compose)
  - Download: https://www.docker.com/products/docker-desktop
  - Verify: `docker --version` and `docker-compose --version`

- **.NET 8 SDK** (for running migrations and local development)
  - Download: https://dotnet.microsoft.com/download/dotnet/8.0
  - Verify: `dotnet --version`

- **pgAdmin** or **DBeaver** (optional, for PostgreSQL database management)
  - Download pgAdmin: https://www.pgadmin.org/download/
  - Download DBeaver: https://dbeaver.io/download/

- **Postman** (for API testing)
  - Download: https://www.postman.com/downloads/

## Overview

The local development environment consists of:

1. **PostgreSQL Database** - Containerized PostgreSQL 16
2. **Backend API** - ASP.NET Core 8.0 Web API
3. **Frontend** - React/Vite application served via Nginx

All services run in Docker containers and communicate via a Docker network.

## Step-by-Step Setup

### Step 1: Clone the Repository

```bash
git clone https://github.com/sqordiadev-lgtm/Sqordia-backend.git
cd Sqordia-backend
```

### Step 2: Start Docker Services

#### 2.1 Start Database and Backend

From the `Sqordia-backend` directory:

```bash
docker-compose -f docker-compose.dev.yml up -d
```

This command will:
- Pull PostgreSQL 16 image (if not already present)
- Create Docker network (`sqordia-network`)
- Start PostgreSQL container (`sqordia-db-dev`)
- Wait for database to be healthy
- Build and start backend API container (`sqordia-api-dev`)

**Expected output:**
```
Creating network "sqordia-backend_sqordia-network" ... done
Creating sqordia-db-dev ... done
Creating sqordia-api-dev ... done
```

#### 2.2 Verify Services are Running

```bash
docker ps
```

You should see:
- `sqordia-db-dev` - Status: Up (healthy)
- `sqordia-api-dev` - Status: Up

#### 2.3 Check Service Health

```bash
# Check backend health
curl http://localhost:5241/health

# Expected response:
# {"status":"Healthy","timestamp":"...","checks":[]}
```

### Step 3: Database Setup

#### 3.1 Run Database Migrations

The backend will automatically apply migrations on startup, but you can also run them manually:

```bash
cd Sqordia-backend
dotnet ef database update --project src/Infrastructure/Sqordia.Persistence --startup-project src/WebAPI
```

**Connection String:**
```
Host=localhost;Port=5432;Database=SqordiaDb;Username=postgres;Password=postgres;
```

#### 3.2 Seed the Database

Run the seed scripts to populate initial data:

**Option A: Using psql (if installed)**
```bash
psql -h localhost -p 5432 -U postgres -d SqordiaDb -f scripts/seed.sql
```

**Option B: Using Docker**
```bash
# Copy seed script into container
docker cp scripts/seed.sql sqordia-db-dev:/tmp/seed.sql

# Execute the script
docker exec sqordia-db-dev psql -U postgres -d SqordiaDb -f /tmp/seed.sql
```

**Option C: Using pgAdmin or DBeaver**
1. Connect to: `localhost:5432`
2. Username: `postgres`
3. Password: `postgres`
4. Database: `SqordiaDb`
5. Open and execute seed scripts from `scripts/` directory

#### 3.3 Verify Database Seeding

```bash
# Check if admin user exists
docker exec sqordia-db-dev psql -U postgres -d SqordiaDb \
  -c "SELECT \"Email\", \"UserName\" FROM \"Users\" WHERE \"Email\" = 'admin@sqordia.com'"
```

### Step 4: Start Frontend

#### 4.1 Build Frontend Docker Image

```bash
cd ../Sqordia-frontend
docker build -t sqordia-frontend:dev .
```

#### 4.2 Start Frontend Container

```bash
docker run -d \
  --name sqordia-frontend-dev \
  -p 5173:80 \
  --network sqordia-backend_sqordia-network \
  sqordia-frontend:dev
```

**Note:** The frontend needs to connect to the backend network to make API calls.

#### 4.3 Verify Frontend is Running

```bash
curl http://localhost:5173
```

You should see the HTML content of the frontend application.

### Step 5: Access the Application

#### 5.1 Frontend
Open your browser and navigate to:
```
http://localhost:5173
```

#### 5.2 Backend API
- **API Base URL:** `http://localhost:5241`
- **Swagger Documentation:** `http://localhost:5241/swagger`
- **Health Check:** `http://localhost:5241/health`

#### 5.3 Default Login Credentials

After seeding the database:
- **Email:** `admin@sqordia.com`
- **Password:** `Sqordia2025!`

## Service Details

### Database (SQL Server)

- **Container:** `sqordia-db-dev`
- **Image:** `mcr.microsoft.com/mssql/server:2022-latest`
- **Port:** `5432`
- **Username:** `postgres`
- **Password:** `postgres` (default, can be changed via `POSTGRES_PASSWORD` env var)
- **Database:** `SqordiaDb`
- **Connection String:** `Host=localhost;Port=5432;Database=SqordiaDb;Username=postgres;Password=postgres;`

### Backend API

- **Container:** `sqordia-api-dev`
- **Port:** `5241` (host) → `8080` (container)
- **Environment:** `Development`
- **API Base URL:** `http://localhost:5241`
- **Health Endpoint:** `http://localhost:5241/health`

**Environment Variables:**
- `ASPNETCORE_ENVIRONMENT=Development`
- `ASPNETCORE_URLS=http://+:8080`
- `ConnectionStrings__DefaultConnection=Host=sqordia-db;Port=5432;Database=SqordiaDb;...`
- `JwtSettings__Secret=S6075c41562a35879f7e40220d435f1f6972c7c7a9deea0c5`
- `Database__Provider=PostgreSQL` (for local development)

### Frontend

- **Container:** `sqordia-frontend-dev`
- **Port:** `5173` (host) → `80` (container)
- **Base URL:** `http://localhost:5173`
- **API URL:** Configured to `http://localhost:5241` (or via `VITE_API_URL` env var)

## Common Commands

### View Logs

```bash
# Backend logs
docker logs sqordia-api-dev -f

# Database logs
docker logs sqordia-db-dev -f

# Frontend logs
docker logs sqordia-frontend-dev -f

# All services logs
docker-compose -f docker-compose.dev.yml logs -f
```

### Stop Services

```bash
# Stop backend and database
docker-compose -f docker-compose.dev.yml down

# Stop frontend
docker stop sqordia-frontend-dev
docker rm sqordia-frontend-dev

# Stop all services
docker stop sqordia-api-dev sqordia-db-dev sqordia-frontend-dev
```

### Restart Services

```bash
# Restart backend and database
docker-compose -f docker-compose.dev.yml restart

# Restart frontend
docker restart sqordia-frontend-dev
```

### Rebuild Services

```bash
# Rebuild and restart backend
docker-compose -f docker-compose.dev.yml up -d --build

# Rebuild frontend
cd ../Sqordia-frontend
docker build -t sqordia-frontend:dev .
docker stop sqordia-frontend-dev
docker rm sqordia-frontend-dev
docker run -d --name sqordia-frontend-dev -p 5173:80 --network sqordia-backend_sqordia-network sqordia-frontend:dev
```

### Clean Up

```bash
# Stop and remove containers
docker-compose -f docker-compose.dev.yml down

# Remove frontend container
docker stop sqordia-frontend-dev
docker rm sqordia-frontend-dev

# Remove volumes (WARNING: This deletes all data)
docker-compose -f docker-compose.dev.yml down -v

# Remove images
docker rmi sqordia-frontend:dev
```

## Troubleshooting

### Database Connection Issues

**Problem:** Cannot connect to SQL Server

**Solutions:**
1. Verify container is running: `docker ps | grep sqordia-db-dev`
2. Check if port 1433 is available: `lsof -i :1433`
3. Wait for database to be healthy: `docker logs sqordia-db-dev`
4. Verify connection string in backend logs: `docker logs sqordia-api-dev`

### Backend Not Starting

**Problem:** Backend container exits immediately

**Solutions:**
1. Check logs: `docker logs sqordia-api-dev`
2. Verify database is healthy: `docker ps | grep sqordia-db-dev`
3. Check environment variables: `docker exec sqordia-api-dev env | grep ConnectionStrings`
4. Ensure migrations are applied: Check database for tables

### Frontend Cannot Connect to Backend

**Problem:** Frontend shows API errors or cannot reach backend

**Solutions:**
1. Verify both containers are on the same network:
   ```bash
   docker network inspect sqordia-backend_sqordia-network
   ```
2. Check frontend API URL configuration
3. Verify backend is accessible: `curl http://localhost:5241/health`
4. Check CORS settings in backend

### Port Already in Use

**Problem:** `Error: bind: address already in use`

**Solutions:**
1. Find process using the port:
   ```bash
   lsof -i :5241  # Backend
   lsof -i :5173  # Frontend
   lsof -i :5432  # Database
   ```
2. Stop the conflicting service
3. Or change ports in `docker-compose.dev.yml`

### Database Migrations Fail

**Problem:** Migrations fail with connection errors

**Solutions:**
1. Ensure database container is healthy: `docker ps`
2. Wait for database to fully start (can take 30-60 seconds)
3. Verify connection string
4. Check database logs: `docker logs sqordia-db-dev`

## Development Workflow

### Making Backend Changes

1. Make code changes
2. Rebuild container: `docker-compose -f docker-compose.dev.yml up -d --build sqordia-webapi`
3. Or restart: `docker-compose -f docker-compose.dev.yml restart sqordia-webapi`

### Making Frontend Changes

1. Make code changes
2. Rebuild image: `docker build -t sqordia-frontend:dev .`
3. Restart container: `docker restart sqordia-frontend-dev`

### Running Migrations

```bash
# Create new migration
dotnet ef migrations add MigrationName \
  --project src/Infrastructure/Sqordia.Persistence \
  --startup-project src/WebAPI

# Apply migrations
dotnet ef database update \
  --project src/Infrastructure/Sqordia.Persistence \
  --startup-project src/WebAPI
```

### Testing API Endpoints

Use Postman collections (see Postman Setup section below) or curl:

```bash
# Health check
curl http://localhost:5241/health

# Login
curl -X POST http://localhost:5241/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@sqordia.com","password":"Sqordia2025!"}'
```

## Environment Variables

### Backend Environment Variables

Set these in `docker-compose.dev.yml` or as environment variables:

- `POSTGRES_PASSWORD` - PostgreSQL password (default: `postgres`)
- `JWT_SECRET` - JWT signing secret
- `JWT_ISSUER` - JWT issuer (default: `Sqordia`)
- `JWT_AUDIENCE` - JWT audience (default: `SqordiaUsers`)
- `GOOGLE_OAUTH_CLIENT_ID` - Google OAuth client ID (optional)
- `GOOGLE_OAUTH_CLIENT_SECRET` - Google OAuth client secret (optional)
- `OPENAI_MODEL` - OpenAI model (default: `gpt-4`)

### Frontend Environment Variables

Set via `.env` file or build args:

- `VITE_API_URL` - Backend API URL (default: `http://localhost:5241`)

## Quick Reference

### Start Everything

```bash
# Backend and database
cd Sqordia-backend
docker-compose -f docker-compose.dev.yml up -d

# Frontend
cd ../Sqordia-frontend
docker build -t sqordia-frontend:dev .
docker run -d --name sqordia-frontend-dev -p 5173:80 --network sqordia-backend_sqordia-network sqordia-frontend:dev
```

### Stop Everything

```bash
docker-compose -f docker-compose.dev.yml down
docker stop sqordia-frontend-dev && docker rm sqordia-frontend-dev
```

### Access URLs

- Frontend: http://localhost:5173
- Backend API: http://localhost:5241
- Swagger: http://localhost:5241/swagger
- Database: localhost:5432

### Default Credentials

- Email: `admin@sqordia.com`
- Password: `Sqordia2025!`

## Postman Setup

### Import Collections

Two Postman collections are available:

1. **Local Development** (`postman/Sqordia_API_Local.json`)
   - Base URL: `http://localhost:5241`
   - For testing against local Docker environment

2. **Production** (`postman/Sqordia_API_Production.json`)
   - Base URL: `http://34.19.252.60:8080`
   - For testing against production deployment

### Import Steps

1. Open Postman
2. Click **Import** button
3. Select the appropriate collection file:
   - For local: `postman/Sqordia_API_Local.json`
   - For production: `postman/Sqordia_API_Production.json`
4. Collection will be imported with all endpoints

### Using the Collections

#### Local Development Collection

1. **Set Environment Variables** (optional):
   - `base_url`: `http://localhost:5241` (already set)
   - `admin_email`: `admin@sqordia.com`
   - `admin_password`: `Sqordia2025!`

2. **Quick Start**:
   - Run "Health Check" to verify API is running
   - Run "Login (Admin)" to authenticate
   - Token will be automatically saved to `jwt_token` variable
   - All subsequent requests will use the token

#### Production Collection

1. **Set Environment Variables**:
   - `base_url`: `http://34.19.252.60:8080` (already set)
   - Update admin credentials if different from local

2. **Authentication**:
   - Use production admin credentials
   - Token will be saved automatically

### Collection Structure

Both collections include:

1. **Health & System** - Health checks and system status
2. **Database Seeding** - Admin-only seeding endpoints
3. **Authentication** - Login, register, token management
4. **User Profile** - Profile management
5. **Two-Factor Authentication** - 2FA setup and management
6. **Security & Sessions** - Session management
7. **Admin Dashboard** - Admin-only endpoints
8. **Role Management** - Role and permission management
9. **Organizations** - Organization CRUD operations
10. **Business Plans** - Business plan management
11. **Templates** - Template management
12. **Questionnaires** - Questionnaire endpoints
13. **OBNL Plans** - OBNL-specific endpoints
14. **Financial** - Financial projections and analysis
15. **Export** - Document export endpoints

### Auto-Save Tokens

The collections include scripts that automatically:
- Save JWT token after login
- Save refresh token
- Save user ID
- Use saved token for authenticated requests

### Testing Workflow

1. **Health Check**: Verify API is running
2. **Login**: Authenticate and get token
3. **Get Current User**: Verify authentication works
4. **Test Endpoints**: Use any endpoint in the collection
5. **Refresh Token**: If token expires, use refresh token endpoint

## Next Steps

1. ✅ All services running
2. ✅ Database seeded
3. ✅ Access frontend at http://localhost:5173
4. ✅ Import Postman collection for API testing
5. ✅ Start developing!

For production deployment, see [DEPLOYMENT.md](./DEPLOYMENT.md).

