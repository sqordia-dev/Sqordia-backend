# Fix ECR Push 403 Forbidden Error

Write-Host "`n=== Fixing ECR Push Error ===" -ForegroundColor Cyan

# Step 1: Verify ECR repository exists
Write-Host "`n[1/3] Checking ECR repository...`n" -ForegroundColor Yellow
$accountId = aws sts get-caller-identity --query Account --output text
$ecrUrl = "$accountId.dkr.ecr.ca-central-1.amazonaws.com"

$repoCheck = aws ecr describe-repositories --repository-names sqordia-api --region ca-central-1 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "⚠️  Repository doesn't exist. Creating it...`n" -ForegroundColor Yellow
    aws ecr create-repository `
        --repository-name sqordia-api `
        --region ca-central-1 `
        --image-scanning-configuration scanOnPush=true `
        --image-tag-mutability MUTABLE 2>&1 | Out-Null
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Repository created!`n" -ForegroundColor Green
    } else {
        Write-Host "❌ Failed to create repository. Check IAM permissions.`n" -ForegroundColor Red
        Write-Host "Required IAM permissions:" -ForegroundColor Yellow
        Write-Host "  - ecr:CreateRepository" -ForegroundColor Gray
        Write-Host "  - ecr:GetAuthorizationToken" -ForegroundColor Gray
        Write-Host "  - ecr:BatchCheckLayerAvailability" -ForegroundColor Gray
        Write-Host "  - ecr:GetDownloadUrlForLayer" -ForegroundColor Gray
        Write-Host "  - ecr:BatchGetImage" -ForegroundColor Gray
        Write-Host "  - ecr:PutImage" -ForegroundColor Gray
        Write-Host "  - ecr:InitiateLayerUpload" -ForegroundColor Gray
        Write-Host "  - ecr:UploadLayerPart" -ForegroundColor Gray
        Write-Host "  - ecr:CompleteLayerUpload`n" -ForegroundColor Gray
        exit 1
    }
} else {
    Write-Host "✅ Repository exists`n" -ForegroundColor Green
}

# Step 2: Re-authenticate with ECR
Write-Host "[2/3] Re-authenticating with ECR...`n" -ForegroundColor Yellow
$password = aws ecr get-login-password --region ca-central-1
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to get ECR password. Check AWS credentials.`n" -ForegroundColor Red
    Write-Host "Run: aws configure" -ForegroundColor Yellow
    exit 1
}

$loginResult = $password | docker login --username AWS --password-stdin $ecrUrl 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ ECR login successful!`n" -ForegroundColor Green
} else {
    Write-Host "❌ ECR login failed:`n" -ForegroundColor Red
    $loginResult | Write-Host
    exit 1
}

# Step 3: Push image
Write-Host "[3/3] Pushing image to ECR...`n" -ForegroundColor Yellow
$ecrImage = "$ecrUrl/sqordia-api:latest"
Write-Host "Pushing: $ecrImage`n" -ForegroundColor Gray
Write-Host "This may take a few minutes...`n" -ForegroundColor Gray

docker push $ecrImage 2>&1 | Tee-Object -Variable pushOutput

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✅ Image pushed successfully!`n" -ForegroundColor Green
    Write-Host "=== Summary ===" -ForegroundColor Cyan
    Write-Host "✅ ECR repository verified/created" -ForegroundColor Green
    Write-Host "✅ ECR authentication successful" -ForegroundColor Green
    Write-Host "✅ Docker image pushed to ECR`n" -ForegroundColor Green
    
    Write-Host "Next: Apply Terraform to deploy" -ForegroundColor Yellow
    Write-Host "  cd infrastructure\terraform" -ForegroundColor White
    Write-Host "  `$env:TF_VAR_rds_password = 'EHYKQsGdokt6cUAL'" -ForegroundColor White
    Write-Host "  terraform apply -auto-approve`n" -ForegroundColor White
} else {
    Write-Host "`n❌ Push failed:`n" -ForegroundColor Red
    $pushOutput | Select-String -Pattern "error|Error|ERROR|403|Forbidden|denied" | Select-Object -Last 10
    
    Write-Host "`nTroubleshooting:" -ForegroundColor Yellow
    Write-Host "1. Verify IAM permissions (see above)" -ForegroundColor Gray
    Write-Host "2. Check AWS credentials: aws sts get-caller-identity" -ForegroundColor Gray
    Write-Host "3. Ensure repository exists: aws ecr describe-repositories --repository-names sqordia-api --region ca-central-1" -ForegroundColor Gray
    exit 1
}

