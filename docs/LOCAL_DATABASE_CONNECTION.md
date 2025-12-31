# Local Database Connection Guide

## Quick Connection Details

### Connection Information

- **Host**: `localhost` (from your machine) or `sqordia-db` (from Docker network)
- **Port**: `5432`
- **Database**: `SqordiaDb`
- **Username**: `postgres`
- **Password**: `postgres` (default, can be changed via `POSTGRES_PASSWORD` environment variable)

### Connection String (for .NET/EF Core)

```
Host=localhost;Port=5432;Database=SqordiaDb;Username=postgres;Password=postgres;
```

## Connection Methods

### 1. Using Docker Exec (Easiest)

Connect directly to the database container:

```powershell
# Connect to PostgreSQL
docker exec -it sqordia-db-dev psql -U postgres -d SqordiaDb
```

Once connected, you can run SQL commands:
```sql
-- List all tables
\dt

-- Query users
SELECT "Email", "UserName" FROM "Users";

-- Exit
\q
```

### 2. Using psql (PostgreSQL Command Line)

If you have PostgreSQL client installed:

```powershell
psql -h localhost -p 5432 -U postgres -d SqordiaDb
```

**Password**: `postgres`

**Windows Installation:**
- Download from: https://www.postgresql.org/download/windows/
- Or use WSL: `wsl sudo apt-get install postgresql-client`

### 3. Using pgAdmin (GUI Tool - Recommended)

1. **Download pgAdmin**: https://www.pgadmin.org/download/

2. **Create Server Connection:**
   - Right-click "Servers" → "Create" → "Server"
   - **General Tab:**
     - Name: `Sqordia Local Dev`
   - **Connection Tab:**
     - Host name/address: `localhost`
     - Port: `5432`
     - Maintenance database: `SqordiaDb`
     - Username: `postgres`
     - Password: `postgres`
   - **Advanced Tab:**
     - DB restriction: `SqordiaDb` (optional)
   - Click "Save"

3. **Connect and Explore:**
   - Expand "Sqordia Local Dev" → "Databases" → "SqordiaDb" → "Schemas" → "public" → "Tables"

### 4. Using DBeaver (Alternative GUI Tool)

1. **Download DBeaver**: https://dbeaver.io/download/

2. **Create New Connection:**
   - Click "New Database Connection" (plug icon)
   - Select "PostgreSQL"
   - **Connection Settings:**
     - Host: `localhost`
     - Port: `5432`
     - Database: `SqordiaDb`
     - Username: `postgres`
     - Password: `postgres`
   - Click "Test Connection" → "Finish"

### 5. Using .NET EF Core Tools

Run migrations or queries from your local machine:

```powershell
# Update connection string in appsettings.json or use environment variable
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=SqordiaDb;Username=postgres;Password=postgres;"

# Run migrations
dotnet ef database update --project src/Infrastructure/Sqordia.Persistence --startup-project src/WebAPI

# Or use EF Core commands
dotnet ef dbcontext info --project src/Infrastructure/Sqordia.Persistence --startup-project src/WebAPI
```

## Prerequisites

### Start the Database Container

If the database isn't running:

```powershell
# Start only the database
docker-compose -f docker-compose.dev.yml up sqordia-db -d

# Or start all services
docker-compose -f docker-compose.dev.yml up -d
```

### Verify Database is Running

```powershell
# Check container status
docker ps | Select-String "sqordia-db"

# Check if database is ready
docker exec sqordia-db-dev pg_isready -U postgres
```

## Common Database Operations

### List All Tables

```powershell
docker exec sqordia-db-dev psql -U postgres -d SqordiaDb -c "\dt"
```

### Query Data

```powershell
# Query users
docker exec sqordia-db-dev psql -U postgres -d SqordiaDb -c "SELECT \"Email\", \"UserName\" FROM \"Users\" LIMIT 10;"

# Query settings
docker exec sqordia-db-dev psql -U postgres -d SqordiaDb -c "SELECT \"Key\", \"Value\" FROM \"Settings\" LIMIT 10;"
```

### Run SQL Script

```powershell
# Copy script to container
docker cp scripts/seed.sql sqordia-db-dev:/tmp/seed.sql

# Execute script
docker exec sqordia-db-dev psql -U postgres -d SqordiaDb -f /tmp/seed.sql
```

### Backup Database

```powershell
# Create backup
docker exec sqordia-db-dev pg_dump -U postgres SqordiaDb > backup.sql

# Or with timestamp
docker exec sqordia-db-dev pg_dump -U postgres SqordiaDb > backup_$(Get-Date -Format "yyyyMMdd_HHmmss").sql
```

### Restore Database

```powershell
# Restore from backup
Get-Content backup.sql | docker exec -i sqordia-db-dev psql -U postgres -d SqordiaDb
```

## Troubleshooting

### Database Container Not Running

```powershell
# Check if container exists
docker ps -a | Select-String "sqordia-db"

# Start container
docker-compose -f docker-compose.dev.yml up sqordia-db -d

# View logs
docker logs sqordia-db-dev
```

### Connection Refused

1. **Check if port 5432 is in use:**
   ```powershell
   netstat -ano | Select-String ":5432"
   ```

2. **Check container port mapping:**
   ```powershell
   docker port sqordia-db-dev
   ```

3. **Restart container:**
   ```powershell
   docker restart sqordia-db-dev
   ```

### Authentication Failed

- Default password is `postgres`
- If changed, check `POSTGRES_PASSWORD` environment variable in `docker-compose.dev.yml`
- Or reset password:
  ```powershell
  docker exec sqordia-db-dev psql -U postgres -c "ALTER USER postgres PASSWORD 'newpassword';"
  ```

### Database Doesn't Exist

The database is created automatically when the container starts. If it doesn't exist:

```powershell
docker exec sqordia-db-dev psql -U postgres -c "CREATE DATABASE \"SqordiaDb\";"
```

## Quick Reference

| Method | Command/Tool | Use Case |
|--------|-------------|----------|
| **Docker Exec** | `docker exec -it sqordia-db-dev psql -U postgres -d SqordiaDb` | Quick queries, CLI access |
| **pgAdmin** | GUI tool | Visual database management |
| **DBeaver** | GUI tool | Alternative to pgAdmin |
| **psql** | `psql -h localhost -p 5432 -U postgres -d SqordiaDb` | If PostgreSQL client installed |
| **EF Core** | `dotnet ef database update` | Migrations, schema management |

## Connection String Examples

### For Application Code
```csharp
"Host=localhost;Port=5432;Database=SqordiaDb;Username=postgres;Password=postgres;"
```

### For Connection Tools
```
Host: localhost
Port: 5432
Database: SqordiaDb
Username: postgres
Password: postgres
```

### For Docker Network (from another container)
```
Host=sqordia-db;Port=5432;Database=SqordiaDb;Username=postgres;Password=postgres;
```

