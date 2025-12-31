# PowerShell script to build and package Lambda functions for Terraform deployment
# Run this script before running terraform apply

$ErrorActionPreference = "Stop"

Write-Host "Building Lambda functions for deployment..." -ForegroundColor Green

$rootPath = Split-Path -Parent $PSScriptRoot
$lambdaPath = Join-Path $rootPath "src\Lambda"
$terraformPath = $PSScriptRoot

# Function to build and package a Lambda function
function Build-LambdaFunction {
    param(
        [string]$FunctionName,
        [string]$ProjectPath
    )

    Write-Host "`nBuilding $FunctionName..." -ForegroundColor Yellow

    $publishPath = Join-Path $ProjectPath "publish"
    
    # Clean previous build
    if (Test-Path $publishPath) {
        Remove-Item -Recurse -Force $publishPath
    }

    # Publish the Lambda function
    Push-Location $ProjectPath
    try {
        dotnet publish -c Release -o publish
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to publish $FunctionName"
        }
    }
    finally {
        Pop-Location
    }

    # Create ZIP file
    $zipPath = Join-Path $terraformPath "$FunctionName.zip"
    if (Test-Path $zipPath) {
        Remove-Item -Force $zipPath
    }

    Write-Host "Creating ZIP package: $zipPath" -ForegroundColor Cyan
    Compress-Archive -Path "$publishPath\*" -DestinationPath $zipPath -Force

    Write-Host "✓ $FunctionName built and packaged successfully" -ForegroundColor Green
}

# Build Email Handler
$emailHandlerPath = Join-Path $lambdaPath "EmailHandler\src\Sqordia.Lambda.EmailHandler"
if (Test-Path $emailHandlerPath) {
    Build-LambdaFunction -FunctionName "email-handler" -ProjectPath $emailHandlerPath
} else {
    Write-Warning "EmailHandler project not found at $emailHandlerPath"
}

# Build AI Generation Handler
$aiHandlerPath = Join-Path $lambdaPath "AIGenerationHandler\src\Sqordia.Lambda.AIGenerationHandler"
if (Test-Path $aiHandlerPath) {
    Build-LambdaFunction -FunctionName "ai-generation-handler" -ProjectPath $aiHandlerPath
} else {
    Write-Warning "AIGenerationHandler project not found at $aiHandlerPath"
}

# Build Export Handler
$exportHandlerPath = Join-Path $lambdaPath "ExportHandler\src\Sqordia.Lambda.ExportHandler"
if (Test-Path $exportHandlerPath) {
    Build-LambdaFunction -FunctionName "export-handler" -ProjectPath $exportHandlerPath
} else {
    Write-Warning "ExportHandler project not found at $exportHandlerPath"
}

Write-Host "`n✓ All Lambda functions built and packaged successfully!" -ForegroundColor Green
Write-Host "You can now run: terraform plan" -ForegroundColor Cyan

