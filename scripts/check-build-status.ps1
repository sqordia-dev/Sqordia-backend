# Check Docker Build Status

Write-Host "`n=== Checking Docker Build Status ===" -ForegroundColor Cyan

# Check if image exists
$image = docker images sqordia-api:latest --format "{{.Repository}}:{{.Tag}}" 2>&1

if ($image -eq "sqordia-api:latest") {
    Write-Host "`n✅ Docker image built successfully!" -ForegroundColor Green
    docker images sqordia-api:latest --format "Image: {{.Repository}}:{{.Tag}} | Size: {{.Size}} | Created: {{.CreatedAt}}"
    
    Write-Host "`nReady to push to ECR. Run:" -ForegroundColor Yellow
    Write-Host "  .\scripts\push-to-ecr.ps1`n" -ForegroundColor Green
} else {
    Write-Host "`n⏳ Docker image not found yet. Build may still be in progress." -ForegroundColor Yellow
    Write-Host "`nCheck Docker Desktop or run:" -ForegroundColor White
    Write-Host "  docker images sqordia-api:latest`n" -ForegroundColor Gray
}

