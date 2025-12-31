# Quick Deployment Guide

## Deploy Backend to AWS ECS Fargate

### Option 1: Automated Script (Recommended)

Run the complete deployment script:

```powershell
.\scripts\complete-deployment.ps1
```

This script will:
1. ✅ Build Docker image
2. ✅ Login to ECR
3. ✅ Tag and push image
4. ✅ Apply Terraform changes

### Option 2: Manual Steps

If the script doesn't work, follow these steps:

#### Step 1: Build Docker Image

```powershell
docker build -t sqordia-api:latest .
```

**Expected output:** `Successfully built [image-id]`

#### Step 2: Login to ECR

```powershell
$accountId = aws sts get-caller-identity --query Account --output text
$ecrUrl = "$accountId.dkr.ecr.ca-central-1.amazonaws.com"
aws ecr get-login-password --region ca-central-1 | docker login --username AWS --password-stdin $ecrUrl
```

**Expected output:** `Login Succeeded`

#### Step 3: Tag and Push Image

```powershell
$accountId = aws sts get-caller-identity --query Account --output text
$ecrImage = "$accountId.dkr.ecr.ca-central-1.amazonaws.com/sqordia-api:latest"
docker tag sqordia-api:latest $ecrImage
docker push $ecrImage
```

**Expected output:** `latest: digest: sha256:...`

#### Step 4: Apply Terraform

```powershell
cd infrastructure\terraform
$env:TF_VAR_rds_password = "EHYKQsGdokt6cUAL"
terraform apply -auto-approve
```

**Expected output:** `Apply complete!`

### Step 5: Verify Deployment

```powershell
# Check ECS service status
aws ecs describe-services --cluster sqordia-cluster-production --services sqordia-api-production --region ca-central-1 --query 'services[0].{Running:runningCount,Desired:desiredCount,Status:status}' --output table

# Check recent logs
aws logs tail /ecs/sqordia-production --region ca-central-1 --since 10m --format short
```

### Get ECS Task Public IP

```powershell
.\scripts\get-ecs-task-ip.ps1
```

## Troubleshooting

### Build Fails

- **Error:** `error CS0128: A local variable or function named 'awsRegion' is already defined`
- **Fix:** Already fixed in `ConfigureServices.cs` - ensure latest code is pulled

### ECR Login Fails

- **Error:** `400 Bad Request`
- **Fix:** Ensure ECR repository exists:
  ```powershell
  aws ecr create-repository --repository-name sqordia-api --region ca-central-1
  ```

### Terraform Apply Fails

- **Error:** `Invalid region endpoint provided`
- **Fix:** Already fixed - ensure `appsettings.Production.json` has correct region

### ECS Tasks Failing

- **Check logs:**
  ```powershell
  aws logs tail /ecs/sqordia-production --region ca-central-1 --since 10m
  ```
- **Common issues:**
  - Secrets Manager permissions (already fixed)
  - Memory allocation (already increased to 1024MB)
  - Region configuration (already fixed)

## Current Status

✅ **Fixed Issues:**
- Duplicate `awsRegion` variable declaration
- AWS region configuration in ECS
- IAM permissions for Secrets Manager
- Memory allocation (1024MB)
- Health check removed (curl not available)

✅ **Ready to Deploy:**
- Docker image builds successfully
- ECR repository exists
- Terraform configuration complete
- ECS task definition configured

## Next Steps After Deployment

1. **Get Public IP:**
   ```powershell
   .\scripts\get-ecs-task-ip.ps1
   ```

2. **Test API:**
   ```powershell
   $ip = (.\scripts\get-ecs-task-ip.ps1 | Select-String -Pattern "\d+\.\d+\.\d+\.\d+" | Select-Object -First 1).Matches.Value
   curl http://$ip:8080/health
   ```

3. **Monitor Logs:**
   ```powershell
   aws logs tail /ecs/sqordia-production --region ca-central-1 --follow
   ```

