# ECS Task Exit Code 139 (Segmentation Fault) - Root Cause Analysis

## 🔍 Problem Summary

ECS tasks are failing with **Exit Code 139** (SIGSEGV - Segmentation Fault), indicating a critical memory access violation that causes the application to crash during startup.

## 📊 Current Status

- **DesiredCount**: 1
- **RunningCount**: 0
- **Task Status**: Failing immediately after start
- **Error Code**: 139 (Segmentation Fault)

## 🔎 Root Causes Identified

### 1. **Dockerfile HEALTHCHECK Issue** ✅ FIXED
- **Problem**: Dockerfile contains a HEALTHCHECK using `curl`, which is **not available** in the .NET runtime image (`mcr.microsoft.com/dotnet/aspnet:8.0`)
- **Impact**: Can cause container initialization failures
- **Fix**: Removed HEALTHCHECK from Dockerfile (ECS Fargate doesn't use Dockerfile HEALTHCHECK anyway)

### 2. **Secrets Manager Access Failure** ⚠️ POTENTIAL ISSUE
- **Problem**: If Secrets Manager fails to retrieve the connection string, the application may start without a valid connection string
- **Impact**: Database migrations fail, potentially causing application crash
- **Current Configuration**: 
  - ECS task definition injects secret as: `ConnectionStrings__DefaultConnection`
  - ASP.NET Core should map this to `ConnectionStrings:DefaultConnection`
  - If secret retrieval fails, environment variable is not set

### 3. **Database Migration Failure** ⚠️ POTENTIAL ISSUE
- **Problem**: `ApplyDatabaseMigrationsAsync()` runs during startup and may fail if:
  - Connection string is missing/invalid
  - Database is unreachable
  - Network connectivity issues
- **Impact**: Unhandled exception during startup can cause segfault
- **Current Error Handling**: Migrations have try-catch, but exceptions are logged and application continues

### 4. **Memory Allocation** ✅ ADDRESSED
- **Previous**: 512 MB (insufficient)
- **Current**: 1024 MB (1 GB) - should be sufficient for .NET 8 application
- **CPU**: Increased from 0.25 vCPU to 0.5 vCPU

## 🔧 Fixes Applied

### 1. Dockerfile
```dockerfile
# Health check removed - curl is not available in .NET runtime image
# ECS Fargate will use application-level health checks via /health endpoint
```

### 2. ECS Task Definition
- ✅ Memory: 512 MB → 1024 MB
- ✅ CPU: 0.25 vCPU → 0.5 vCPU
- ✅ IAM Permissions: Added `secretsmanager:DescribeSecret`
- ✅ Health Check: Removed (curl not available)

### 3. IAM Permissions
```terraform
# Added DescribeSecret permission
"secretsmanager:DescribeSecret"
```

## 🚀 Next Steps

### Immediate Actions

1. **Rebuild Docker Image**
   ```bash
   docker build -t sqordia-api:latest .
   docker tag sqordia-api:latest <account-id>.dkr.ecr.ca-central-1.amazonaws.com/sqordia-api:latest
   docker push <account-id>.dkr.ecr.ca-central-1.amazonaws.com/sqordia-api:latest
   ```

2. **Apply Terraform Changes**
   ```bash
   cd infrastructure/terraform
   set TF_VAR_rds_password=EHYKQsGdokt6cUAL
   terraform apply
   ```

3. **Verify Secrets Manager Access**
   ```bash
   aws secretsmanager get-secret-value \
     --secret-id sqordia-rds-connection-string-production \
     --region ca-central-1
   ```

4. **Check CloudWatch Logs**
   ```bash
   aws logs tail /ecs/sqordia-production --region ca-central-1 --since 1h
   ```

### Diagnostic Commands

```powershell
# Check ECS service status
aws ecs describe-services \
  --cluster sqordia-cluster-production \
  --services sqordia-api-production \
  --region ca-central-1 \
  --query 'services[0].{RunningCount:runningCount,DesiredCount:desiredCount,Events:events[0:3].message}' \
  --output json

# Check stopped tasks
aws ecs list-tasks \
  --cluster sqordia-cluster-production \
  --service-name sqordia-api-production \
  --desired-status STOPPED \
  --region ca-central-1

# Get task details
aws ecs describe-tasks \
  --cluster sqordia-cluster-production \
  --tasks <task-arn> \
  --region ca-central-1 \
  --query 'tasks[0].{StoppedReason:stoppedReason,ExitCode:containers[0].exitCode,Reason:containers[0].reason}'

# Check CloudWatch logs
aws logs tail /ecs/sqordia-production --region ca-central-1 --since 2h --format short
```

## 📝 Application Startup Sequence

1. **Program.cs** starts
2. **Serilog** bootstrap logger initialized
3. **WebApplication.CreateBuilder()** - reads configuration
4. **Secrets Manager** injects `ConnectionStrings__DefaultConnection` (if successful)
5. **Dependency Injection** configured
6. **ApplyDatabaseMigrationsAsync()** - runs migrations
7. **LoadCriticalSettingsAsync()** - loads settings from DB
8. **ConfigureMiddleware()** - sets up middleware
9. **app.Run()** - starts Kestrel server

## ⚠️ Potential Failure Points

1. **Secrets Manager Failure**: If secret cannot be retrieved, connection string is missing
2. **Database Connection Failure**: If RDS is unreachable, migrations fail
3. **Migration Failure**: If migrations throw unhandled exception, app crashes
4. **Settings Loading Failure**: If database is unavailable, settings loading fails

## 🎯 Expected Resolution

After applying fixes:
1. Docker image rebuilt without HEALTHCHECK
2. ECS task definition updated with increased resources
3. IAM permissions fixed for Secrets Manager
4. Tasks should start successfully within 2-3 minutes

## 📊 Monitoring

After deployment, monitor:
- **CloudWatch Logs**: `/ecs/sqordia-production`
- **ECS Service Metrics**: RunningCount, DesiredCount
- **Task Status**: Check for any new failures
- **Application Logs**: Look for startup messages

## 🔄 Rollback Plan

If issues persist:
1. Check CloudWatch logs for specific error messages
2. Verify Secrets Manager secret exists and is accessible
3. Test database connectivity from ECS subnet
4. Consider temporarily increasing memory to 2048 MB
5. Review application code for potential memory leaks

