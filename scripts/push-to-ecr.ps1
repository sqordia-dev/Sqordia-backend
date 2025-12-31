# Push Docker Image to AWS ECR

Write-Host "`n=== Pushing to AWS ECR ===" -ForegroundColor Cyan

# Check if image exists
$image = docker images sqordia-api:latest --format "{{.Repository}}:{{.Tag}}" 2>&1
if ($image -ne "sqordia-api:latest") {
    Write-Host "`n❌ Docker image 'sqordia-api:latest' not found!" -ForegroundColor Red
    Write-Host "Please build the image first: docker build -t sqordia-api:latest .`n" -ForegroundColor Yellow
    exit 1
}

Write-Host "✅ Image found: sqordia-api:latest`n" -ForegroundColor Green

# Get AWS Account ID
Write-Host "Getting AWS account ID..." -ForegroundColor Yellow
$accountId = aws sts get-caller-identity --query Account --output text

if ([string]::IsNullOrEmpty($accountId)) {
    Write-Host "`n❌ Failed to get AWS account ID. Is AWS CLI configured?" -ForegroundColor Red
    Write-Host "Run: aws configure`n" -ForegroundColor Yellow
    exit 1
}

Write-Host "✅ Account ID: $accountId`n" -ForegroundColor Green

# Login to ECR
Write-Host "Logging into ECR..." -ForegroundColor Yellow
$ecrUrl = "$accountId.dkr.ecr.ca-central-1.amazonaws.com"

aws ecr get-login-password --region ca-central-1 | docker login --username AWS --password-stdin $ecrUrl

if ($LASTEXITCODE -ne 0) {
    Write-Host "`n❌ ECR login failed" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Logged into ECR`n" -ForegroundColor Green

# Create repository
Write-Host "Creating ECR repository..." -ForegroundColor Yellow
aws ecr create-repository --repository-name sqordia-api --region ca-central-1 2>&1 | Out-Null

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Repository created" -ForegroundColor Green
} else {
    Write-Host "ℹ️  Repository already exists (this is OK)" -ForegroundColor Yellow
}

# Tag image
Write-Host "`nTagging image..." -ForegroundColor Yellow
$imageTag = "$ecrUrl/sqordia-api:latest"
docker tag sqordia-api:latest $imageTag

if ($LASTEXITCODE -ne 0) {
    Write-Host "`n❌ Failed to tag image" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Image tagged: $imageTag`n" -ForegroundColor Green

# Push image
Write-Host "Pushing to ECR (this may take a few minutes)..." -ForegroundColor Yellow
docker push $imageTag

if ($LASTEXITCODE -ne 0) {
    Write-Host "`n❌ Push failed" -ForegroundColor Red
    exit 1
}

Write-Host "`n✅ Successfully pushed to ECR!" -ForegroundColor Green
Write-Host "`n📦 Image URI: $imageTag" -ForegroundColor Cyan
Write-Host "`n🎯 Use this image URI in Lightsail deployment!`n" -ForegroundColor Yellow

