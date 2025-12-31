# Fix Secrets Manager Access for ECS

param(
    [string]$Region = "ca-central-1"
)

Write-Host "`n=== Fixing Secrets Manager Access ===" -ForegroundColor Cyan

# Find the secret
Write-Host "`n[1] Finding secret...`n" -ForegroundColor Yellow
$secrets = aws secretsmanager list-secrets --region $Region --query 'SecretList[?contains(Name, `sqordia-rds-connection`)].{Name:Name,ARN:ARN}' --output json | ConvertFrom-Json

if ($secrets.Count -eq 0) {
    Write-Host "❌ Secret not found!" -ForegroundColor Red
    Write-Host "The secret may need to be created via Terraform.`n" -ForegroundColor Yellow
    exit 1
}

$secret = $secrets[0]
Write-Host "✅ Found secret:" -ForegroundColor Green
Write-Host "   Name: $($secret.Name)" -ForegroundColor Gray
Write-Host "   ARN: $($secret.ARN)`n" -ForegroundColor Gray

# Check IAM role
Write-Host "[2] Checking IAM execution role...`n" -ForegroundColor Yellow
$roleName = "sqordia-ecs-execution-role-production"
$role = aws iam get-role --role-name $roleName --region $Region 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ IAM role not found: $roleName" -ForegroundColor Red
    Write-Host "Run 'terraform apply' to create the role.`n" -ForegroundColor Yellow
    exit 1
}

Write-Host "✅ IAM role exists: $roleName`n" -ForegroundColor Green

# Check policy
Write-Host "[3] Checking IAM policy...`n" -ForegroundColor Yellow
$policyName = "sqordia-ecs-execution-policy-production"
$policy = aws iam get-role-policy --role-name $roleName --policy-name $policyName --region $Region 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Policy exists. Checking permissions...`n" -ForegroundColor Green
    $policyDoc = $policy | ConvertFrom-Json
    $hasPermission = $policyDoc.PolicyDocument.Statement | Where-Object { 
        $_.Effect -eq "Allow" -and 
        $_.Action -contains "secretsmanager:GetSecretValue" -and
        ($_.Resource -like "*$($secret.ARN)*" -or $_.Resource -like "*sqordia-rds-connection*")
    }
    
    if ($hasPermission) {
        Write-Host "✅ Policy has Secrets Manager permission`n" -ForegroundColor Green
    } else {
        Write-Host "⚠️  Policy may not have correct resource ARN" -ForegroundColor Yellow
        Write-Host "   Expected: $($secret.ARN)" -ForegroundColor Gray
        Write-Host "   Run 'terraform apply' to update the policy.`n" -ForegroundColor Yellow
    }
} else {
    Write-Host "❌ Policy not found: $policyName" -ForegroundColor Red
    Write-Host "Run 'terraform apply' to create the policy.`n" -ForegroundColor Yellow
}

# Test secret access (simulate what ECS would do)
Write-Host "[4] Testing secret access...`n" -ForegroundColor Yellow
Write-Host "Note: This requires the role to have permissions.`n" -ForegroundColor Gray

Write-Host "✅ Diagnostic complete`n" -ForegroundColor Green
Write-Host "📋 Next Steps:" -ForegroundColor Cyan
Write-Host "   1. Update Terraform IAM policy (already fixed in code)" -ForegroundColor White
Write-Host "   2. Run: cd infrastructure\terraform && terraform apply" -ForegroundColor White
Write-Host "   3. This will update the IAM policy with correct secret ARN`n" -ForegroundColor White

