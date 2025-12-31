# Deploy to AWS Lightsail - Quick Start Guide

## 🚀 Quick Deployment Steps

### Prerequisites ✅

- ✅ AWS Account configured
- ✅ Terraform infrastructure deployed (RDS, S3, SQS, Lambda)
- ✅ Docker installed locally
- ✅ Application builds successfully

### Step 1: Get Required Information

Get the connection details from Terraform:

```powershell
cd infrastructure/terraform
terraform output
```

**You'll need:**
- RDS endpoint
- S3 bucket name
- SQS queue URLs
- Lightsail IAM role ARN

### Step 2: Build Docker Image

```powershell
# From project root
docker build -t sqordia-api:latest .
```

### Step 3: Push to Container Registry

**Option A: AWS ECR (Recommended)**

```powershell
# Login to ECR
aws ecr get-login-password --region ca-central-1 | docker login --username AWS --password-stdin 743569700143.dkr.ecr.ca-central-1.amazonaws.com

# Create ECR repository (if not exists)
aws ecr create-repository --repository-name sqordia-api --region ca-central-1

# Tag and push
docker tag sqordia-api:latest 743569700143.dkr.ecr.ca-central-1.amazonaws.com/sqordia-api:latest
docker push 743569700143.dkr.ecr.ca-central-1.amazonaws.com/sqordia-api:latest
```

**Option B: Docker Hub**

```powershell
docker login
docker tag sqordia-api:latest YOUR_USERNAME/sqordia-api:latest
docker push YOUR_USERNAME/sqordia-api:latest
```

### Step 4: Create Lightsail Container Service

1. **Go to AWS Lightsail Console**
   - https://console.aws.amazon.com/lightsail/
   - Region: **Canada (Central)** - `ca-central-1`

2. **Create Container Service**
   - Click **"Containers"** → **"Create container service"**
   - **Power**: **Micro** (0.5 vCPU, 1 GB RAM) - $20/month
   - **Scale**: 1 container
   - **Name**: `sqordia-api-production`
   - Click **"Create container service"**

3. **Wait for Service Creation** (2-3 minutes)

### Step 5: Configure Deployment

1. **Click on your service** → **"Create deployment"**

2. **Container Configuration**:
   - **Container name**: `api`
   - **Image**: Your ECR or Docker Hub image URL
   - **Port mappings**: 
     - Container port: `8080`
     - Protocol: `HTTP`

3. **Environment Variables** (Add these):

```bash
# Database Connection
ConnectionStrings__DefaultConnection=Host=sqordia-db-production.c326icw2wbit.ca-central-1.rds.amazonaws.com;Port=5432;Database=SqordiaDb;Username=sqordia_admin;Password=EHYKQsGdokt6cUAL;SSL Mode=Require;Trust Server Certificate=true

# Application
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080

# AWS Services
AWS_REGION=ca-central-1
S3_BUCKET_NAME=sqordia-documents-production

# SQS Queues (get from terraform output)
EMAIL_QUEUE_URL=https://sqs.ca-central-1.amazonaws.com/743569700143/sqordia-email-queue-production
AI_GENERATION_QUEUE_URL=https://sqs.ca-central-1.amazonaws.com/743569700143/sqordia-ai-generation-queue-production
EXPORT_QUEUE_URL=https://sqs.ca-central-1.amazonaws.com/743569700143/sqordia-export-queue-production

# JWT (set your own secret)
JwtSettings__Secret=YourSuperSecretKeyThatIsAtLeast32CharactersLongForSecurity123!
JwtSettings__Issuer=Sqordia
JwtSettings__Audience=SqordiaUsers

# Google OAuth (if needed)
GoogleOAuth__ClientId=your-client-id
GoogleOAuth__ClientSecret=your-client-secret
```

4. **Public Endpoint**:
   - Enable **"Public endpoint"**
   - Container: `api`
   - Port: `8080`

5. **Click "Save and deploy"**

### Step 6: Configure VPC Peering (For RDS Access)

Lightsail needs VPC peering to access your RDS database.

1. **In Lightsail Console**:
   - Go to **Networking** → **VPC peering**
   - Click **"Create peering connection"**
   - Select your VPC (from Terraform: `sqordia-vpc-production`)
   - Click **"Create"**

2. **Accept in VPC Console**:
   - Go to **VPC Console** → **Peering Connections**
   - Find pending connection → **Actions** → **Accept Request**

3. **Update Route Tables**:
   - Add route in Lightsail route table to your VPC
   - Add route in your VPC route table to Lightsail

### Step 7: Verify Deployment

1. **Check Logs**:
   - Lightsail Console → Your service → **Logs** tab
   - Look for: "Database migrations completed successfully"
   - Look for: "Sqordia application started successfully"

2. **Get Service URL**:
   - Lightsail Console → Your service → **Public endpoint**
   - Test: `https://your-service-url.lightsail.awsapps.com/api/health`

3. **Verify Migrations**:
   - Check logs for migration messages
   - Or connect to RDS and verify tables exist

## 🎯 What Happens on Startup

When your container starts:

1. ✅ **Connects to RDS** (via VPC peering)
2. ✅ **Runs migrations automatically** (via `ApplyDatabaseMigrationsAsync`)
3. ✅ **Loads critical settings** from database
4. ✅ **Starts API server** on port 8080

## 📋 Environment Variables Reference

Get all values from Terraform:

```powershell
cd infrastructure/terraform

# RDS
terraform output rds_endpoint
terraform output rds_address

# S3
terraform output s3_bucket_name

# SQS
terraform output email_queue_url
terraform output ai_generation_queue_url
terraform output export_queue_url
```

## 🔧 Troubleshooting

### Container Won't Start
- Check logs in Lightsail Console
- Verify environment variables
- Check Docker image is accessible

### Cannot Connect to RDS
- Verify VPC peering is active
- Check security group allows traffic
- Verify connection string format

### Migrations Not Running
- Check application logs
- Verify connection string
- Check database permissions

## 📊 Cost Estimate

- **Lightsail Container Service (Micro)**: $20/month
- **RDS PostgreSQL (Free Tier)**: $0/month (first year)
- **S3**: ~$0.50/month
- **SQS**: ~$0.40/month
- **Lambda**: Free tier
- **Total**: ~$21/month ✅

## 🎉 Next Steps After Deployment

1. ✅ Verify API is accessible
2. ✅ Test endpoints
3. ✅ Configure custom domain (optional)
4. ✅ Set up monitoring
5. ✅ Configure CI/CD (GitHub Actions)

## 📚 Full Documentation

See `infrastructure/terraform/LIGHTSAIL_DEPLOYMENT.md` for detailed instructions.

