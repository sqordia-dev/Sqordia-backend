# Deploy Docker Image to AWS ECR
# This script builds and pushes the Docker image to AWS ECR for Lightsail deployment

Write-Host "`n=== Deploying to AWS ECR ===" -ForegroundColor Cyan

# Step 1: Build Docker Image
Write-Host "`n[Step 1/4] Building Docker image..." -ForegroundColor Yellow
docker build -t sqordia-api:latest .

if ($LASTEXITCODE -ne 0) {
    Write-Host "`n❌ Docker build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Docker image built successfully" -ForegroundColor Green

# Step 2: Get AWS Account ID
Write-Host "`n[Step 2/4] Getting AWS account ID..." -ForegroundColor Yellow
$accountId = aws sts get-caller-identity --query Account --output text

if ($LASTEXITCODE -ne 0) {
    Write-Host "`n❌ Failed to get AWS account ID. Make sure AWS CLI is configured." -ForegroundColor Red
    exit 1
}

Write-Host "✅ Account ID: $accountId" -ForegroundColor Green

# Step 3: Login to ECR
Write-Host "`n[Step 3/4] Logging into AWS ECR..." -ForegroundColor Yellow
$ecrUrl = "$accountId.dkr.ecr.ca-central-1.amazonaws.com"
aws ecr get-login-password --region ca-central-1 | docker login --username AWS --password-stdin $ecrUrl

if ($LASTEXITCODE -ne 0) {
    Write-Host "`n❌ Failed to login to ECR" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Logged into ECR successfully" -ForegroundColor Green

# Step 4: Create ECR Repository (if not exists)
Write-Host "`n[Step 4/5] Creating ECR repository (if not exists)..." -ForegroundColor Yellow
aws ecr create-repository --repository-name sqordia-api --region ca-central-1 2>&1 | Out-Null

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Repository created" -ForegroundColor Green
} else {
    Write-Host "ℹ️  Repository may already exist (this is OK)" -ForegroundColor Yellow
}

# Step 5: Tag and Push
Write-Host "`n[Step 5/5] Tagging and pushing image..." -ForegroundColor Yellow
$imageTag = "$ecrUrl/sqordia-api:latest"

docker tag sqordia-api:latest $imageTag

if ($LASTEXITCODE -ne 0) {
    Write-Host "`n❌ Failed to tag image" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Image tagged: $imageTag" -ForegroundColor Green

Write-Host "`nPushing to ECR (this may take a few minutes)..." -ForegroundColor Yellow
docker push $imageTag

if ($LASTEXITCODE -ne 0) {
    Write-Host "`n❌ Failed to push image to ECR" -ForegroundColor Red
    exit 1
}

Write-Host "`n✅ Successfully pushed to ECR!" -ForegroundColor Green
Write-Host "`n📦 Image URI: $imageTag" -ForegroundColor Cyan
Write-Host "`n🎯 Next Steps:" -ForegroundColor Yellow
Write-Host "   1. Go to AWS Lightsail Console" -ForegroundColor White
Write-Host "   2. Create container service" -ForegroundColor White
Write-Host "   3. Use this image: $imageTag" -ForegroundColor White
Write-Host "   4. See DEPLOY_TO_LIGHTSAIL.md for details`n" -ForegroundColor White

