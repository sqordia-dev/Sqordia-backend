# Serilog CloudWatch Setup Guide

This guide explains how Serilog is configured to send logs to AWS CloudWatch.

## Overview

The application uses Serilog for structured logging with the following sinks:
- **Console** - For local development and immediate feedback
- **File** - For local log files (7-day retention)
- **CloudWatch** - For production environments (AWS)

## Configuration

### Development (appsettings.json)

CloudWatch is **disabled** by default in development. Log levels are optimized for cost:

```json
"Serilog": {
  "MinimumLevel": {
    "Default": "Warning",
    "Override": {
      "Microsoft": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "System": "Warning",
      "Sqordia": "Information"
    }
  },
  "CloudWatch": {
    "Enabled": false,
    "LogGroupName": "/aws/sqordia/api",
    "Region": "ca-central-1",
    "RetentionDays": 7
  }
}
```

**Optimization**: Only logs **Warning+** from Microsoft libraries, but keeps **Information+** for your application code. This reduces log volume by ~70-80% while maintaining useful application logs.

### Production (appsettings.Production.json)

CloudWatch is **enabled** automatically in production with the same optimized log levels:

```json
"Serilog": {
  "MinimumLevel": {
    "Default": "Warning",
    "Override": {
      "Microsoft": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "System": "Warning",
      "Sqordia": "Information"
    }
  },
  "CloudWatch": {
    "Enabled": true,
    "LogGroupName": "/aws/sqordia/api",
    "Region": "ca-central-1",
    "RetentionDays": 7
  }
}
```

**Cost Optimization**: 
- 7-day retention reduces storage costs by 77% vs 30-day retention
- Filtered log levels reduce ingestion by ~70-80%
- **Expected cost**: $0.50-1.50/month for small-medium traffic

## CloudWatch Configuration

### Log Group

- **Default Log Group**: `/aws/sqordia/api`
- **Customizable**: Set `Serilog:CloudWatch:LogGroupName` in appsettings

### Log Stream

- **Auto-generated**: `{MachineName}-{Timestamp}` (e.g., `server-20241219234500`)
- **Customizable**: Set `Serilog:CloudWatch:LogStreamName` in appsettings

### Region

- **Default**: `ca-central-1` (Canada Central)
- **Source**: Uses `AwsStorage:Region` from configuration
- **Customizable**: Set `Serilog:CloudWatch:Region` in appsettings

## AWS IAM Permissions Required

Your AWS Lightsail container or EC2 instance needs the following IAM permissions:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "logs:CreateLogGroup",
        "logs:CreateLogStream",
        "logs:PutLogEvents",
        "logs:DescribeLogStreams"
      ],
      "Resource": "arn:aws:logs:ca-central-1:*:log-group:/aws/sqordia/*"
    }
  ]
}
```

### Minimal Policy (if log group already exists)

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "logs:CreateLogStream",
        "logs:PutLogEvents"
      ],
      "Resource": "arn:aws:logs:ca-central-1:*:log-group:/aws/sqordia/api:*"
    }
  ]
}
```

## CloudWatch Logs Behavior

### Batching

- **Batch Size**: 100 log events per batch
- **Flush Period**: 5 seconds
- **Queue Limit**: 10,000 events
- **Retry Attempts**: 3

### Log Format

Logs are sent to CloudWatch in **JSON format** for easy querying and filtering.

### Log Levels

- **Minimum Level**: Information
- **Override**: Microsoft.AspNetCore logs are set to Warning

## Manual CloudWatch Setup

### 1. Create Log Group (Optional)

If the log group doesn't exist, CloudWatch will create it automatically. However, you can pre-create it:

```bash
aws logs create-log-group \
  --log-group-name /aws/sqordia/api \
  --region ca-central-1
```

### 2. Set Retention (Already Configured via Terraform)

The log group is created with **7-day retention** via Terraform (`infrastructure/terraform/cloudwatch.tf`). This is optimal for cost control.

If you need to change it manually:

```bash
aws logs put-retention-policy \
  --log-group-name /aws/sqordia/api \
  --retention-in-days 7 \
  --region ca-central-1
```

**Cost Note**: CloudWatch Logs pricing:
- **Ingestion**: $0.50 per GB ingested
- **Storage**: $0.03 per GB/month
- **7-day retention** reduces storage costs by 77% vs 30-day retention

### 3. Verify IAM Permissions

Ensure your Lightsail container service or EC2 instance has the required IAM role/permissions.

## Testing CloudWatch Logging

### Enable in Development

To test CloudWatch logging locally, set the environment variable:

```bash
# Windows PowerShell
$env:Serilog__CloudWatch__Enabled="true"

# Linux/Mac
export Serilog__CloudWatch__Enabled=true
```

Or update `appsettings.json`:

```json
"Serilog": {
  "CloudWatch": {
    "Enabled": true
  }
}
```

### View Logs in CloudWatch

1. Go to AWS CloudWatch Console
2. Navigate to **Logs** → **Log groups**
3. Find `/aws/sqordia/api`
4. Click on a log stream to view logs

### Query Logs

Use CloudWatch Logs Insights to query:

```
fields @timestamp, @message, @level
| filter @level = "Error"
| sort @timestamp desc
| limit 100
```

## Troubleshooting

### Logs Not Appearing in CloudWatch

1. **Check IAM Permissions**: Verify the instance has CloudWatch Logs permissions
2. **Check Region**: Ensure the region matches your AWS resources
3. **Check Environment**: Verify `ASPNETCORE_ENVIRONMENT=Production` or `Serilog:CloudWatch:Enabled=true`
4. **Check AWS Credentials**: Ensure AWS credentials are configured correctly

### High CloudWatch Costs

1. **Set Retention Policy**: Use 7-30 day retention instead of "Never expire"
2. **Filter Log Levels**: Increase minimum log level to Warning or Error
3. **Review Log Volume**: Check if excessive logging is occurring

### Performance Issues

1. **Batch Size**: Increase `BatchSizeLimit` if experiencing high volume
2. **Flush Period**: Increase `Period` to reduce API calls
3. **Queue Limit**: Monitor queue size to prevent memory issues

## Environment Variables

You can override configuration via environment variables:

```bash
# Enable CloudWatch
Serilog__CloudWatch__Enabled=true

# Custom log group
Serilog__CloudWatch__LogGroupName=/aws/sqordia/custom

# Custom region
Serilog__CloudWatch__Region=us-east-1
```

## Cost Estimation

### CloudWatch Logs Pricing (ca-central-1)

- **Ingestion**: $0.50 per GB ingested
- **Storage**: $0.03 per GB/month
- **Data Transfer**: Free within same region
- **No charges for**: API calls, log groups, log streams

### Cost Scenarios

#### Scenario 1: Small SaaS (100-500 users/month) - Low Traffic
- **Log Volume**: ~50 MB/day = 1.5 GB/month
- **Ingestion Cost**: 1.5 GB × $0.50 = **$0.75/month**
- **Storage Cost** (30-day retention): 1.5 GB × $0.03 = **$0.05/month**
- **Total**: **~$0.80/month** ✅

#### Scenario 2: Medium SaaS (1,000-5,000 users/month) - Moderate Traffic
- **Log Volume**: ~200 MB/day = 6 GB/month
- **Ingestion Cost**: 6 GB × $0.50 = **$3.00/month**
- **Storage Cost** (30-day retention): 6 GB × $0.03 = **$0.18/month**
- **Total**: **~$3.18/month** ✅

#### Scenario 3: High Traffic (10,000+ users/month)
- **Log Volume**: ~500 MB/day = 15 GB/month
- **Ingestion Cost**: 15 GB × $0.50 = **$7.50/month**
- **Storage Cost** (30-day retention): 15 GB × $0.03 = **$0.45/month**
- **Total**: **~$7.95/month** ⚠️

### Budget Impact on Your $20/Month Budget

**Current AWS Costs** (estimated):
- Lightsail: $20/month
- RDS (after free tier): ~$15/month
- S3: ~$0.50/month
- SES: ~$0.10/month
- **Subtotal**: ~$35.60/month

**With CloudWatch Logs**:
- Small SaaS: **+$0.80/month** = **$36.40/month total** ✅
- Medium SaaS: **+$3.18/month** = **$38.78/month total** ⚠️
- High Traffic: **+$7.95/month** = **$43.55/month total** ⚠️

**Note**: Your current budget is $20/month, but actual costs are already ~$35/month. CloudWatch adds minimal cost for small/medium traffic.

### Cost Optimization Strategies

1. **Set Retention Policy** (Critical for cost control):
   - 7 days: Reduces storage by 77%
   - 14 days: Reduces storage by 53%
   - 30 days: Recommended balance
   - **Never expire**: Most expensive (not recommended)

2. **Filter Log Levels**:
   - Change minimum level from `Information` to `Warning`
   - Reduces ingestion by ~70-80%
   - **Savings**: ~$0.50-2.50/month depending on traffic

3. **Use Log Sampling** (for high-volume logs):
   - Sample 10% of Information logs
   - Keep 100% of Warning/Error logs
   - **Savings**: ~$0.50-5.00/month

4. **Monitor and Alert**:
   - Set CloudWatch billing alerts at $5/month
   - Review log volume weekly
   - Adjust retention/levels as needed

### Recommended Configuration for $20 Budget

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Warning",  // Only log warnings and above
      "Override": {
        "Sqordia": "Information"  // Keep detailed logs for your app
      }
    },
    "CloudWatch": {
      "Enabled": true,
      "LogGroupName": "/aws/sqordia/api",
      "Region": "ca-central-1"
    }
  }
}
```

**With this config**:
- Only warnings/errors from Microsoft libraries
- Full Information logs for your application
- **Estimated cost**: **$0.50-1.50/month** ✅

### Cost Comparison

| Option | Monthly Cost | Pros | Cons |
|--------|-------------|------|------|
| **CloudWatch (30-day retention)** | $0.80-3.18 | Centralized, searchable, scalable | Ongoing cost |
| **CloudWatch (7-day retention)** | $0.50-2.00 | Lower cost | Less history |
| **File logs only** | $0 | Free | No centralized view, harder to search |
| **CloudWatch (Warning+ only)** | $0.30-1.00 | Very low cost | Less detail |

### Recommendation

**For your $20/month budget**:
- ✅ **Use CloudWatch with 7-14 day retention**
- ✅ **Set minimum level to Warning for Microsoft libraries**
- ✅ **Keep Information level for your application code**
- ✅ **Set up billing alerts at $2/month**
- **Expected cost**: **$0.50-1.50/month** (2.5-7.5% of budget)

**This is worth it because**:
- Essential for debugging production issues
- Helps identify performance problems
- Enables security monitoring
- Cost is minimal compared to value

## Best Practices

1. **Use Structured Logging**: Leverage Serilog's structured logging for better querying
2. **Set Appropriate Log Levels**: Don't log everything at Information level
3. **Use Log Retention**: Set retention to 7-30 days based on needs
4. **Monitor Costs**: Set up CloudWatch billing alerts
5. **Use Log Insights**: Create saved queries for common debugging scenarios

## Example Log Queries

### Find All Errors in Last Hour

```
fields @timestamp, @message
| filter @level = "Error" or @level = "Fatal"
| sort @timestamp desc
```

### Find Slow Requests

```
fields @timestamp, @message, ElapsedMilliseconds
| filter @message like /completed/
| filter ElapsedMilliseconds > 1000
| sort ElapsedMilliseconds desc
```

### Find Authentication Failures

```
fields @timestamp, @message
| filter @message like /authentication/ or @message like /login/
| filter @level = "Warning" or @level = "Error"
```

