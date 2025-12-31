# Get ECS Task Public IP

param(
    [string]$Cluster = "sqordia-cluster-production",
    [string]$Service = "sqordia-api-production",
    [string]$Region = "ca-central-1"
)

Write-Host "`n=== Getting ECS Task Public IP ===" -ForegroundColor Cyan
Write-Host "Cluster: $Cluster" -ForegroundColor Gray
Write-Host "Service: $Service" -ForegroundColor Gray
Write-Host "Region: $Region`n" -ForegroundColor Gray

# Get task ARN
Write-Host "Getting task ARN..." -ForegroundColor Yellow
$taskArn = aws ecs list-tasks --cluster $Cluster --service-name $Service --region $Region --query 'taskArns[0]' --output text

if ([string]::IsNullOrEmpty($taskArn)) {
    Write-Host "`n❌ No tasks found for service $Service" -ForegroundColor Red
    Write-Host "Make sure the service is running.`n" -ForegroundColor Yellow
    exit 1
}

Write-Host "✅ Task ARN: $taskArn`n" -ForegroundColor Green

# Get network interface ID
Write-Host "Getting network interface ID..." -ForegroundColor Yellow
$eniId = aws ecs describe-tasks --cluster $Cluster --tasks $taskArn --region $Region --query 'tasks[0].attachments[0].details[?name==`networkInterfaceId`].value' --output text

if ([string]::IsNullOrEmpty($eniId)) {
    Write-Host "`n❌ Could not get network interface ID" -ForegroundColor Red
    exit 1
}

# Get public IP
Write-Host "Getting public IP..." -ForegroundColor Yellow
$publicIp = aws ec2 describe-network-interfaces --network-interface-ids $eniId --region $Region --query 'NetworkInterfaces[0].Association.PublicIp' --output text

if ([string]::IsNullOrEmpty($publicIp)) {
    Write-Host "`n❌ Could not get public IP" -ForegroundColor Red
    exit 1
}

Write-Host "`n✅ Public IP: $publicIp" -ForegroundColor Green
Write-Host "`n🌐 Your API is available at:" -ForegroundColor Cyan
Write-Host "   http://$publicIp:8080/api" -ForegroundColor Yellow
Write-Host "   http://$publicIp:8080/api/health`n" -ForegroundColor Yellow

Write-Host "📝 Note: This IP will change if the task restarts." -ForegroundColor Gray
Write-Host "   For a stable endpoint, consider using an ALB (adds ~16/month).`n" -ForegroundColor Gray

