# GitHub Actions Setup Guide

This guide explains how to set up and use GitHub Actions for automated AWS deployment.

## Overview

The GitHub Actions workflow automatically:
1. Builds and tests the .NET application
2. Builds Lambda functions
3. Runs Terraform plan to preview changes
4. Applies infrastructure changes (on main/develop branches)
5. Runs database migrations
6. Performs health checks

## Workflow Triggers

The workflow is triggered by:

- **Push to `main` branch** → Production deployment
- **Push to `develop` branch** → Staging deployment
- **Pull request to `main`** → Terraform plan only (no deployment)
- **Manual workflow dispatch** → On-demand deployment with environment selection

## Prerequisites

1. **GitHub Secrets configured** - See [GITHUB_SECRETS_SETUP.md](GITHUB_SECRETS_SETUP.md)
2. **AWS Account** - With appropriate IAM user and permissions
3. **Terraform** - Infrastructure code in `infrastructure/terraform/`
4. **AWS Resources** - RDS, S3 bucket, etc. (created via Terraform or manually)

## Required GitHub Secrets

See [GITHUB_SECRETS_SETUP.md](GITHUB_SECRETS_SETUP.md) for complete list. Minimum required:

- `AWS_ACCESS_KEY_ID`
- `AWS_SECRET_ACCESS_KEY`
- `RDS_ENDPOINT`
- `RDS_DATABASE_NAME`
- `RDS_USERNAME`
- `RDS_PASSWORD`
- `JWT_SECRET`
- `S3_BUCKET_NAME`
- `SES_FROM_EMAIL`

## Workflow Steps

### 1. Build and Test
- Restores NuGet packages
- Builds the solution
- Runs unit tests

### 2. Build Lambda Functions
- Builds EmailHandler, AIGenerationHandler, and ExportHandler
- Packages Lambda functions as ZIP files
- Uploads packages as artifacts

### 3. Terraform Plan
- Initializes Terraform
- Validates configuration
- Creates execution plan
- Shows what will change (always runs, even on PRs)

### 4. Terraform Apply
- Applies infrastructure changes
- Deploys Lambda functions
- Updates AWS Lightsail container service
- **Only runs on push to main/develop** (not on PRs)

### 5. Database Migration
- Runs EF Core migrations against RDS PostgreSQL
- Updates database schema
- **Only runs on push to main/develop**

### 6. Health Check
- Verifies API is responding
- Checks Lambda functions are deployed
- **Only runs on push to main/develop**

## Environment-Specific Deployments

### Production (main branch)
- Environment: `production`
- Deploys to production AWS resources
- Uses production RDS instance
- Uses production S3 bucket

### Staging (develop branch)
- Environment: `staging`
- Deploys to staging AWS resources
- Uses staging RDS instance
- Uses staging S3 bucket

## Manual Deployment

You can trigger a manual deployment:

1. Go to **Actions** tab in GitHub
2. Select **Deploy to AWS** workflow
3. Click **Run workflow**
4. Select environment (production or staging)
5. Click **Run workflow**

## Monitoring Deployments

### View Workflow Runs
1. Go to **Actions** tab
2. Click on **Deploy to AWS** workflow
3. View run history and logs

### Check Deployment Status
- Green checkmark = Success
- Red X = Failure (check logs)
- Yellow circle = In progress

### View Logs
Click on a workflow run to see detailed logs for each step.

## Troubleshooting

### Build Failures
- Check .NET SDK version compatibility
- Verify all NuGet packages are available
- Review build logs for specific errors

### Terraform Failures
- Verify AWS credentials are correct
- Check IAM permissions
- Review Terraform plan output
- Ensure resources don't already exist (if creating new)

### Lambda Build Failures
- Verify .NET 8 SDK is available
- Check Lambda project references
- Review build logs

### Database Migration Failures
- Verify RDS endpoint is correct
- Check database credentials
- Ensure security group allows connections
- Review migration logs

### Health Check Failures
- Verify API endpoint is correct
- Check Lightsail container service status
- Review application logs in CloudWatch

## Best Practices

1. **Test in staging first** - Deploy to develop branch before main
2. **Review Terraform plan** - Always check plan output before apply
3. **Monitor deployments** - Watch workflow runs and logs
4. **Use feature branches** - Create PRs to test changes
5. **Keep secrets secure** - Never commit secrets to Git

## Workflow File

The workflow is defined in `.github/workflows/deploy-aws.yml`.

## Related Documentation

- [GITHUB_SECRETS_SETUP.md](GITHUB_SECRETS_SETUP.md) - GitHub Secrets configuration
- [AWS_CLI_SETUP.md](AWS_CLI_SETUP.md) - AWS CLI setup
- [infrastructure/terraform/README.md](../infrastructure/terraform/README.md) - Terraform documentation

