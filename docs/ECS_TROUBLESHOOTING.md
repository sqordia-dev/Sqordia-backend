# ECS Task Failure Troubleshooting

## 🔍 Diagnosing Failed Tasks

### Quick Diagnostic Commands

Run these commands in PowerShell or CMD to diagnose task failures:

#### 1. Check Service Events
```powershell
aws ecs describe-services --cluster sqordia-cluster-production --services sqordia-api-production --region ca-central-1 --query 'services[0].events[0:10]' --output table
```

#### 2. Check Stopped Tasks (Failed Tasks)
```powershell
# Get stopped task ARNs
$stoppedTasks = aws ecs list-tasks --cluster sqordia-cluster-production --service-name sqordia-api-production --desired-status STOPPED --region ca-central-1 --query 'taskArns[0:5]' --output json | ConvertFrom-Json

# Get details for each stopped task
foreach ($taskArn in $stoppedTasks) {
    aws ecs describe-tasks --cluster sqordia-cluster-production --tasks $taskArn --region ca-central-1 --query 'tasks[0].{StoppedReason:stoppedReason,LastStatus:lastStatus,ExitCode:containers[0].exitCode,Reason:containers[0].reason}' --output table
}
```

#### 3. Check Running Tasks
```powershell
aws ecs list-tasks --cluster sqordia-cluster-production --service-name sqordia-api-production --desired-status RUNNING --region ca-central-1
```

#### 4. Check CloudWatch Logs
```powershell
aws logs tail /ecs/sqordia-production --region ca-central-1 --since 2h --format short
```

#### 5. Use Diagnostic Script
```powershell
.\scripts\check-ecs-errors.ps1
```

## 🐛 Common Failure Causes

### 1. Health Check Failure
**Symptoms:**
- Tasks start then stop immediately
- Exit code: 1
- Reason: "Essential container in task exited"

**Cause:**
- Health check command fails (curl/wget not available)
- Health check endpoint not responding

**Fix:**
- ✅ Already fixed: Removed container health check from task definition
- Update task definition: `terraform apply`

### 2. Container Startup Error
**Symptoms:**
- Tasks fail to start
- Exit code: Non-zero
- Logs show application errors

**Common Causes:**
- Missing environment variables
- Database connection failed
- Secrets Manager access denied
- Application crash on startup

**Check:**
```powershell
aws logs tail /ecs/sqordia-production --region ca-central-1 --since 1h
```

### 3. Database Connection Issues
**Symptoms:**
- Tasks start but fail after timeout
- Logs show "Connection refused" or "Timeout"

**Check:**
- RDS security group allows traffic from ECS security group
- RDS endpoint is correct
- Secrets Manager secret is accessible

### 4. Secrets Manager Access Denied
**Symptoms:**
- Tasks fail immediately
- Exit code: 1
- Logs show "AccessDenied" errors

**Fix:**
- Check IAM role has `secretsmanager:GetSecretValue` permission
- Verify secret ARN is correct

### 5. Image Pull Errors
**Symptoms:**
- Tasks fail in PENDING state
- Reason: "CannotPullContainerError"

**Fix:**
- Verify ECR image exists
- Check ECS execution role has ECR permissions
- Verify image URI is correct

## 🔧 Quick Fixes

### Fix 1: Update Task Definition (Remove Health Check)
```powershell
cd infrastructure\terraform
terraform apply
```

### Fix 2: Check IAM Permissions
Verify ECS execution role has:
- ECR pull permissions
- Secrets Manager read permissions
- CloudWatch Logs write permissions

### Fix 3: Verify Security Groups
```powershell
# Check ECS security group allows outbound to RDS
aws ec2 describe-security-groups --group-ids <ecs-sg-id> --region ca-central-1

# Check RDS security group allows inbound from ECS
aws ec2 describe-security-groups --group-ids <rds-sg-id> --region ca-central-1
```

## 📊 Check Task Details in AWS Console

1. Go to: https://console.aws.amazon.com/ecs/
2. Select: **sqordia-cluster-production**
3. Click: **Services** → **sqordia-api-production**
4. Click: **Tasks** tab
5. Click on a **stopped** task
6. Check:
   - **Stopped reason**
   - **Container exit code**
   - **Container reason**
   - **Logs** tab

## ✅ Verification Steps

After fixing issues:

1. **Update task definition:**
   ```powershell
   cd infrastructure\terraform
   terraform apply
   ```

2. **Force new deployment:**
   ```powershell
   aws ecs update-service --cluster sqordia-cluster-production --service sqordia-api-production --force-new-deployment --region ca-central-1
   ```

3. **Monitor new tasks:**
   ```powershell
   aws ecs describe-services --cluster sqordia-cluster-production --services sqordia-api-production --region ca-central-1 --query 'services[0].events[0:5]' --output table
   ```

4. **Check logs:**
   ```powershell
   aws logs tail /ecs/sqordia-production --follow --region ca-central-1
   ```

## 🎯 Next Steps

1. Run diagnostic commands above
2. Identify the specific error from stopped tasks
3. Apply the appropriate fix
4. Update task definition if needed
5. Force new deployment to test fix

