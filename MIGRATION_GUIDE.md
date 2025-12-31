# Database Migration Guide

## Current Situation

Your RDS database is in a **private VPC subnet**, which means:
- ❌ Cannot run migrations directly from your local machine
- ✅ Migrations will run automatically when your app starts on AWS Lightsail
- ✅ Your app is configured to run migrations on startup

## Option 1: Automatic Migrations (Recommended) ✅

Your application is already configured to run migrations automatically when it starts!

**Location**: `src/WebAPI/Extensions/WebApplicationExtensions.cs`

```csharp
await app.ApplyDatabaseMigrationsAsync();
```

**What happens:**
1. When your app starts on AWS Lightsail (in the same VPC)
2. It will automatically connect to RDS
3. Run all pending migrations
4. Create the database schema

**No action needed** - just deploy your app to AWS Lightsail!

## Option 2: Enable Public Access (Development Only)

If you need to run migrations from your local machine for testing:

### Step 1: Enable Public Access on RDS

1. Go to **AWS Console** → **RDS** → **Databases**
2. Select `sqordia-db-production`
3. Click **"Modify"**
4. Scroll to **"Connectivity"** section
5. Expand **"Additional connectivity configuration"**
6. Check **"Publicly accessible"**
7. Click **"Continue"** → **"Apply immediately"**

⚠️ **Security Warning**: Only do this for development/testing!

### Step 2: Update Security Group

1. Go to **RDS** → **Security** → Click on the security group
2. Click **"Edit inbound rules"**
3. Add rule:
   - **Type**: PostgreSQL
   - **Port**: 5432
   - **Source**: Your IP address (or `0.0.0.0/0` for testing only)
4. Click **"Save rules"**

### Step 3: Run Migrations

```powershell
cd src/WebAPI
dotnet ef database update --project ..\Infrastructure\Sqordia.Persistence --startup-project .
```

### Step 4: Disable Public Access (After Migrations)

Once migrations are done, **disable public access** again for security:
1. Go back to RDS → Modify
2. Uncheck "Publicly accessible"
3. Apply changes

## Option 3: Run Migrations via Terraform

You can also create a migration script that runs from within AWS:

```powershell
# This would require setting up an EC2 instance or Lambda function
# More complex, but keeps RDS private
```

## Option 4: Use AWS Systems Manager Session Manager

If you have an EC2 instance in the VPC:
1. Connect via Session Manager
2. Install .NET SDK on the instance
3. Run migrations from there

## Recommended Approach

**For Production**: Use **Option 1** (Automatic Migrations)
- Deploy your app to AWS Lightsail
- Migrations run automatically on startup
- No manual intervention needed
- RDS stays private and secure

**For Development/Testing**: Use **Option 2** (Temporary Public Access)
- Enable public access temporarily
- Run migrations
- Disable public access immediately after

## Current Migration Status

Your connection string is configured in:
- ✅ `src/WebAPI/appsettings.json`
- ✅ `src/WebAPI/appsettings.Production.json`

Migrations will run automatically when:
- ✅ App starts on AWS Lightsail
- ✅ App can reach RDS (same VPC)
- ✅ Connection string is valid

## Next Steps

1. **Deploy to AWS Lightsail** → Migrations run automatically ✅
2. **OR** Enable public access temporarily → Run migrations → Disable public access

## Verify Migrations

After migrations run, you can verify by:
1. Connecting to the database
2. Checking for tables: `\dt` in psql
3. Or query: `SELECT table_name FROM information_schema.tables WHERE table_schema = 'public';`

