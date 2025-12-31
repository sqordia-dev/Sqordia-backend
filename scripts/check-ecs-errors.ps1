# Check ECS Task Errors and Logs

param(
    [string]$Cluster = "sqordia-cluster-production",
    [string]$Service = "sqordia-api-production",
    [string]$Region = "ca-central-1"
)

Write-Host "`n=== ECS Service Diagnostics ===" -ForegroundColor Cyan
Write-Host "Cluster: $Cluster" -ForegroundColor Gray
Write-Host "Service: $Service" -ForegroundColor Gray
Write-Host "Region: $Region`n" -ForegroundColor Gray

# Check service events
Write-Host "[1] Recent Service Events:" -ForegroundColor Yellow
aws ecs describe-services --cluster $Cluster --services $Service --region $Region --query 'services[0].events[0:10]' --output table

# Check stopped tasks
Write-Host "`n[2] Recent Stopped Tasks:" -ForegroundColor Yellow
$stoppedTasks = aws ecs list-tasks --cluster $Cluster --service-name $Service --desired-status STOPPED --region $Region --query 'taskArns[0:5]' --output json | ConvertFrom-Json

if ($stoppedTasks.Count -gt 0) {
    Write-Host "Found $($stoppedTasks.Count) stopped task(s)`n" -ForegroundColor Gray
    foreach ($taskArn in $stoppedTasks) {
        $taskId = $taskArn.Split('/')[-1]
        Write-Host "Task: $taskId" -ForegroundColor Cyan
        $taskDetails = aws ecs describe-tasks --cluster $Cluster --tasks $taskArn --region $Region --query 'tasks[0]' --output json | ConvertFrom-Json
        
        Write-Host "  Status: $($taskDetails.lastStatus)" -ForegroundColor Gray
        Write-Host "  Stopped Reason: $($taskDetails.stoppedReason)" -ForegroundColor $(if ($taskDetails.stoppedReason -like '*Error*') { 'Red' } else { 'Yellow' })
        Write-Host "  Stopped At: $($taskDetails.stoppedAt)`n" -ForegroundColor Gray
        
        # Check container exit code
        if ($taskDetails.containers) {
            foreach ($container in $taskDetails.containers) {
                if ($container.exitCode) {
                    Write-Host "  Container Exit Code: $($container.exitCode)" -ForegroundColor $(if ($container.exitCode -ne 0) { 'Red' } else { 'Green' })
                }
                if ($container.reason) {
                    Write-Host "  Container Reason: $($container.reason)" -ForegroundColor Yellow
                }
            }
        }
        Write-Host ""
    }
} else {
    Write-Host "No stopped tasks found`n" -ForegroundColor Gray
}

# Check running tasks
Write-Host "[3] Running Tasks:" -ForegroundColor Yellow
$runningTasks = aws ecs list-tasks --cluster $Cluster --service-name $Service --desired-status RUNNING --region $Region --query 'taskArns' --output json | ConvertFrom-Json

if ($runningTasks.Count -gt 0) {
    Write-Host "Found $($runningTasks.Count) running task(s)`n" -ForegroundColor Green
    foreach ($taskArn in $runningTasks) {
        $taskId = $taskArn.Split('/')[-1]
        Write-Host "Task: $taskId" -ForegroundColor Cyan
        $taskDetails = aws ecs describe-tasks --cluster $Cluster --tasks $taskArn --region $Region --query 'tasks[0]' --output json | ConvertFrom-Json
        Write-Host "  Status: $($taskDetails.lastStatus)" -ForegroundColor Green
        Write-Host "  Health Status: $($taskDetails.healthStatus)" -ForegroundColor $(if ($taskDetails.healthStatus -eq 'HEALTHY') { 'Green' } else { 'Yellow' })
        Write-Host ""
    }
} else {
    Write-Host "No running tasks found`n" -ForegroundColor Yellow
}

# Check CloudWatch logs
Write-Host "[4] Recent CloudWatch Logs (last 20 lines):" -ForegroundColor Yellow
Write-Host "Log Group: /ecs/$($Cluster.Replace('-cluster-production', '-production'))`n" -ForegroundColor Gray
aws logs tail "/ecs/sqordia-production" --region $Region --since 1h --format short 2>&1 | Select-Object -Last 20

Write-Host "`n✅ Diagnostics complete`n" -ForegroundColor Green

