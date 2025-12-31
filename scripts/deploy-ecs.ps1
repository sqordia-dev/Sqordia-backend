# Deploy ECS Fargate Infrastructure

param(
    [switch]$PlanOnly = $false
)

Set-Location "$PSScriptRoot\..\infrastructure\terraform"

Write-Host "`n=== ECS Fargate Deployment ===" -ForegroundColor Cyan
Write-Host "Working directory: $(Get-Location)`n" -ForegroundColor Gray

# Step 1: Initialize
Write-Host "[Step 1/3] Initializing Terraform..." -ForegroundColor Yellow
terraform init

if ($LASTEXITCODE -ne 0) {
    Write-Host "`n❌ Terraform init failed" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Terraform initialized`n" -ForegroundColor Green

# Step 2: Plan
Write-Host "[Step 2/3] Planning changes..." -ForegroundColor Yellow
terraform plan -out=tfplan

if ($LASTEXITCODE -ne 0) {
    Write-Host "`n❌ Terraform plan failed" -ForegroundColor Red
    exit 1
}

Write-Host "`n✅ Plan created successfully`n" -ForegroundColor Green

if ($PlanOnly) {
    Write-Host "📋 Plan-only mode. Review the plan above." -ForegroundColor Cyan
    Write-Host "To apply, run: terraform apply tfplan`n" -ForegroundColor Yellow
    exit 0
}

# Step 3: Apply
Write-Host "[Step 3/3] Applying changes (this will take 10-15 minutes)..." -ForegroundColor Yellow
Write-Host "This will create:" -ForegroundColor White
Write-Host "  • ECS Cluster" -ForegroundColor Gray
Write-Host "  • ECS Task Definition & Service" -ForegroundColor Gray
Write-Host "  • Public Subnets" -ForegroundColor Gray
Write-Host "  • Security Groups" -ForegroundColor Gray
Write-Host "  • IAM Roles" -ForegroundColor Gray
Write-Host "  • Secrets Manager Secret" -ForegroundColor Gray
Write-Host "  • CloudWatch Log Groups`n" -ForegroundColor Gray

$confirm = Read-Host "Do you want to proceed? (yes/no)"
if ($confirm -ne "yes") {
    Write-Host "`nDeployment cancelled.`n" -ForegroundColor Yellow
    exit 0
}

terraform apply tfplan

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✅ Deployment successful!`n" -ForegroundColor Green
    Write-Host "📋 Next steps:" -ForegroundColor Cyan
    Write-Host "  1. Get your ECS task public IP:" -ForegroundColor White
    Write-Host "     .\scripts\get-ecs-task-ip.ps1" -ForegroundColor Gray
    Write-Host "`n  2. Test your API:" -ForegroundColor White
    Write-Host "     curl http://<public-ip>:8080/api/health" -ForegroundColor Gray
    Write-Host "`n  3. View logs:" -ForegroundColor White
    Write-Host "     aws logs tail /ecs/sqordia-production --follow --region ca-central-1`n" -ForegroundColor Gray
} else {
    Write-Host "`n❌ Deployment failed. Check errors above.`n" -ForegroundColor Red
    exit 1
}

