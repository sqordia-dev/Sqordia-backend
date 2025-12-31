# Lambda Functions Deployment Guide

## Prerequisites

1. .NET 8 SDK installed
2. Terraform installed (>= 1.0)
3. AWS CLI configured
4. Lambda functions built and packaged

## Building Lambda Functions

Before deploying with Terraform, you need to build and package the Lambda functions.

### Windows (PowerShell)

```powershell
cd infrastructure/terraform
.\build-lambda.ps1
```

### Linux/Mac (Bash)

```bash
cd infrastructure/terraform
chmod +x build-lambda.sh
./build-lambda.sh
```

### Manual Build

If you prefer to build manually:

```bash
# Email Handler
cd src/Lambda/EmailHandler/src/Sqordia.Lambda.EmailHandler
dotnet publish -c Release -o publish
cd publish
zip -r ../../../../../../infrastructure/terraform/email-handler.zip .

# AI Generation Handler
cd src/Lambda/AIGenerationHandler/src/Sqordia.Lambda.AIGenerationHandler
dotnet publish -c Release -o publish
cd publish
zip -r ../../../../../../infrastructure/terraform/ai-generation-handler.zip .

# Export Handler
cd src/Lambda/ExportHandler/src/Sqordia.Lambda.ExportHandler
dotnet publish -c Release -o publish
cd publish
zip -r ../../../../../../infrastructure/terraform/export-handler.zip .
```

## Deployment Steps

### 1. Configure Variables

Copy and edit the example variables file:

```bash
cd infrastructure/terraform
cp terraform.tfvars.example terraform.tfvars
# Edit terraform.tfvars with your values
```

### 2. Build Lambda Functions

```bash
# Windows
.\build-lambda.ps1

# Linux/Mac
./build-lambda.sh
```

### 3. Initialize Terraform

```bash
terraform init
```

### 4. Review Plan

```bash
terraform plan
```

### 5. Apply Configuration

```bash
terraform apply
```

Type `yes` when prompted.

### 6. Verify Deployment

```bash
# Check Lambda functions
aws lambda list-functions --query 'Functions[?contains(FunctionName, `sqordia`)].FunctionName'

# Check SQS queues
aws sqs list-queues --queue-name-prefix sqordia

# Test Lambda function
aws lambda invoke --function-name sqordia-email-handler-production response.json
```

## Updating Lambda Functions

When you make changes to Lambda function code:

1. **Rebuild the functions**:
   ```bash
   .\build-lambda.ps1  # or ./build-lambda.sh
   ```

2. **Apply Terraform**:
   ```bash
   terraform apply
   ```

Terraform will detect the changed ZIP files (via `source_code_hash`) and update the Lambda functions.

## Handler Names

The Lambda handler names are:

- **Email Handler**: `Sqordia.Lambda.EmailHandler::Sqordia.Lambda.EmailHandler.Function::FunctionHandler`
- **AI Generation Handler**: `Sqordia.Lambda.AIGenerationHandler::Sqordia.Lambda.AIGenerationHandler.Function::FunctionHandler`
- **Export Handler**: `Sqordia.Lambda.ExportHandler::Sqordia.Lambda.ExportHandler.Function::FunctionHandler`

## Environment Variables

Lambda functions receive these environment variables:

### Email Handler
- `RDS_ENDPOINT`
- `DATABASE_NAME`
- `DATABASE_USERNAME`
- `SES_FROM_EMAIL`
- `SES_FROM_NAME`
- `AWS_REGION`
- `ENVIRONMENT`

### AI Generation Handler
- `RDS_ENDPOINT`
- `DATABASE_NAME`
- `DATABASE_USERNAME`
- `AWS_REGION`
- `ENVIRONMENT`
- `OPENAI_SECRET_NAME`
- `CLAUDE_SECRET_NAME`
- `GEMINI_SECRET_NAME`
- `DEFAULT_AI_PROVIDER`

### Export Handler
- `RDS_ENDPOINT`
- `DATABASE_NAME`
- `DATABASE_USERNAME`
- `S3_BUCKET_NAME`
- `AWS_REGION`
- `ENVIRONMENT`

## Troubleshooting

### Lambda Function Not Found

- Ensure ZIP files exist in `infrastructure/terraform/` directory
- Run `build-lambda.ps1` or `build-lambda.sh` to create them

### Handler Not Found Error

- Verify handler name matches the namespace in Function.cs
- Check that the assembly name matches the project name

### Timeout Errors

- Increase `timeout` in `lambda.tf` for long-running functions
- Check CloudWatch Logs for performance issues

### Permission Errors

- Verify IAM role has correct policies attached
- Check that Lambda can access RDS, S3, SES
- Verify API keys are stored in database Settings table (encrypted)

## CI/CD Integration

You can integrate Lambda building into your CI/CD pipeline:

```yaml
# GitHub Actions example
- name: Build Lambda Functions
  run: |
    cd infrastructure/terraform
    ./build-lambda.sh

- name: Terraform Apply
  run: |
    cd infrastructure/terraform
    terraform init
    terraform apply -auto-approve
```

## Next Steps

1. ✅ Lambda functions built
2. ✅ Terraform configuration updated
3. ⏳ Deploy infrastructure
4. ⏳ Test Lambda functions
5. ⏳ Integrate with main API

