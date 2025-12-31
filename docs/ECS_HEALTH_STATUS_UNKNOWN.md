# ECS Health Status "Unknown" - Explanation

## 🔍 What "Unknown" Health Status Means

When ECS shows health status as **"Unknown"**, it means:
- The container is running (task status is "Running")
- **BUT** ECS cannot determine if the application is healthy
- This happens when **no health check is configured** at the container level

## ✅ This is Expected Behavior

In our current configuration:
- **Container-level health check**: Removed (curl not available in .NET runtime)
- **Service-level health monitoring**: ECS monitors task status only
- **Application health endpoint**: Available at `/health` but not used by ECS

## 📊 Current Status Interpretation

### Dashboard Shows:
- **Service Status**: Active ✅
- **Task Status**: Running ✅
- **Health Status**: Unknown ⚠️

### What This Means:
- ✅ Task is running (container started successfully)
- ✅ No immediate crashes (Exit Code 139 resolved)
- ⚠️ ECS can't verify application health automatically
- ⚠️ Need manual verification that app is responding

## 🔧 How to Verify Application is Actually Working

### Option 1: Get Task Public IP and Test

```powershell
# Get running task details
$taskArn = aws ecs list-tasks --cluster sqordia-cluster-production --service-name sqordia-api-production --desired-status RUNNING --region ca-central-1 --query 'taskArns[0]' --output text

# Get public IP
$publicIp = aws ecs describe-tasks --cluster sqordia-cluster-production --tasks $taskArn --region ca-central-1 --query 'tasks[0].attachments[0].details[?name==`publicIPv4Address`].value|[0]' --output text

# Test health endpoint
curl http://$publicIp:8080/health
```

### Option 2: Check CloudWatch Logs

```powershell
aws logs tail /ecs/sqordia-production --region ca-central-1 --since 10m --format short
```

Look for:
- `"Sqordia application started successfully"`
- `"Database migrations completed successfully"`
- Any error messages

### Option 3: Check Application Logs

```powershell
aws logs tail /ecs/sqordia-production --region ca-central-1 --since 10m --format short --filter-pattern "Starting\|started\|error\|Error"
```

## 🎯 Next Steps

### If Application is Working:
1. **Health Status "Unknown" is OK** - it's expected without container health checks
2. Monitor CloudWatch logs for any errors
3. Consider adding a proper health check (see below)

### If Application is Not Working:
1. Check CloudWatch logs for errors
2. Verify database connectivity
3. Check Secrets Manager access
4. Review application startup sequence

## 🔧 Adding Proper Health Check (Optional)

To get "Healthy" status instead of "Unknown", you have two options:

### Option A: Install curl in Dockerfile (Simple)

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# ... rest of Dockerfile
```

Then uncomment health check in `ecs.tf`:
```terraform
healthCheck = {
  command     = ["CMD-SHELL", "curl -f http://localhost:8080/health || exit 1"]
  interval    = 30
  timeout     = 5
  retries     = 3
  startPeriod = 120
}
```

### Option B: Use ALB Health Checks (Recommended for Production)

Add an Application Load Balancer with target group health checks:
- More reliable than container-level checks
- Can check from outside the container
- Better for production workloads
- Adds ~$16/month cost

## 📝 Summary

**"Unknown" health status is normal** when:
- Container health checks are disabled
- Task is running successfully
- Application needs manual verification

**To verify application is working:**
1. Get task public IP
2. Test `/health` endpoint
3. Check CloudWatch logs
4. Monitor for errors

**Current status suggests:**
- ✅ Segmentation fault (Exit Code 139) is resolved
- ✅ Tasks are starting successfully
- ⚠️ Need to verify application is responding

