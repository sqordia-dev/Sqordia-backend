# Complete deployment script: Build, Push, Deploy

Write-Host "`n=== Complete Deployment Process ===" -ForegroundColor Cyan
Write-Host "This will:" -ForegroundColor Yellow
Write-Host "  1. Build Docker image" -ForegroundColor Gray
Write-Host "  2. Login to ECR" -ForegroundColor Gray
Write-Host "  3. Tag and push image" -ForegroundColor Gray
Write-Host "  4. Apply Terraform changes`n" -ForegroundColor Gray

# Step 1: Build
Write-Host "[1/4] Building Docker image...`n" -ForegroundColor Yellow
docker build -t sqordia-api:latest . 2>&1 | Tee-Object -Variable buildOutput
if ($LASTEXITCODE -ne 0) {
    Write-Host "`n❌ Build failed:`n" -ForegroundColor Red
    $buildOutput | Select-String -Pattern "error|Error|ERROR" | Select-Object -Last 15
    exit 1
}
Write-Host "`n✅ Build successful!`n" -ForegroundColor Green

# Step 2: ECR Login
Write-Host "[2/4] Logging in to ECR...`n" -ForegroundColor Yellow
$accountId = aws sts get-caller-identity --query Account --output text
$ecrUrl = "$accountId.dkr.ecr.ca-central-1.amazonaws.com"
$password = aws ecr get-login-password --region ca-central-1
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to get ECR password`n" -ForegroundColor Red
    exit 1
}
$password | docker login --username AWS --password-stdin $ecrUrl 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ ECR login failed`n" -ForegroundColor Red
    exit 1
}
Write-Host "✅ ECR login successful!`n" -ForegroundColor Green

# Step 3: Tag and Push
Write-Host "[3/4] Tagging and pushing image...`n" -ForegroundColor Yellow
$ecrImage = "$accountId.dkr.ecr.ca-central-1.amazonaws.com/sqordia-api:latest"
docker tag sqordia-api:latest $ecrImage
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to tag image`n" -ForegroundColor Red
    exit 1
}
Write-Host "Pushing to ECR (this may take a few minutes)...`n" -ForegroundColor Gray
docker push $ecrImage 2>&1 | Tee-Object -Variable pushOutput
if ($LASTEXITCODE -ne 0) {
    Write-Host "`n❌ Push failed:`n" -ForegroundColor Red
    $pushOutput | Select-String -Pattern "error|Error|ERROR|denied" | Select-Object -Last 10
    exit 1
}
Write-Host "`n✅ Image pushed successfully!`n" -ForegroundColor Green

# Step 4: Terraform Apply
Write-Host "[4/4] Applying Terraform changes...`n" -ForegroundColor Yellow
Push-Location infrastructure\terraform
$env:TF_VAR_rds_password = "EHYKQsGdokt6cUAL"
Write-Host "Running terraform apply (this may take 5-10 minutes)...`n" -ForegroundColor Gray
terraform apply -auto-approve 2>&1 | Tee-Object -Variable tfOutput
Pop-Location

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✅ Deployment Complete!`n" -ForegroundColor Green
    Write-Host "=== Summary ===" -ForegroundColor Cyan
    Write-Host "✅ Docker image built and pushed" -ForegroundColor Green
    Write-Host "✅ ECS task definition updated" -ForegroundColor Green
    Write-Host "✅ New tasks deploying...`n" -ForegroundColor Green
    
    Write-Host "Monitor deployment status:" -ForegroundColor Yellow
    Write-Host "  aws ecs describe-services --cluster sqordia-cluster-production --services sqordia-api-production --region ca-central-1 --query 'services[0].{RunningCount:runningCount,DesiredCount:desiredCount}' --output table`n" -ForegroundColor White
    
    Write-Host "Check CloudWatch logs:" -ForegroundColor Yellow
    Write-Host "  aws logs tail /ecs/sqordia-production --region ca-central-1 --since 10m`n" -ForegroundColor White
} else {
    Write-Host "`n❌ Terraform apply failed:`n" -ForegroundColor Red
    $tfOutput | Select-String -Pattern "error|Error|ERROR" | Select-Object -Last 15
    exit 1
}
