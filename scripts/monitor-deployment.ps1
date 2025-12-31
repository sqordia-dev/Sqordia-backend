# Monitor Deployment Progress

Write-Host "`n=== Deployment Monitor ===" -ForegroundColor Cyan

# Check Docker
Write-Host "`n[1] Checking Docker..." -ForegroundColor Yellow
try {
    $dockerVersion = docker --version 2>&1
    Write-Host "  ✅ Docker: $dockerVersion" -ForegroundColor Green
} catch {
    Write-Host "  ❌ Docker not found" -ForegroundColor Red
    exit 1
}

# Check if image exists
Write-Host "`n[2] Checking Docker image..." -ForegroundColor Yellow
$image = docker images sqordia-api:latest --format "{{.Repository}}:{{.Tag}}" 2>&1

if ($image -eq "sqordia-api:latest") {
    $imageInfo = docker images sqordia-api:latest --format "Size: {{.Size}} | Created: {{.CreatedAt}}"
    Write-Host "  ✅ Image found: sqordia-api:latest" -ForegroundColor Green
    Write-Host "  $imageInfo" -ForegroundColor Gray
    
    # Check AWS
    Write-Host "`n[3] Checking AWS CLI..." -ForegroundColor Yellow
    try {
        $accountId = aws sts get-caller-identity --query Account --output text
        Write-Host "  ✅ AWS Account: $accountId" -ForegroundColor Green
        
        # Check ECR login
        Write-Host "`n[4] Ready to push to ECR!" -ForegroundColor Green
        Write-Host "  Run: .\scripts\push-to-ecr.ps1`n" -ForegroundColor Cyan
    } catch {
        Write-Host "  ❌ AWS CLI not configured" -ForegroundColor Red
        Write-Host "  Run: aws configure`n" -ForegroundColor Yellow
    }
} else {
    Write-Host "  ⏳ Image not found yet" -ForegroundColor Yellow
    Write-Host "  Build may still be in progress..." -ForegroundColor Gray
    Write-Host "  Check Docker Desktop or wait a few minutes`n" -ForegroundColor Gray
}

# Check running containers
Write-Host "[5] Docker build processes:" -ForegroundColor Yellow
$buildProcesses = docker ps -a --filter "ancestor=sqordia-api:latest" --format "{{.Status}}" 2>&1
if ($buildProcesses) {
    Write-Host "  $buildProcesses" -ForegroundColor Gray
} else {
    Write-Host "  No active build containers" -ForegroundColor Gray
}

Write-Host "`n========================================" -ForegroundColor Cyan

