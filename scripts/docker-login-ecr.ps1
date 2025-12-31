# PowerShell script to login to AWS ECR

param(
    [string]$Region = "ca-central-1",
    [string]$RepositoryName = "sqordia-api"
)

Write-Host "`n=== AWS ECR Docker Login ===" -ForegroundColor Cyan

# Get AWS account ID
$accountId = aws sts get-caller-identity --query Account --output text
if (-not $accountId) {
    Write-Host "❌ Error: Could not get AWS account ID. Make sure AWS CLI is configured." -ForegroundColor Red
    exit 1
}

$ecrUrl = "$accountId.dkr.ecr.$Region.amazonaws.com"
Write-Host "Account ID: $accountId" -ForegroundColor Yellow
Write-Host "ECR URL: $ecrUrl`n" -ForegroundColor Yellow

# Check if repository exists
Write-Host "Checking if ECR repository exists..." -ForegroundColor Gray
$repoExists = aws ecr describe-repositories --repository-names $RepositoryName --region $Region 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Host "⚠️  Repository '$RepositoryName' not found. Creating it...`n" -ForegroundColor Yellow
    aws ecr create-repository `
        --repository-name $RepositoryName `
        --region $Region `
        --image-scanning-configuration scanOnPush=true `
        --image-tag-mutability MUTABLE
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Repository created successfully!`n" -ForegroundColor Green
    } else {
        Write-Host "❌ Error creating repository. Please check AWS permissions." -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "✅ Repository exists.`n" -ForegroundColor Green
}

# Get ECR login password and login
Write-Host "Logging in to ECR..." -ForegroundColor Yellow
$password = aws ecr get-login-password --region $Region

if ($LASTEXITCODE -eq 0) {
    $password | docker login --username AWS --password-stdin $ecrUrl
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Successfully logged in to ECR!`n" -ForegroundColor Green
        Write-Host "You can now push images using:" -ForegroundColor Cyan
        Write-Host "  docker tag sqordia-api:latest $ecrUrl/$RepositoryName`:latest" -ForegroundColor White
        Write-Host "  docker push $ecrUrl/$RepositoryName`:latest`n" -ForegroundColor White
    } else {
        Write-Host "❌ Docker login failed. Make sure Docker is running." -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "❌ Error getting ECR login password. Check AWS credentials." -ForegroundColor Red
    exit 1
}

