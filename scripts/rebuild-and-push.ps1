# Complete script to rebuild and push Docker image to ECR

Write-Host "`n=== Fresh Docker Build and Push ===" -ForegroundColor Cyan

# Step 1: Verify Docker
Write-Host "`n[1/5] Verifying Docker is running...`n" -ForegroundColor Yellow
docker ps 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Docker is not running. Please start Docker Desktop.`n" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Docker is running`n" -ForegroundColor Green

# Step 2: Verify AWS credentials
Write-Host "[2/5] Verifying AWS credentials...`n" -ForegroundColor Yellow
$identity = aws sts get-caller-identity --output json 2>&1 | ConvertFrom-Json
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ AWS credentials invalid. Run: aws configure`n" -ForegroundColor Red
    exit 1
}
Write-Host "✅ AWS credentials valid" -ForegroundColor Green
Write-Host "  Account ID: $($identity.Account)`n" -ForegroundColor Gray

# Step 3: Ensure ECR repository exists and login
Write-Host "[3/5] Ensuring ECR repository exists and logging in...`n" -ForegroundColor Yellow
$accountId = aws sts get-caller-identity --query Account --output text
$ecrUrl = "$accountId.dkr.ecr.ca-central-1.amazonaws.com"
Write-Host "Account ID: $accountId" -ForegroundColor Gray
Write-Host "ECR URL: $ecrUrl`n" -ForegroundColor Gray

$repoExists = aws ecr describe-repositories --repository-names sqordia-api --region ca-central-1 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "Creating ECR repository...`n" -ForegroundColor Yellow
    aws ecr create-repository --repository-name sqordia-api --region ca-central-1 --image-scanning-configuration scanOnPush=true --image-tag-mutability MUTABLE 2>&1 | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Repository created`n" -ForegroundColor Green
    } else {
        Write-Host "⚠️  Repository may already exist`n" -ForegroundColor Yellow
    }
} else {
    Write-Host "✅ Repository exists`n" -ForegroundColor Green
}

Write-Host "Logging in to ECR...`n" -ForegroundColor Yellow
$password = aws ecr get-login-password --region ca-central-1
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to get ECR login password`n" -ForegroundColor Red
    exit 1
}

$password | docker login --username AWS --password-stdin $ecrUrl 2>&1 | Out-Null
if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ ECR login successful`n" -ForegroundColor Green
} else {
    Write-Host "❌ ECR login failed`n" -ForegroundColor Red
    exit 1
}

# Step 4: Build Docker image
Write-Host "[4/5] Building Docker image (this may take a few minutes)...`n" -ForegroundColor Yellow
Write-Host "Building with:" -ForegroundColor Gray
Write-Host "  • Fixed Dockerfile (HEALTHCHECK removed)" -ForegroundColor Gray
Write-Host "  • Fixed AWS region configuration`n" -ForegroundColor Gray

docker build -t sqordia-api:latest . 2>&1 | Tee-Object -Variable buildOutput
if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✅ Docker image built successfully!`n" -ForegroundColor Green
} else {
    Write-Host "`n❌ Build failed. Errors:`n" -ForegroundColor Red
    $buildOutput | Select-String -Pattern "error|Error|ERROR" | Select-Object -Last 10
    exit 1
}

# Step 5: Tag and push
Write-Host "[5/5] Tagging and pushing to ECR...`n" -ForegroundColor Yellow
$ecrImage = "$accountId.dkr.ecr.ca-central-1.amazonaws.com/sqordia-api:latest"
Write-Host "Tagging: sqordia-api:latest -> $ecrImage`n" -ForegroundColor Gray

docker tag sqordia-api:latest $ecrImage
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to tag image`n" -ForegroundColor Red
    exit 1
}

Write-Host "Pushing to ECR (this may take a few minutes)...`n" -ForegroundColor Gray
docker push $ecrImage 2>&1 | Tee-Object -Variable pushOutput
if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✅ Image pushed successfully to ECR!`n" -ForegroundColor Green
    
    Write-Host "=== Summary ===" -ForegroundColor Cyan
    Write-Host "✅ Docker image built fresh" -ForegroundColor Green
    Write-Host "✅ Image pushed to ECR" -ForegroundColor Green
    Write-Host "✅ Ready for deployment`n" -ForegroundColor Green
    
    Write-Host "Next: Apply Terraform changes" -ForegroundColor Yellow
    Write-Host "  cd infrastructure\terraform" -ForegroundColor White
    Write-Host "  `$env:TF_VAR_rds_password = 'EHYKQsGdokt6cUAL'" -ForegroundColor White
    Write-Host "  terraform apply`n" -ForegroundColor White
} else {
    Write-Host "`n❌ Push failed:`n" -ForegroundColor Red
    $pushOutput | Select-String -Pattern "error|Error|ERROR|denied" | Select-Object -Last 10
    exit 1
}

