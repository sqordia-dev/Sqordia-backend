# Quick Deploy to AWS Lightsail

## 🚀 Fast Track Deployment

### Step 1: Build Docker Image

```powershell
docker build -t sqordia-api:latest .
```

### Step 2: Push to AWS ECR

```powershell
# Get your AWS account ID
$accountId = aws sts get-caller-identity --query Account --output text

# Login to ECR
aws ecr get-login-password --region ca-central-1 | docker login --username AWS --password-stdin $accountId.dkr.ecr.ca-central-1.amazonaws.com

# Create repository
aws ecr create-repository --repository-name sqordia-api --region ca-central-1 2>$null

# Tag and push
docker tag sqordia-api:latest $accountId.dkr.ecr.ca-central-1.amazonaws.com/sqordia-api:latest
docker push $accountId.dkr.ecr.ca-central-1.amazonaws.com/sqordia-api:latest
```

### Step 3: Deploy to Lightsail

**Go to AWS Console:**
1. https://console.aws.amazon.com/lightsail/ → **Containers** → **Create container service**
2. **Power**: Micro ($20/month)
3. **Scale**: 1
4. **Name**: `sqordia-api-production`
5. Click **Create**

**After creation:**
1. Click service → **Create deployment**
2. **Image**: `743569700143.dkr.ecr.ca-central-1.amazonaws.com/sqordia-api:latest`
3. **Port**: 8080
4. **Environment Variables** (see DEPLOY_TO_LIGHTSAIL.md)
5. **Public endpoint**: Enable
6. **Deploy**

### Step 4: VPC Peering

1. Lightsail → **Networking** → **VPC peering** → **Create**
2. Select VPC: `sqordia-vpc-production`
3. Accept in VPC Console

### Step 5: Verify

- Check logs for "Database migrations completed"
- Test: `https://your-service.lightsail.awsapps.com/api/health`

## 📋 Required Environment Variables

Copy these to Lightsail container environment variables:

```
ConnectionStrings__DefaultConnection=Host=sqordia-db-production.c326icw2wbit.ca-central-1.rds.amazonaws.com;Port=5432;Database=SqordiaDb;Username=sqordia_admin;Password=EHYKQsGdokt6cUAL;SSL Mode=Require;Trust Server Certificate=true
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
AWS_REGION=ca-central-1
S3_BUCKET_NAME=sqordia-documents-production
EMAIL_QUEUE_URL=https://sqs.ca-central-1.amazonaws.com/743569700143/sqordia-email-queue-production
AI_GENERATION_QUEUE_URL=https://sqs.ca-central-1.amazonaws.com/743569700143/sqordia-ai-generation-queue-production
EXPORT_QUEUE_URL=https://sqs.ca-central-1.amazonaws.com/743569700143/sqordia-export-queue-production
```

## ✅ What Happens

1. Container starts
2. Connects to RDS (via VPC peering)
3. **Runs migrations automatically** ✅
4. Starts API server
5. Ready to accept requests!

## 📖 Full Guide

See `DEPLOY_TO_LIGHTSAIL.md` for complete instructions.

