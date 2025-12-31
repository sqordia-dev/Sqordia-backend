# Quick RDS Connection Guide

## Connection Details

- **Host**: `sqordia-db-production.c326icw2wbit.ca-central-1.rds.amazonaws.com`
- **Port**: `5432`
- **Database**: `SqordiaDb`
- **Username**: `sqordia_admin`
- **Password**: `EHYKQsGdokt6cUAL`

## ⚠️ Important: Private Subnet

The RDS database is in a **private VPC subnet**, which means:
- ❌ **Cannot connect directly** from your local machine
- ✅ **Can connect** from:
  - AWS Lambda functions (already configured)
  - AWS Lightsail instances in the same VPC
  - EC2 instances in the same VPC
  - AWS Systems Manager Session Manager (if configured)
  - Bastion host/jump server in the VPC

## Connection Methods

### 1. From Your Application (Already Configured ✅)

Your `appsettings.json` already has the connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=sqordia-db-production.c326icw2wbit.ca-central-1.rds.amazonaws.com;Port=5432;Database=SqordiaDb;Username=sqordia_admin;Password=EHYKQsGdokt6cUAL;SSL Mode=Require;Trust Server Certificate=true"
  }
}
```

**This will work when your app runs on:**
- AWS Lightsail (in the same VPC)
- AWS Lambda (already configured)
- Any AWS service in the same VPC

### 2. Using psql (If You Have Access)

```bash
psql -h sqordia-db-production.c326icw2wbit.ca-central-1.rds.amazonaws.com \
     -p 5432 \
     -U sqordia_admin \
     -d SqordiaDb \
     --set=sslmode=require
```

**Note**: This will only work if you're connecting from within the AWS VPC.

### 3. Using pgAdmin (GUI Tool)

1. Download pgAdmin: https://www.pgadmin.org/download/
2. Create new server connection:
   - **Name**: Sqordia Production
   - **Host**: `sqordia-db-production.c326icw2wbit.ca-central-1.rds.amazonaws.com`
   - **Port**: `5432`
   - **Database**: `SqordiaDb`
   - **Username**: `sqordia_admin`
   - **Password**: `EHYKQsGdokt6cUAL`
   - **SSL Mode**: Require

**Note**: This will only work if you're connecting from within the AWS VPC.

### 4. Test Connection Script

Run the test script (if you have VPC access):

```powershell
.\scripts\test-db-connection.ps1
```

### 5. From AWS CloudShell (If Available)

AWS CloudShell might have VPC access. Try:

```bash
psql -h sqordia-db-production.c326icw2wbit.ca-central-1.rds.amazonaws.com \
     -p 5432 \
     -U sqordia_admin \
     -d SqordiaDb \
     --set=sslmode=require
```

## Making RDS Publicly Accessible (Not Recommended for Production)

If you need to connect from your local machine for development, you can temporarily:

1. **Modify RDS Security Group** to allow your IP
2. **Enable Public Access** on the RDS instance

**⚠️ Security Warning**: Only do this for development/testing. Never in production!

### Steps to Enable Public Access:

1. Go to AWS Console → RDS → Databases
2. Select `sqordia-db-production`
3. Click "Modify"
4. Under "Connectivity", expand "Additional connectivity configuration"
5. Check "Publicly accessible"
6. Click "Continue" → "Apply immediately"

Then update the security group to allow your IP:
- Go to RDS → Security → VPC security groups
- Add inbound rule: PostgreSQL (5432) from your IP address

## Connection String Format

For use in applications:

```
Host=sqordia-db-production.c326icw2wbit.ca-central-1.rds.amazonaws.com;Port=5432;Database=SqordiaDb;Username=sqordia_admin;Password=EHYKQsGdokt6cUAL;SSL Mode=Require;Trust Server Certificate=true
```

## Running Migrations

Your application will automatically run migrations when it starts (if configured). Or manually:

```powershell
cd src/WebAPI
dotnet ef database update --project ../Infrastructure/Sqordia.Persistence
```

## Next Steps

1. ✅ Connection string is already configured in `appsettings.json`
2. ✅ Lambda functions have the RDS endpoint configured
3. ⏳ Deploy your application to AWS Lightsail to connect
4. ⏳ Or set up a bastion host for local access

## Full Documentation

See `docs/DATABASE_CONNECTION.md` for complete connection guide with all methods.

