# Lambda Functions - .NET 8 Implementation

This directory contains AWS Lambda functions for processing asynchronous jobs from SQS queues.

## Projects

1. **EmailHandler** - Processes email jobs and sends via AWS SES
2. **AIGenerationHandler** - Processes AI business plan generation jobs
3. **ExportHandler** - Processes document export jobs (PDF/Word)

## Architecture

All Lambda functions follow .NET 8 best practices:

- ✅ **Dependency Injection** - Microsoft.Extensions.DependencyInjection
- ✅ **Configuration** - IOptions pattern with environment variables
- ✅ **Structured Logging** - ILogger<T> with proper log levels
- ✅ **Error Handling** - Try-catch with proper exception handling
- ✅ **Async/Await** - All I/O operations are async
- ✅ **Nullable Reference Types** - Enabled for type safety
- ✅ **Ready-to-Run** - Compiled for faster cold starts

## Project Structure

Each Lambda function follows this structure:

```
Sqordia.Lambda.{Handler}/
├── Function.cs                    # Lambda entry point
├── Startup.cs                     # DI configuration
├── Configuration/
│   └── LambdaConfiguration.cs    # Configuration class
├── Models/
│   └── {Job}Message.cs           # SQS message models
└── Services/
    ├── I{Service}.cs              # Service interface
    └── {Service}.cs               # Service implementation
```

## Building and Deploying

### Build Lambda Functions

```bash
# Email Handler
cd src/Lambda/EmailHandler/src/Sqordia.Lambda.EmailHandler
dotnet publish -c Release -o publish

# AI Generation Handler
cd src/Lambda/AIGenerationHandler/src/Sqordia.Lambda.AIGenerationHandler
dotnet publish -c Release -o publish

# Export Handler
cd src/Lambda/ExportHandler/src/Sqordia.Lambda.ExportHandler
dotnet publish -c Release -o publish
```

### Package for Deployment

```bash
# Create ZIP files
cd publish
zip -r ../../email-handler.zip .
```

### Deploy via Terraform

The Terraform configuration in `infrastructure/terraform/` will deploy these Lambda functions. Update `lambda.tf` with the correct paths to the ZIP files.

## Environment Variables

Lambda functions require these environment variables (set in Terraform):

- `RDS_ENDPOINT` - RDS PostgreSQL endpoint
- `DATABASE_NAME` - Database name
- `DATABASE_USERNAME` - Database username
- `SES_FROM_EMAIL` - SES sender email (EmailHandler only)
- `SES_FROM_NAME` - SES sender name (EmailHandler only)
- `S3_BUCKET_NAME` - S3 bucket for exports (ExportHandler only)
- `AWS_REGION` - AWS region
- `ENVIRONMENT` - Environment name

## Message Formats

### Email Job Message

```json
{
  "jobId": "guid",
  "emailType": "welcome|verification|password-reset",
  "toEmail": "user@example.com",
  "toName": "User Name",
  "subject": "Email Subject",
  "body": "Plain text body",
  "htmlBody": "<p>HTML body</p>",
  "metadata": {}
}
```

### AI Generation Job Message

```json
{
  "jobId": "guid",
  "businessPlanId": "guid",
  "planType": "standard|obnl",
  "language": "fr|en",
  "sections": ["executive-summary", "market-analysis"]
}
```

### Export Job Message

```json
{
  "jobId": "guid",
  "businessPlanId": "guid",
  "exportType": "pdf|word|excel",
  "language": "fr|en",
  "template": "default"
}
```

## Testing Locally

Use AWS SAM CLI or LocalStack for local testing:

```bash
# Install AWS SAM CLI
# Test Lambda function
sam local invoke EmailHandlerFunction -e event.json
```

## Monitoring

- **CloudWatch Logs**: `/aws/lambda/{function-name}`
- **CloudWatch Metrics**: Invocations, Duration, Errors
- **Dead-Letter Queues**: Check for failed messages

## Best Practices

See `docs/LAMBDA_BEST_PRACTICES.md` for detailed best practices documentation.

## Next Steps

1. ✅ Lambda functions created with best practices
2. ⏳ Implement business logic in service classes
3. ⏳ Add database access (if needed)
4. ⏳ Add AI service integration (AIGenerationHandler)
5. ⏳ Add document generation (ExportHandler)
6. ⏳ Build and package functions
7. ⏳ Deploy via Terraform

