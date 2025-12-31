# Database Connection Guide

## RDS PostgreSQL Connection Details

### Connection Information

- **Host**: `sqordia-db-production.c326icw2wbit.ca-central-1.rds.amazonaws.com`
- **Port**: `5432`
- **Database**: `SqordiaDb`
- **Username**: `sqordia_admin`
- **Password**: [Set in `TF_VAR_rds_password` environment variable]

### Connection String Format

#### For ASP.NET Core (appsettings.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=sqordia-db-production.c326icw2wbit.ca-central-1.rds.amazonaws.com;Port=5432;Database=SqordiaDb;Username=sqordia_admin;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=true"
  }
}
```

#### For EF Core Migrations

```bash
Host=sqordia-db-production.c326icw2wbit.ca-central-1.rds.amazonaws.com;Port=5432;Database=SqordiaDb;Username=sqordia_admin;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=true
```

## Connection Methods

### 1. Using psql (PostgreSQL Command Line)

#### Install psql (if not already installed)

**Windows:**
- Download PostgreSQL from https://www.postgresql.org/download/windows/
- Or use WSL: `wsl sudo apt-get install postgresql-client`

**macOS:**
```bash
brew install postgresql
```

**Linux:**
```bash
sudo apt-get install postgresql-client
```

#### Connect via psql

```bash
psql -h sqordia-db-production.c326icw2wbit.ca-central-1.rds.amazonaws.com \
     -p 5432 \
     -U sqordia_admin \
     -d SqordiaDb \
     --set=sslmode=require
```

You'll be prompted for the password.

**Alternative (with password in connection string):**
```bash
PGPASSWORD='YOUR_PASSWORD' psql -h sqordia-db-production.c326icw2wbit.ca-central-1.rds.amazonaws.com \
     -p 5432 \
     -U sqordia_admin \
     -d SqordiaDb \
     --set=sslmode=require
```

### 2. Using pgAdmin (GUI Tool)

1. Download and install pgAdmin: https://www.pgadmin.org/download/
2. Open pgAdmin
3. Right-click "Servers" → "Create" → "Server"
4. Fill in:
   - **Name**: `Sqordia Production`
   - **Host**: `sqordia-db-production.c326icw2wbit.ca-central-1.rds.amazonaws.com`
   - **Port**: `5432`
   - **Database**: `SqordiaDb`
   - **Username**: `sqordia_admin`
   - **Password**: [Your RDS password]
   - **SSL Mode**: `Require`
5. Click "Save"

### 3. Using .NET EF Core Migrations

#### Update appsettings.Production.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=sqordia-db-production.c326icw2wbit.ca-central-1.rds.amazonaws.com;Port=5432;Database=SqordiaDb;Username=sqordia_admin;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=true"
  }
}
```

#### Run Migrations

```powershell
# Set connection string as environment variable
$env:CONNECTION_STRING = "Host=sqordia-db-production.c326icw2wbit.ca-central-1.rds.amazonaws.com;Port=5432;Database=SqordiaDb;Username=sqordia_admin;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=true"

# Navigate to WebAPI project
cd src/WebAPI

# Run migrations
dotnet ef database update --project ../Infrastructure/Sqordia.Persistence
```

### 4. Using Docker (psql in container)

```bash
docker run -it --rm postgres:16-alpine psql \
  -h sqordia-db-production.c326icw2wbit.ca-central-1.rds.amazonaws.com \
  -p 5432 \
  -U sqordia_admin \
  -d SqordiaDb \
  --set=sslmode=require
```

### 5. Test Connection (PowerShell)

```powershell
# Test connection using Test-NetConnection (Windows)
Test-NetConnection -ComputerName sqordia-db-production.c326icw2wbit.ca-central-1.rds.amazonaws.com -Port 5432
```

## Security Notes

### SSL/TLS Connection

RDS requires SSL connections. Always use:
- `SSL Mode=Require` in connection strings
- `--set=sslmode=require` in psql commands

### Network Access

The RDS instance is in a **private subnet** and not publicly accessible by default. To connect:

1. **From AWS Services** (Lambda, Lightsail in same VPC): Direct connection works
2. **From Your Local Machine**: 
   - Use AWS Systems Manager Session Manager (if configured)
   - Use a bastion host/jump server
   - Or temporarily enable public access (not recommended for production)

### Current Network Configuration

The RDS security group allows connections from:
- The VPC CIDR block: `10.0.0.0/16`
- This means only resources within the VPC can connect directly

## Connection String for Application

### Environment Variable (Recommended)

Set in your deployment environment:

```bash
CONNECTION_STRING="Host=sqordia-db-production.c326icw2wbit.ca-central-1.rds.amazonaws.com;Port=5432;Database=SqordiaDb;Username=sqordia_admin;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=true"
```

### For Lambda Functions

Lambda functions already have the RDS endpoint configured via environment variables. They can connect using:

```csharp
var connectionString = $"Host={Environment.GetEnvironmentVariable("RDS_ENDPOINT")};" +
                      $"Port=5432;" +
                      $"Database={Environment.GetEnvironmentVariable("DATABASE_NAME")};" +
                      $"Username={Environment.GetEnvironmentVariable("DATABASE_USERNAME")};" +
                      $"Password={Environment.GetEnvironmentVariable("DATABASE_PASSWORD")};" +
                      $"SSL Mode=Require;Trust Server Certificate=true";
```

**Note**: You'll need to add `DATABASE_PASSWORD` to Lambda environment variables or retrieve it from AWS Secrets Manager.

## Troubleshooting

### Connection Timeout

- **Check Security Group**: Ensure your IP or VPC is allowed
- **Check RDS Status**: Ensure instance is `available` (not `backing-up` or `modifying`)
- **Check Network**: Ensure you're connecting from an allowed network

### Authentication Failed

- **Verify Username**: Should be `sqordia_admin`
- **Verify Password**: Check `TF_VAR_rds_password` environment variable
- **Check Database Name**: Should be `SqordiaDb`

### SSL Connection Required

- Always include `SSL Mode=Require` in connection strings
- For psql, use `--set=sslmode=require`

## Quick Connection Test

### Using psql

```bash
psql "host=sqordia-db-production.c326icw2wbit.ca-central-1.rds.amazonaws.com port=5432 dbname=SqordiaDb user=sqordia_admin sslmode=require"
```

### Using .NET

```csharp
using Npgsql;

var connectionString = "Host=sqordia-db-production.c326icw2wbit.ca-central-1.rds.amazonaws.com;Port=5432;Database=SqordiaDb;Username=sqordia_admin;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=true";

using var connection = new NpgsqlConnection(connectionString);
await connection.OpenAsync();
Console.WriteLine("Connected successfully!");
```

## Next Steps

1. ✅ Wait for RDS instance to be `available`
2. ✅ Test connection using one of the methods above
3. ✅ Run database migrations: `dotnet ef database update`
4. ✅ Verify connection in your application
5. ✅ Update Lambda environment variables if needed

