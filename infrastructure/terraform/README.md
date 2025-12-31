# Terraform Infrastructure for Sqordia AWS Deployment

This directory contains Terraform configuration files for provisioning AWS infrastructure for the Sqordia backend application.

## Architecture

The Terraform configuration provisions:

- **SQS Queues**: Email, AI Generation, and Document Export queues with dead-letter queues
- **Lambda Functions**: Handlers for processing jobs from SQS queues
- **IAM Roles & Policies**: Permissions for Lambda and Lightsail to access AWS services
- **CloudWatch Log Groups**: Logging for Lambda functions and API application (7-day retention)

**Note**: API keys (OpenAI, Claude, Gemini) are stored in the database Settings table (encrypted) instead of AWS Secrets Manager to save costs.

## Prerequisites

1. **AWS CLI configured** (see `docs/AWS_CLI_SETUP.md`)
2. **Terraform installed** (version >= 1.0)
   ```bash
   # Windows (using winget)
   winget install HashiCorp.Terraform
   
   # Or download from: https://www.terraform.io/downloads
   ```

3. **AWS credentials** with appropriate permissions (see `docs/AWS_CLI_SETUP.md`)

## File Structure

```
infrastructure/terraform/
├── main.tf              # Provider configuration
├── variables.tf         # Input variables
├── outputs.tf          # Output values
├── sqs.tf              # SQS queues configuration
├── lambda.tf           # Lambda functions configuration
├── iam.tf              # IAM roles and policies
├── cloudwatch.tf       # CloudWatch log groups
├── terraform.tfvars.example  # Example variables file
└── README.md           # This file
```

## Quick Start

### 1. Initialize Terraform

```bash
cd infrastructure/terraform
terraform init
```

### 2. Configure Variables

Copy the example variables file and update with your values:

```bash
cp terraform.tfvars.example terraform.tfvars
```

Edit `terraform.tfvars` and set:
- `aws_region`
- `environment`
- `rds_endpoint` (after RDS is created)
- `s3_bucket_name` (after S3 is created)
- Other configuration values

### 3. Review Plan

```bash
terraform plan
```

This will show you what resources will be created.

### 4. Apply Configuration

```bash
terraform apply
```

Type `yes` when prompted to create the resources.

### 5. Get Output Values

After applying, get the queue URLs and other outputs:

```bash
terraform output
```

Or get specific outputs:

```bash
terraform output email_queue_url
terraform output ai_generation_queue_url
terraform output export_queue_url
```

## Setting Up API Keys

API keys are now stored in the database Settings table (encrypted) instead of AWS Secrets Manager.

**To set API keys:**

1. Use the Settings API (after deployment):
   ```bash
   POST /api/v1/settings/secrets/AI:OpenAI:ApiKey
   {
     "secret": "your-openai-api-key",
     "category": "AI",
     "description": "OpenAI API key for AI generation"
   }
   ```

2. Or directly in the database:
   ```sql
   INSERT INTO "Settings" ("Key", "Value", "Category", "SettingType", "DataType", "IsEncrypted", "IsPublic", "Created", "IsDeleted")
   VALUES ('AI:OpenAI:ApiKey', '<encrypted-value>', 'AI', 2, 1, true, false, NOW(), false);
   ```

**Note**: Values should be encrypted using the `SETTINGS_ENCRYPTION_KEY` environment variable.

## Lambda Function Deployment

**Note**: The Lambda functions need to be built and deployed separately. The Terraform configuration references deployment packages that need to be created.

### Building Lambda Functions

1. Create Lambda function projects (separate from main API)
2. Build and package as ZIP files
3. Update `lambda.tf` with correct paths to ZIP files
4. Or use Terraform's `archive_file` data source to create ZIPs automatically

### Example Lambda Deployment Package Structure

```
lambda-functions/
├── EmailHandler/
│   ├── EmailHandler.csproj
│   ├── Function.cs
│   └── ...
├── AIGenerationHandler/
│   ├── AIGenerationHandler.csproj
│   ├── Function.cs
│   └── ...
└── ExportHandler/
    ├── ExportHandler.csproj
    ├── Function.cs
    └── ...
```

## Integration with Main Infrastructure

This Lambda/SQS module should be integrated with:

1. **RDS Module**: Provides `rds_endpoint` variable
2. **S3 Module**: Provides `s3_bucket_name` variable
3. **Lightsail Module**: Uses `lightsail_sqs_role_arn` to send messages to queues

## Updating Infrastructure

After making changes to Terraform files:

```bash
terraform plan    # Review changes
terraform apply   # Apply changes
```

## Destroying Infrastructure

⚠️ **Warning**: This will delete all resources!

```bash
terraform destroy
```

## Troubleshooting

### Lambda Functions Not Triggering

1. Check CloudWatch Logs:
   ```bash
   aws logs tail /aws/lambda/sqordia-email-handler-production --follow
   ```

2. Verify event source mappings:
   ```bash
   aws lambda list-event-source-mappings --function-name sqordia-email-handler-production
   ```

3. Check SQS queue visibility timeout matches Lambda timeout

### Permission Errors

1. Verify IAM roles have correct policies attached
2. Check Lambda execution role has access to required services
3. Ensure Secrets Manager secrets are accessible

### Queue Messages Not Processing

1. Check dead-letter queues for failed messages
2. Review Lambda function logs in CloudWatch
3. Verify message format matches Lambda handler expectations

## Cost Monitoring

Monitor costs via AWS Cost Explorer:
- Lambda invocations and duration
- SQS requests
- CloudWatch Logs storage

Expected cost: **$0-2/month** for small SaaS (within free tier)

## Next Steps

1. ✅ Create Lambda function projects
2. ✅ Build and deploy Lambda functions
3. ✅ Update API to send jobs to SQS queues
4. ✅ Test end-to-end flow
5. ✅ Set up monitoring and alerts

## Additional Resources

- [AWS Lambda Documentation](https://docs.aws.amazon.com/lambda/)
- [AWS SQS Documentation](https://docs.aws.amazon.com/sqs/)
- [Terraform AWS Provider Documentation](https://registry.terraform.io/providers/hashicorp/aws/latest/docs)

