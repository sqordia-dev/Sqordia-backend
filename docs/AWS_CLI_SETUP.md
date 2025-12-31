# AWS CLI Setup Guide

This guide will help you configure AWS CLI to connect to your AWS account for Terraform infrastructure deployment.

## Prerequisites

- AWS Account with appropriate permissions
- AWS CLI installed (✅ Already installed)

## Step 1: Get AWS Access Credentials

You have three options:

### Option A: Browser-Based Authentication (Recommended - Most Secure) ⭐

**Requirements**: AWS CLI v2.32.0 or later

This method uses your AWS Management Console credentials and opens a browser for authentication. No access keys needed!

1. **Check your AWS CLI version**:
   ```bash
   aws --version
   ```
   If you have v2.32.0 or later, you can use this method.

2. **Login with browser**:
   ```bash
   aws login
   ```
   
   This will:
   - Open your default web browser
   - Prompt you to sign in with your AWS credentials
   - Generate temporary credentials (valid for up to 12 hours)
   - Automatically configure AWS CLI

3. **If browser doesn't open automatically**:
   ```bash
   aws login --remote
   ```
   This will provide a URL you can open on any device.

4. **Verify authentication**:
   ```bash
   aws sts get-caller-identity
   ```

**Benefits**:
- ✅ No access keys to manage
- ✅ More secure (temporary credentials)
- ✅ Uses your existing AWS Console login
- ✅ Automatic credential refresh

**Note**: Credentials expire after 12 hours. Run `aws login` again to refresh.

### Option B: Use Your Root Account (Not Recommended for Production)

1. Go to AWS Console → Your Account Name (top right) → Security Credentials
2. Scroll to "Access keys" section
3. Click "Create access key"
4. Select "Command Line Interface (CLI)"
5. Download the credentials (you'll only see the secret key once!)

### Option C: Create IAM User (Traditional Method)

1. Go to AWS Console → IAM → Users → Create User
2. User name: `terraform-sqordia` (or your preferred name)
3. Select "Provide user access to the AWS Management Console" (optional) or "Access key - Programmatic access"
4. Click "Next"

5. **Attach Policies**:

   **How to find policies in AWS Console:**
   - In the "Set permissions" step, you'll see three options:
     1. "Add user to group" (skip this)
     2. "Copy permissions from existing user" (skip this)
     3. **"Attach policies directly"** ← Select this
   - In the search box, type the service name to filter policies
   
   **Required Policies** (search for these in the policy list):
   - `AmazonRDSFullAccess` - For RDS PostgreSQL
     - Search: "RDS" or "AmazonRDS"
   - `AmazonS3FullAccess` - For S3 bucket
     - Search: "S3" or "AmazonS3"
   - `AmazonSESFullAccess` - For email service
     - Search: "SES" or "AmazonSES"
   - `AmazonLightsailFullAccess` - For Lightsail container service
     - Search: "Lightsail" or "AmazonLightsail"
     - **Note**: If you can't find this, you can also use `PowerUserAccess` (less secure) or create a custom policy
   - ~~`SecretsManagerReadWrite`~~ - **No longer needed** (API keys stored in database Settings table)
   - `IAMFullAccess` - For creating IAM roles (if needed)
     - Search: "IAM" or "IAMFullAccess"
   - `AmazonEC2FullAccess` - For VPC and networking (if needed)
     - Search: "EC2" or "AmazonEC2"
   
   **Tip**: If you can't find exact policy names:
   - Use the search box in the policy list
   - Look for policies with "FullAccess" or "ReadWrite" in the name
   - You can attach multiple policies by checking the boxes

6. Click "Next" → "Create user"
7. **Important**: Click "Download .csv" to save credentials, or copy:
   - Access Key ID
   - Secret Access Key

## Step 2: Configure AWS CLI

### If Using Browser Authentication (Option A)

After running `aws login`, set your default region:

```bash
aws configure set default.region ca-central-1
aws configure set default.output json
```

That's it! You're ready to use Terraform.

### If Using Access Keys (Options B or C)

Open PowerShell or Command Prompt and run:

```bash
aws configure
```

You'll be prompted for:

1. **AWS Access Key ID**: Paste your Access Key ID
2. **AWS Secret Access Key**: Paste your Secret Access Key
3. **Default region name**: Enter `ca-central-1` (Canada Central - our default region)
4. **Default output format**: Enter `json` (recommended)

Example:
```
AWS Access Key ID [None]: AKIAIOSFODNN7EXAMPLE
AWS Secret Access Key [None]: wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY
Default region name [None]: ca-central-1
Default output format [None]: json
```

## Step 3: Verify Configuration

Test your AWS CLI connection:

```bash
# Check your identity
aws sts get-caller-identity

# List your S3 buckets (should be empty or show existing buckets)
aws s3 ls

# List available regions
aws ec2 describe-regions --query 'Regions[].RegionName' --output table
```

If you see your account ID and user ARN, you're all set! ✅

## Step 4: Configure AWS CLI Profiles (Optional)

If you have multiple AWS accounts or environments, use profiles:

```bash
# Create a profile for production
aws configure --profile sqordia-prod

# Create a profile for development
aws configure --profile sqordia-dev

# Use a specific profile
aws s3 ls --profile sqordia-prod

# Set default profile (add to your shell profile)
export AWS_PROFILE=sqordia-prod  # Linux/Mac
$env:AWS_PROFILE="sqordia-prod"  # PowerShell
```

## Step 5: Test Terraform Access

Once configured, Terraform will automatically use these credentials. Test with:

```bash
# This will be done after Terraform is set up
cd infrastructure/terraform
terraform init
terraform plan
```

## Security Best Practices

1. **Never commit credentials** to Git
   - AWS credentials are stored in `~/.aws/credentials` (automatically ignored by Git)
   - Add `*.pem`, `*.key`, `credentials` to `.gitignore`

2. **Use IAM roles** when possible (for EC2/Lightsail instances)

3. **Rotate access keys** regularly (every 90 days recommended)

4. **Use least privilege** - Only grant necessary permissions

5. **Enable MFA** for IAM users (if using console access)

## Troubleshooting

### Can't Find Specific Policies

If you can't find `AmazonLightsailFullAccess` or `SecretsManagerReadWrite`:

**Option 1: Search in AWS Console**
- In the policy list, use the search box at the top
- Type "Lightsail" or "Secrets" to filter
- Look for any policy with "Lightsail" or "SecretsManager" in the name

**Option 2: Use Alternative Policies**
- For Lightsail: Use `PowerUserAccess` (grants most permissions, but less secure)
- For Secrets Manager: Use `SecretsManagerReadWrite` or any policy with "SecretsManager" in the name

**Option 3: Create Custom Policy (Advanced)**
If you need specific permissions, you can create a custom policy:
1. Go to IAM → Policies → Create Policy
2. Use JSON editor and paste the required permissions
3. Attach the custom policy to your user

**Option 4: Use AdministratorAccess (Not Recommended)**
- `AdministratorAccess` - Grants full access (use only for testing/development)
- ⚠️ **Warning**: This is very powerful, use with caution

### "Unable to locate credentials"

- Make sure you ran `aws configure`
- Check credentials file exists: `cat ~/.aws/credentials` (Linux/Mac) or `type %USERPROFILE%\.aws\credentials` (Windows)
- Verify credentials are correct

### "Access Denied" errors

- Check IAM user has required policies attached
- Verify you're using the correct AWS account
- Check region permissions (some services aren't available in all regions)
- Try using `AdministratorAccess` temporarily to test if it's a permissions issue

### "Invalid region"

- Use `aws ec2 describe-regions` to see available regions
- Common regions: `us-east-1`, `us-west-2`, `eu-west-1`, `ap-southeast-1`

## Next Steps

After AWS CLI is configured:

1. ✅ Install Terraform (if not already installed)
2. ✅ Create Terraform configuration files
3. ✅ Initialize Terraform: `terraform init`
4. ✅ Plan infrastructure: `terraform plan`
5. ✅ Apply infrastructure: `terraform apply`

## Configuration File Locations

- **Windows**: `C:\Users\YourUsername\.aws\credentials`
- **Linux/Mac**: `~/.aws/credentials`

You can also manually edit these files if needed:

```ini
[default]
aws_access_key_id = YOUR_ACCESS_KEY
aws_secret_access_key = YOUR_SECRET_KEY
region = us-east-1
output = json
```

---

**Need Help?** Check AWS CLI documentation: https://docs.aws.amazon.com/cli/latest/userguide/

