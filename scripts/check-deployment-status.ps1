# Deployment Status Check Script

Write-Host "`n=== Deployment Status Check ===" -ForegroundColor Cyan

# 1. Docker Image Status
Write-Host "`n[1] Docker Image Status:" -ForegroundColor Yellow
$dockerImage = docker images sqordia-api --format "{{.Repository}}:{{.Tag}} - {{.Size}} - {{.CreatedAt}}" 2>&1
if ($dockerImage) {
    Write-Host "  ✅ Image found:" -ForegroundColor Green
    Write-Host "  $dockerImage" -ForegroundColor Gray
} else {
    Write-Host "  ❌ No image found locally" -ForegroundColor Red
}

# 2. ECR Image Status
Write-Host "`n[2] ECR Image Status:" -ForegroundColor Yellow
try {
    $ecrImages = aws ecr describe-images --repository-name sqordia-api --region ca-central-1 --query 'imageDetails[0].{Tags:imageTags,PushedAt:imagePushedAt}' --output json 2>&1 | ConvertFrom-Json
    if ($ecrImages.Tags) {
        Write-Host "  ✅ Image in ECR:" -ForegroundColor Green
        Write-Host "  Tags: $($ecrImages.Tags -join ', ')" -ForegroundColor Gray
        Write-Host "  Pushed: $($ecrImages.PushedAt)" -ForegroundColor Gray
    } else {
        Write-Host "  ⚠️  No images in ECR yet" -ForegroundColor Yellow
    }
} catch {
    Write-Host "  ⚠️  Could not check ECR (may not exist yet)" -ForegroundColor Yellow
}

# 3. ECS Service Status
Write-Host "`n[3] ECS Service Status:" -ForegroundColor Yellow
try {
    $service = aws ecs describe-services --cluster sqordia-cluster-production --services sqordia-api-production --region ca-central-1 --query 'services[0]' --output json 2>&1 | ConvertFrom-Json
    if ($service) {
        Write-Host "  Status: $($service.status)" -ForegroundColor $(if ($service.status -eq "ACTIVE") { "Green" } else { "Yellow" })
        Write-Host "  Running: $($service.runningCount) / Desired: $($service.desiredCount)" -ForegroundColor $(if ($service.runningCount -eq $service.desiredCount) { "Green" } else { "Yellow" })
        Write-Host "  Pending: $($service.pendingCount)" -ForegroundColor Gray
        Write-Host "  Task Definition: $($service.taskDefinition -replace '.*/', '')" -ForegroundColor Gray
        
        if ($service.events.Count -gt 0) {
            Write-Host "`n  Recent Events:" -ForegroundColor Gray
            $service.events | Select-Object -First 3 | ForEach-Object {
                Write-Host "    $($_.createdAt): $($_.message)" -ForegroundColor Gray
            }
        }
    } else {
        Write-Host "  ❌ Service not found" -ForegroundColor Red
    }
} catch {
    Write-Host "  ⚠️  Could not check ECS service: $_" -ForegroundColor Yellow
}

# 4. Recent Tasks
Write-Host "`n[4] Recent Task Status:" -ForegroundColor Yellow
try {
    $taskArns = aws ecs list-tasks --cluster sqordia-cluster-production --service-name sqordia-api-production --region ca-central-1 --query 'taskArns[0:2]' --output json 2>&1 | ConvertFrom-Json
    if ($taskArns -and $taskArns.Count -gt 0) {
        foreach ($taskArn in $taskArns) {
            if ($taskArn) {
                $task = aws ecs describe-tasks --cluster sqordia-cluster-production --tasks $taskArn --region ca-central-1 --query 'tasks[0]' --output json 2>&1 | ConvertFrom-Json
                if ($task) {
                    Write-Host "  Task: $($taskArn -replace '.*/', '')" -ForegroundColor Gray
                    Write-Host "    Status: $($task.lastStatus)" -ForegroundColor $(if ($task.lastStatus -eq "RUNNING") { "Green" } else { "Yellow" })
                    Write-Host "    Health: $($task.healthStatus)" -ForegroundColor Gray
                    if ($task.stoppedReason) {
                        Write-Host "    Stopped Reason: $($task.stoppedReason)" -ForegroundColor Red
                    }
                    Write-Host ""
                }
            }
        }
    } else {
        Write-Host "  ⚠️  No tasks found" -ForegroundColor Yellow
    }
} catch {
    Write-Host "  ⚠️  Could not check tasks: $_" -ForegroundColor Yellow
}

# 5. Recent Logs
Write-Host "[5] Recent CloudWatch Logs (last 10 minutes):" -ForegroundColor Yellow
try {
    $logs = aws logs tail /ecs/sqordia-production --region ca-central-1 --since 10m --format short 2>&1 | Select-Object -Last 10
    if ($logs) {
        Write-Host "  Recent log entries:" -ForegroundColor Gray
        $logs | ForEach-Object { Write-Host "    $_" -ForegroundColor Gray }
    } else {
        Write-Host "  ⚠️  No recent logs found" -ForegroundColor Yellow
    }
} catch {
    Write-Host "  ⚠️  Could not retrieve logs: $_" -ForegroundColor Yellow
}

Write-Host "`n=== Status Check Complete ===" -ForegroundColor Cyan
Write-Host "`nNext Steps:" -ForegroundColor Yellow
Write-Host "  • If image not built: docker build -t sqordia-api:latest ." -ForegroundColor White
Write-Host "  • If not in ECR: Push image using scripts/rebuild-and-push.ps1" -ForegroundColor White
Write-Host "  • If ECS not running: Check CloudWatch logs for errors" -ForegroundColor White
Write-Host "  • To deploy: Run scripts/complete-deployment.ps1`n" -ForegroundColor White

