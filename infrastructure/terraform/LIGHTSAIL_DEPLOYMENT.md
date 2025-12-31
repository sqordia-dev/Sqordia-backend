# AWS Lightsail Deployment Guide

## Overview

This guide will help you deploy your ASP.NET Core 8 application to AWS Lightsail Container Service. Lightsail Container Service is recommended over Lightsail Instances because:
- ✅ Easier Docker deployment
- ✅ Automatic scaling
- ✅ Built-in load balancing
- ✅ Automatic SSL certificates
- ✅ Better integration with containerized applications

## Prerequisites

- ✅ AWS Account with Lightsail access
- ✅ Docker image of your application
- ✅ Terraform infrastructure already deployed (RDS, S3, SQS, Lambda)
- ✅ AWS CLI configured

## Step 1: Build Docker Image

### Create Dockerfile (if not exists)

Ensure you have a `Dockerfile` in your project root:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/WebAPI/WebAPI.csproj", "src/WebAPI/"]
COPY ["src/Core/Sqordia.Domain/Sqordia.Domain.csproj", "src/Core/Sqordia.Domain/"]
COPY ["src/Core/Sqordia.Application/Sqordia.Application.csproj", "src/Core/Sqordia.Application/"]
COPY ["src/Infrastructure/Sqordia.Persistence/Sqordia.Persistence.csproj", "src/Infrastructure/Sqordia.Persistence/"]
COPY ["src/Infrastructure/Sqordia.Infrastructure/Sqordia.Infrastructure.csproj", "src/Infrastructure/Sqordia.Infrastructure/"]
COPY ["src/Common/Sqordia.Contracts/Sqordia.Contracts.csproj", "src/Common/Sqordia.Contracts/"]

RUN dotnet restore "src/WebAPI/WebAPI.csproj"
COPY . .
WORKDIR "/src/src/WebAPI"
RUN dotnet build "WebAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WebAPI.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebAPI.dll"]
```

### Build and Tag Docker Image

```powershell
# Build the Docker image
docker build -t sqordia-api:latest .

# Tag for AWS ECR (if using ECR) or prepare for Lightsail
docker tag sqordia-api:latest sqordia-api:latest
```

## Step 2: Create Lightsail Container Service

### Option A: Using AWS Console (Recommended for First Time)

1. **Go to AWS Lightsail Console**
   - Navigate to: https://console.aws.amazon.com/lightsail/
   - Select your region: **Canada (Central)** - `ca-central-1`

2. **Create Container Service**
   - Click **"Containers"** in the left menu
   - Click **"Create container service"**
   - **Power**: Select **Nano** (0.25 vCPU, 0.5 GB RAM) - $7/month
     - Or **Micro** (0.5 vCPU, 1 GB RAM) - $20/month (recommended)
   - **Scale**: 1 container
   - **Name**: `sqordia-api-production`
   - Click **"Create container service"**

3. **Configure Container Deployment**
   - Wait for service to be created (2-3 minutes)
   - Click on your service name
   - Click **"Create deployment"**

4. **Configure Container**
   - **Container name**: `api`
   - **Image**: 
     - If using ECR: `your-account.dkr.ecr.ca-central-1.amazonaws.com/sqordia-api:latest`
     - If using Docker Hub: `your-dockerhub-username/sqordia-api:latest`
     - Or use a public image temporarily for testing
   
   - **Environment Variables**:
     ```
     ConnectionStrings__DefaultConnection=Host=sqordia-db-production.c326icw2wbit.ca-central-1.rds.amazonaws.com;Port=5432;Database=SqordiaDb;Username=sqordia_admin;Password=EHYKQsGdokt6cUAL;SSL Mode=Require;Trust Server Certificate=true
     ASPNETCORE_ENVIRONMENT=Production
     ASPNETCORE_URLS=http://+:80
     AWS_REGION=ca-central-1
     S3_BUCKET_NAME=sqordia-documents-production
     ```
   
   - **Port Mappings**:
     - Container port: `80`
     - Protocol: `HTTP`
   
   - **Public Endpoint**: Enable (to access your API)

5. **Deploy**
   - Click **"Save and deploy"**
   - Wait for deployment (5-10 minutes)

### Option B: Using AWS CLI

```powershell
# Create container service
aws lightsail create-container-service `
  --service-name sqordia-api-production `
  --power nano `
  --scale 1 `
  --region ca-central-1

# Wait for service to be ready, then create deployment
aws lightsail create-container-service-deployment `
  --service-name sqordia-api-production `
  --containers "api={image=your-image:latest,ports={80=HTTP},environment={ConnectionStrings__DefaultConnection=Host=sqordia-db-production.c326icw2wbit.ca-central-1.rds.amazonaws.com;Port=5432;Database=SqordiaDb;Username=sqordia_admin;Password=EHYKQsGdokt6cUAL;SSL Mode=Require;Trust Server Certificate=true,ASPNETCORE_ENVIRONMENT=Production}}" `
  --public-endpoint "containerName=api,containerPort=80,healthCheck={healthyThreshold=2,unhealthyThreshold=2,timeoutSeconds=5,intervalSeconds=30,path=/,successCodes=200-499}" `
  --region ca-central-1
```

## Step 3: Configure VPC Peering (For RDS Access)

Lightsail Container Service needs VPC peering to access RDS in your VPC.

### Get Your VPC Information

```powershell
# Get VPC ID from Terraform outputs
cd infrastructure/terraform
terraform output
```

### Create VPC Peering Connection

1. **In Lightsail Console**:
   - Go to **Networking** → **VPC peering**
   - Click **"Create peering connection"**
   - Select your VPC (from Terraform)
   - Click **"Create"**

2. **Accept Peering in VPC Console**:
   - Go to **VPC Console** → **Peering Connections**
   - Find the pending peering connection
   - Accept it

3. **Update Route Tables**:
   - Add routes in both VPC route tables to allow traffic

## Step 4: Configure Environment Variables

Set these environment variables in your Lightsail container:

```bash
# Database
ConnectionStrings__DefaultConnection=Host=sqordia-db-production.c326icw2wbit.ca-central-1.rds.amazonaws.com;Port=5432;Database=SqordiaDb;Username=sqordia_admin;Password=EHYKQsGdokt6cUAL;SSL Mode=Require;Trust Server Certificate=true

# AWS
AWS_REGION=ca-central-1
S3_BUCKET_NAME=sqordia-documents-production

# Application
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:80

# SQS Queue URLs (from Terraform outputs)
EMAIL_QUEUE_URL=https://sqs.ca-central-1.amazonaws.com/743569700143/sqordia-email-queue-production
AI_GENERATION_QUEUE_URL=https://sqs.ca-central-1.amazonaws.com/743569700143/sqordia-ai-generation-queue-production
EXPORT_QUEUE_URL=https://sqs.ca-central-1.amazonaws.com/743569700143/sqordia-export-queue-production

# JWT (set securely)
JwtSettings__Secret=YourSuperSecretKeyThatIsAtLeast32CharactersLongForSecurity123!
JwtSettings__Issuer=Sqordia
JwtSettings__Audience=SqordiaUsers

# Google OAuth (if needed)
GoogleOAuth__ClientId=your-client-id
GoogleOAuth__ClientSecret=your-client-secret
```

## Step 5: Get SQS Queue URLs

Get the queue URLs from Terraform:

```powershell
cd infrastructure/terraform
terraform output email_queue_url
terraform output ai_generation_queue_url
terraform output export_queue_url
```

## Step 6: Verify Deployment

1. **Check Container Logs**:
   - In Lightsail Console → Your service → **Logs** tab
   - Look for: "Database migrations completed successfully"
   - Look for: "Sqordia application started successfully"

2. **Test API Endpoint**:
   - Get your service URL from Lightsail Console
   - Test: `https://your-service-url.lightsail.awsapps.com/api/health`

3. **Verify Database Connection**:
   - Check logs for successful database connection
   - Verify migrations ran (check database tables)

## Step 7: Set Up Custom Domain (Optional)

1. **In Lightsail Console**:
   - Go to your container service
   - Click **"Custom domains"**
   - Add your domain
   - Follow DNS configuration instructions

## Troubleshooting

### Container Won't Start

- Check logs in Lightsail Console
- Verify environment variables are set correctly
- Check database connection string format

### Cannot Connect to RDS

- Verify VPC peering is configured
- Check security group allows traffic from Lightsail
- Verify RDS endpoint is correct

### Migrations Not Running

- Check application logs
- Verify connection string is correct
- Check database permissions

## Cost Estimate

- **Lightsail Container Service (Micro)**: $20/month
- **RDS PostgreSQL (Free Tier)**: $0/month (first year)
- **S3 Storage**: ~$0.50/month
- **SQS**: ~$0.40/month
- **Lambda**: Free tier (1M requests/month)
- **Total**: ~$21/month

## Next Steps

1. ✅ Deploy container service
2. ✅ Verify migrations ran
3. ✅ Test API endpoints
4. ✅ Configure custom domain (optional)
5. ✅ Set up monitoring and alerts

## Additional Resources

- [AWS Lightsail Container Service Documentation](https://docs.aws.amazon.com/lightsail/latest/userguide/amazon-lightsail-container-services.html)
- [Lightsail Pricing](https://aws.amazon.com/lightsail/pricing/)

