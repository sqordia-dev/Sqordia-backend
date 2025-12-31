# Test RDS PostgreSQL Connection
# This script tests the connection to the RDS database

param(
    [string]$Password = $env:TF_VAR_rds_password
)

$endpoint = "sqordia-db-production.c326icw2wbit.ca-central-1.rds.amazonaws.com"
$port = 5432
$database = "SqordiaDb"
$username = "sqordia_admin"

Write-Host "`n=== Testing RDS Database Connection ===" -ForegroundColor Cyan
Write-Host "`nEndpoint: $endpoint" -ForegroundColor White
Write-Host "Port: $port" -ForegroundColor White
Write-Host "Database: $database" -ForegroundColor White
Write-Host "Username: $username" -ForegroundColor White

if ([string]::IsNullOrEmpty($Password)) {
    Write-Host "`nERROR: Password not provided!" -ForegroundColor Red
    Write-Host "Set the password using:" -ForegroundColor Yellow
    Write-Host "  `$env:TF_VAR_rds_password = 'YOUR_PASSWORD'" -ForegroundColor Green
    Write-Host "Or pass it as a parameter:" -ForegroundColor Yellow
    Write-Host "  .\test-db-connection.ps1 -Password 'YOUR_PASSWORD'" -ForegroundColor Green
    exit 1
}

# Test network connectivity
Write-Host "`n1. Testing network connectivity..." -ForegroundColor Cyan
try {
    $tcpClient = New-Object System.Net.Sockets.TcpClient
    $connection = $tcpClient.BeginConnect($endpoint, $port, $null, $null)
    $wait = $connection.AsyncWaitHandle.WaitOne(5000, $false)
    
    if ($wait) {
        $tcpClient.EndConnect($connection)
        Write-Host "   ✓ Network connection successful" -ForegroundColor Green
        $tcpClient.Close()
    } else {
        Write-Host "   ✗ Connection timeout (database may be in private subnet)" -ForegroundColor Yellow
        Write-Host "   Note: RDS is in a private subnet. You may need to connect from within AWS VPC." -ForegroundColor Gray
    }
} catch {
    Write-Host "   ✗ Connection failed: $_" -ForegroundColor Red
}

# Test using psql if available
Write-Host "`n2. Testing with psql (if installed)..." -ForegroundColor Cyan
$psqlPath = Get-Command psql -ErrorAction SilentlyContinue
if ($psqlPath) {
    $connectionString = "host=$endpoint port=$port dbname=$database user=$username sslmode=require"
    $env:PGPASSWORD = $Password
    
    try {
        $result = psql -h $endpoint -p $port -U $username -d $database -c "SELECT version();" 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "   ✓ Database connection successful!" -ForegroundColor Green
            Write-Host "   PostgreSQL Version:" -ForegroundColor Gray
            $result | Where-Object { $_ -match "PostgreSQL" } | ForEach-Object { Write-Host "     $_" -ForegroundColor Gray }
        } else {
            Write-Host "   ✗ Connection failed" -ForegroundColor Red
            Write-Host "   Error: $result" -ForegroundColor Red
        }
    } catch {
        Write-Host "   ✗ Error: $_" -ForegroundColor Red
    } finally {
        Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
    }
} else {
    Write-Host "   ⚠ psql not found. Install PostgreSQL client to test connection." -ForegroundColor Yellow
    Write-Host "   Download: https://www.postgresql.org/download/windows/" -ForegroundColor Gray
}

# Show connection string
Write-Host "`n3. Connection String for Application:" -ForegroundColor Cyan
$connectionString = "Host=$endpoint;Port=$port;Database=$database;Username=$username;Password=$Password;SSL Mode=Require;Trust Server Certificate=true"
Write-Host "   $connectionString" -ForegroundColor Gray

Write-Host "`n=== Connection Test Complete ===" -ForegroundColor Cyan

