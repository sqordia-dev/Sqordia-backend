# Terraform Deployment Instructions

## Quick Start Guide

### Step 1: Configure Variables

Create `terraform.tfvars` from the example:

```powershell
# Windows PowerShell
cd infrastructure/terraform
Copy-Item terraform.tfvars.example terraform.tfvars
```

Edit `terraform.tfvars` and set the required values:

**Required Variables:**
- `rds_endpoint` - Your RDS PostgreSQL endpoint (e.g., `sqordia-db.xxxxx.ca-central-1.rds.amazonaws.com`)
- `s3_bucket_name` - Your S3 bucket name (e.g., `sqordia-documents-production`)
- `ses_from_email` - Your verified SES email address

**Optional Variables (have defaults):**
- `aws_region` - Default: `ca-central-1`
- `environment` - Default: `production`
- `project_name` - Default: `sqordia`

### Step 2: Initialize Terraform

```powershell
cd infrastructure/terraform
terraform init
```

This downloads the AWS provider and initializes the backend.

### Step 3: Review Plan

```powershell
terraform plan
```

This shows what resources will be created without actually creating them.

**Expected Resources:**
- 3 SQS Queues (email, ai-generation, export) + 3 Dead-Letter Queues
- 3 Lambda Functions (email-handler, ai-generation-handler, export-handler)
- 4 CloudWatch Log Groups (3 for Lambda + 1 for API)
- 2 IAM Roles (Lambda execution, Lightsail SQS)
- Note: API keys stored in database Settings table (encrypted) - no Secrets Manager needed
- Various IAM policies

### Step 4: Apply Infrastructure

```powershell
terraform apply
```

Terraform will prompt you to confirm. Type `yes` to proceed.

**Note**: The Lambda functions will fail initially because the deployment packages don't exist yet. You'll need to build and deploy them separately (see `LAMBDA_DEPLOYMENT.md`).

### Step 5: Set API Keys in Database

API keys are stored in the database Settings table (encrypted). Use the Settings API:

```powershell
# After API is deployed, use the Settings API to store encrypted secrets
# POST /api/v1/settings/secrets/AI:OpenAI:ApiKey
# POST /api/v1/settings/secrets/AI:Claude:ApiKey
# POST /api/v1/settings/secrets/AI:Gemini:ApiKey
```

**Note**: Make sure `SETTINGS_ENCRYPTION_KEY` environment variable is set for encryption.

### Step 6: Build and Deploy Lambda Functions

See `LAMBDA_DEPLOYMENT.md` for instructions on building and deploying the Lambda functions.

## Troubleshooting

### Error: "No valid credential sources found"

**Solution**: Configure AWS credentials:
```powershell
aws configure
```

### Error: "Access Denied"

**Solution**: Ensure your IAM user/role has the required permissions:
- `AmazonLightsailFullAccess`
- `RDSFullAccess`
- `S3FullAccess`
- `SESFullAccess`
- `LambdaFullAccess`
- `SQSFullAccess`
- `CloudWatchLogsFullAccess`

### Error: "Lambda function deployment package not found"

**Solution**: This is expected. Build the Lambda packages first (see `LAMBDA_DEPLOYMENT.md`), then update the Lambda functions.

## What Gets Created

### SQS Queues
- `sqordia-email-queue-production`
- `sqordia-ai-generation-queue-production`
- `sqordia-export-queue-production`
- Plus 3 dead-letter queues

### Lambda Functions
- `sqordia-email-handler-production`
- `sqordia-ai-generation-handler-production`
- `sqordia-export-handler-production`

### CloudWatch Log Groups
- `/aws/lambda/sqordia-email-handler-production` (7-day retention)
- `/aws/lambda/sqordia-ai-generation-handler-production` (7-day retention)
- `/aws/lambda/sqordia-export-handler-production` (7-day retention)
- `/aws/sqordia/api` (7-day retention)

### IAM Roles
- `sqordia-lambda-execution-role-production`
- `sqordia-lightsail-sqs-role-production`

### Database Settings (API Keys)
- API keys stored in database Settings table (encrypted)
- Use Settings API: `/api/v1/settings/secrets/{key}`

## Cost Estimate

**Monthly Costs:**
- SQS: ~$0 (free tier: 1M requests/month)
- Lambda: ~$0-2/month (free tier: 1M requests, 400K GB-seconds)
- CloudWatch Logs: ~$0.30-1.20/month (with 7-day retention)
- Secrets Manager: $0/month (using database encryption instead)

**Total**: ~$0.30-2.30/month (within your $20/month budget)

## Next Steps

1. ✅ Create infrastructure (this guide)
2. ⏭️ Build Lambda deployment packages
3. ⏭️ Deploy Lambda functions
4. ⏭️ Configure Lightsail container service
5. ⏭️ Update API to use SQS queues

