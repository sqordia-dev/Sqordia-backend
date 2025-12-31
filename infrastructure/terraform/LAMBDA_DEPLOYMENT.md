# Lambda Function Deployment Guide

## Overview

The Terraform configuration references Lambda deployment packages (ZIP files) that need to be created separately. This guide explains how to build and deploy the Lambda functions.

## Lambda Functions Required

1. **Email Handler** - Processes email jobs from SQS and sends via SES
2. **AI Generation Handler** - Processes AI business plan generation jobs
3. **Export Handler** - Processes document export jobs (PDF/Word)

## Prerequisites

- .NET 8 SDK installed
- AWS CLI configured
- Terraform applied (creates Lambda function placeholders)

## Project Structure

Create separate Lambda function projects:

```
lambda-functions/
├── EmailHandler/
│   ├── EmailHandler.csproj
│   ├── Function.cs
│   └── Program.cs
├── AIGenerationHandler/
│   ├── AIGenerationHandler.csproj
│   ├── Function.cs
│   └── Program.cs
└── ExportHandler/
    ├── ExportHandler.csproj
    ├── Function.cs
    └── Program.cs
```

## Creating Lambda Function Projects

### 1. Email Handler

```bash
mkdir -p lambda-functions/EmailHandler
cd lambda-functions/EmailHandler
dotnet new classlib -n EmailHandler
```

### 2. AI Generation Handler

```bash
mkdir -p lambda-functions/AIGenerationHandler
cd lambda-functions/AIGenerationHandler
dotnet new classlib -n AIGenerationHandler
```

### 3. Export Handler

```bash
mkdir -p lambda-functions/ExportHandler
cd lambda-functions/ExportHandler
dotnet new classlib -n ExportHandler
```

## Lambda Function Template

Each Lambda function should follow this structure:

```csharp
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using System.Text.Json;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace EmailHandler;

public class Function
{
    public async Task FunctionHandler(SQSEvent sqsEvent, ILambdaContext context)
    {
        foreach (var record in sqsEvent.Records)
        {
            try
            {
                // Parse message body
                var message = JsonSerializer.Deserialize<EmailJob>(record.Body);
                
                // Process email job
                await ProcessEmailJob(message);
                
                // Message will be automatically deleted from queue on success
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"Error processing message: {ex.Message}");
                // Message will be retried or moved to DLQ
                throw;
            }
        }
    }
    
    private async Task ProcessEmailJob(EmailJob job)
    {
        // Implementation here
        // - Read from RDS if needed
        // - Send email via SES
        // - Update status in RDS
    }
}

public class EmailJob
{
    public string JobId { get; set; }
    public string EmailType { get; set; }
    public string ToEmail { get; set; }
    public string Subject { get; set; }
    public string Body { get; set; }
    // Add other properties as needed
}
```

## Building and Packaging

### Option 1: Manual ZIP Creation

```bash
# Build the project
dotnet publish -c Release -o publish

# Create ZIP file
cd publish
zip -r ../../email-handler.zip .
```

### Option 2: Using Terraform archive_file (Recommended)

Update `lambda.tf` to use `archive_file` data source:

```hcl
data "archive_file" "email_handler_zip" {
  type        = "zip"
  source_dir  = "${path.module}/../lambda-functions/EmailHandler/publish"
  output_path = "${path.module}/email-handler.zip"
}

resource "aws_lambda_function" "email_handler" {
  # ... other configuration ...
  filename         = data.archive_file.email_handler_zip.output_path
  source_code_hash = data.archive_file.email_handler_zip.output_base64sha256
  # ... rest of configuration ...
}
```

## Required NuGet Packages

Each Lambda function needs:

```xml
<ItemGroup>
  <PackageReference Include="Amazon.Lambda.Core" Version="2.2.0" />
  <PackageReference Include="Amazon.Lambda.SQSEvents" Version="2.2.0" />
  <PackageReference Include="Amazon.Lambda.Serialization.SystemTextJson" Version="2.2.0" />
  <PackageReference Include="AWSSDK.SimpleEmail" Version="3.7.400.0" />
  <PackageReference Include="Npgsql" Version="8.0.3" />
  <PackageReference Include="AWSSDK.S3" Version="3.7.400.0" />
  <!-- Note: Secrets Manager removed - API keys stored in database Settings table -->
</ItemGroup>
```

## Environment Variables

Lambda functions receive these environment variables (set in `lambda.tf`):

- `RDS_ENDPOINT` - RDS PostgreSQL endpoint
- `DATABASE_NAME` - Database name
- `S3_BUCKET_NAME` - S3 bucket for exports
- `SES_FROM_EMAIL` - SES sender email
- `AWS_REGION` - AWS region
- `ENVIRONMENT` - Environment name

## Accessing API Keys from Database

API keys are now stored in the database Settings table (encrypted) instead of Secrets Manager.

```csharp
// Connect to database and query Settings table
await using var conn = new NpgsqlConnection(connectionString);
await conn.OpenAsync();

var query = "SELECT \"Value\", \"IsEncrypted\" FROM \"Settings\" WHERE \"Key\" = @key AND \"IsDeleted\" = false";
await using var cmd = new NpgsqlCommand(query, conn);
cmd.Parameters.AddWithValue("key", "AI:OpenAI:ApiKey");

await using var reader = await cmd.ExecuteReaderAsync();
if (await reader.ReadAsync())
{
    var value = reader.GetString(0);
    var isEncrypted = reader.GetBoolean(1);
    var apiKey = isEncrypted ? DecryptValue(value) : value; // Decrypt if needed
}
```

## Database Connection

```csharp
using Npgsql;

// Get RDS password from environment variable or IAM authentication
var rdsEndpoint = Environment.GetEnvironmentVariable("RDS_ENDPOINT");
var databaseName = Environment.GetEnvironmentVariable("DATABASE_NAME");
var username = Environment.GetEnvironmentVariable("DATABASE_USERNAME") ?? "sqordia_admin";
var password = Environment.GetEnvironmentVariable("RDS_PASSWORD"); // Or use IAM auth

var connectionString = $"Host={rdsEndpoint};" +
                       $"Database={databaseName};" +
                       $"Username={username};" +
                       $"Password={password};" +
                       "SSL Mode=Require;";

await using var conn = new NpgsqlConnection(connectionString);
await conn.OpenAsync();

// Query Settings table for API keys
var apiKey = await GetSettingFromDatabaseAsync(conn, "AI:OpenAI:ApiKey");
```

## Testing Locally

Use AWS SAM or LocalStack for local testing:

```bash
# Install AWS SAM CLI
# Test Lambda function locally
sam local invoke EmailHandlerFunction -e event.json
```

## Deployment Workflow

1. **Build Lambda functions**:
   ```bash
   cd lambda-functions/EmailHandler
   dotnet publish -c Release -o ../../infrastructure/terraform/publish/EmailHandler
   ```

2. **Update Terraform** to use `archive_file` or provide ZIP paths

3. **Apply Terraform**:
   ```bash
   cd infrastructure/terraform
   terraform apply
   ```

4. **Verify deployment**:
   ```bash
   aws lambda get-function --function-name sqordia-email-handler-production
   ```

## Monitoring

- CloudWatch Logs: `/aws/lambda/{function-name}`
- CloudWatch Metrics: Invocations, Duration, Errors
- X-Ray Tracing (optional): For detailed debugging

## Next Steps

1. Create Lambda function projects
2. Implement handlers for each queue type
3. Build and package functions
4. Update Terraform to use packages
5. Deploy and test

## Resources

- [AWS Lambda .NET Documentation](https://docs.aws.amazon.com/lambda/latest/dg/lambda-dotnet.html)
- [AWS Lambda with SQS](https://docs.aws.amazon.com/lambda/latest/dg/with-sqs.html)
- [.NET Lambda Templates](https://github.com/aws/aws-lambda-dotnet)

