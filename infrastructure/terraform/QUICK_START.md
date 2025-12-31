# Quick Start: Deploy Infrastructure with Terraform

## Prerequisites Checklist

- [ ] AWS Account created
- [ ] IAM User created with required permissions
- [ ] AWS Access Key ID and Secret Access Key obtained
- [ ] AWS CLI configured
- [ ] Terraform installed ✅
- [ ] RDS endpoint (if RDS already exists)
- [ ] S3 bucket name (if S3 already exists)

## Step 1: Configure AWS Credentials

Run this command and enter your credentials when prompted:

```powershell
aws configure
```

**You'll need:**
- AWS Access Key ID
- AWS Secret Access Key  
- Default region: `ca-central-1`
- Default output format: `json`

**If you don't have credentials yet:**

1. Go to AWS Console → IAM → Users
2. Create a new user (e.g., `terraform-sqordia`)
3. Attach these policies:
   - `AmazonLightsailFullAccess`
   - `AmazonRDSFullAccess`
   - `AmazonS3FullAccess`
   - `AmazonSESFullAccess`
   - `AWSLambdaFullAccess`
   - `AmazonSQSFullAccess`
   - `CloudWatchLogsFullAccess`
4. Create access key → Copy Access Key ID and Secret Access Key
5. Run `aws configure` and enter them

## Step 2: Update terraform.tfvars

Edit `infrastructure/terraform/terraform.tfvars` and set:

```hcl
# Required if RDS already exists
rds_endpoint = "your-rds-endpoint.ca-central-1.rds.amazonaws.com"

# Required if S3 bucket already exists
s3_bucket_name = "your-s3-bucket-name"

# Optional - can leave empty if resources don't exist yet
# Terraform will still create SQS, Lambda, CloudWatch, IAM resources
```

**Note**: If RDS and S3 don't exist yet, you can leave them empty. Terraform will create:
- ✅ SQS Queues
- ✅ Lambda Functions (with placeholder packages)
- ✅ CloudWatch Log Groups
- ✅ IAM Roles and Policies

You can create RDS and S3 separately later and update the variables.

## Step 3: Run Terraform Plan

```powershell
cd infrastructure/terraform
terraform plan
```

This shows what will be created without actually creating it.

## Step 4: Apply Infrastructure

```powershell
terraform apply
```

Type `yes` when prompted to create the resources.

## What Gets Created

- **3 SQS Queues** + 3 Dead-Letter Queues
- **3 Lambda Functions** (with placeholder packages - update later)
- **4 CloudWatch Log Groups** (7-day retention)
- **2 IAM Roles** + Policies
- **Total Cost**: ~$0.30-2.30/month

## After Deployment

1. **Build Lambda packages** (see `LAMBDA_DEPLOYMENT.md`)
2. **Update Lambda functions** with real packages
3. **Store API keys** in database Settings table via API
4. **Configure Lightsail** container service to use SQS queues

