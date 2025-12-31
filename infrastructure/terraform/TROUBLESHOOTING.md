# Terraform Troubleshooting Guide

## InvalidClientTokenId Error

If you see "InvalidClientTokenId: The security token included in the request is invalid", check:

### 1. Verify Credentials Are Correct

- Double-check the Access Key ID and Secret Access Key
- Make sure there are no extra spaces or characters
- Copy-paste directly from AWS Console (don't type manually)

### 2. Check if Keys Are Active

1. Go to AWS Console → IAM → Users
2. Select your user
3. Go to "Security credentials" tab
4. Check if the access key status is "Active"

### 3. Verify IAM User Permissions

Your IAM user needs these policies:
- `AmazonLightsailFullAccess`
- `AmazonRDSFullAccess`
- `AmazonS3FullAccess`
- `AmazonSESFullAccess`
- `AWSLambdaFullAccess`
- `AmazonSQSFullAccess`
- `CloudWatchLogsFullAccess`

Or use `PowerUserAccess` (less secure but easier)

### 4. Reconfigure AWS CLI

If credentials are correct but still not working:

```powershell
aws configure
```

Enter:
- Access Key ID: `AKIA221BVGUXYO214R4L`
- Secret Access Key: `Lch8+FzjK0w+Ou8P9F9r4VWCSxe8nCVVOkejAGfk`
- Region: `ca-central-1`
- Output: `json`

### 5. Test Credentials

```powershell
aws sts get-caller-identity
```

This should return your AWS account ID and user ARN. If it fails, credentials are invalid.

### 6. Create New Access Key

If credentials don't work:
1. Go to AWS Console → IAM → Users → Your User
2. Security credentials → Access keys
3. Create access key
4. Download and configure with `aws configure`

## Other Common Errors

### Error: "Access Denied"
- **Solution**: Add required IAM policies to your user

### Error: "Region not available"
- **Solution**: Ensure region `ca-central-1` is available in your AWS account

### Error: "Lambda deployment package not found"
- **Solution**: This is expected. Placeholder packages are created. Build real packages later (see `LAMBDA_DEPLOYMENT.md`)

