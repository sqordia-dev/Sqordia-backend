# ECS Fargate Deployment Guide

## 🚀 Overview

This guide walks you through deploying your Sqordia API to AWS ECS Fargate using Terraform.

## 📋 Prerequisites

- ✅ Docker image built and pushed to ECR
- ✅ Terraform configured
- ✅ AWS CLI configured
- ✅ RDS, S3, SQS, Lambda already deployed

## 🏗️ Architecture

```
Internet → ECS Fargate Service (Public IP) → RDS (via VPC)
                      ↓
                    S3, SQS
```

**Note:** ALB removed to save costs. Service is accessible directly via task's public IP address.

## 📦 What Gets Created

1. **ECS Cluster** - Container orchestration
2. **ECS Task Definition** - Container configuration
3. **ECS Service** - Runs your containers (with public IP)
4. **Security Groups** - Network security
5. **IAM Roles** - Permissions for ECS
6. **Secrets Manager** - Stores RDS connection string
7. **Public Subnets** - For ECS tasks

**Note:** Application Load Balancer removed to save costs (~$16/month). Service accessible directly via public IP.

## 💰 Estimated Costs

| Resource | Monthly Cost |
|----------|--------------|
| ECS Fargate (0.25 vCPU, 512MB) | ~$7-10 |
| Data Transfer | ~$1-2 |
| **Total** | **~$8-12/month** ✅ |

**Note:** ALB has been removed to stay within your $20/month budget. The service is accessible directly via the task's public IP address.

**Note:** ALB has been completely removed. If you need it later, you would need to recreate the `alb.tf` file.

## 🚀 Deployment Steps

### Step 1: Review Terraform Configuration

The following files have been created:
- `infrastructure/terraform/ecs.tf` - ECS cluster and service
- `infrastructure/terraform/ecs_iam.tf` - IAM roles
- `infrastructure/terraform/ecs_security.tf` - Security groups
- `infrastructure/terraform/public_subnets.tf` - Public subnets
- `infrastructure/terraform/secrets.tf` - Secrets Manager

### Step 2: Initialize and Plan

```powershell
cd infrastructure/terraform
terraform init
terraform plan
```

### Step 3: Apply Configuration

```powershell
terraform apply
```

This will create:
- ECS cluster and service
- Application Load Balancer
- Security groups
- IAM roles
- Secrets Manager secret

**Time:** ~5-10 minutes

### Step 4: Verify Deployment

1. **Check ECS Service:**
   ```powershell
   aws ecs describe-services --cluster sqordia-cluster-production --services sqordia-api-production --region ca-central-1
   ```

2. **Get Task Public IP:**
   ```powershell
   # Get task ID
   $taskId = aws ecs list-tasks --cluster sqordia-cluster-production --service-name sqordia-api-production --region ca-central-1 --query 'taskArns[0]' --output text
   
   # Get task details including public IP
   aws ecs describe-tasks --cluster sqordia-cluster-production --tasks $taskId --region ca-central-1 --query 'tasks[0].attachments[0].details[?name==`networkInterfaceId`].value' --output text
   ```

   Or use this PowerShell script:
   ```powershell
   $cluster = "sqordia-cluster-production"
   $service = "sqordia-api-production"
   $region = "ca-central-1"
   
   $taskArn = aws ecs list-tasks --cluster $cluster --service-name $service --region $region --query 'taskArns[0]' --output text
   $eniId = aws ecs describe-tasks --cluster $cluster --tasks $taskArn --region $region --query 'tasks[0].attachments[0].details[?name==`networkInterfaceId`].value' --output text
   $publicIp = aws ec2 describe-network-interfaces --network-interface-ids $eniId --region $region --query 'NetworkInterfaces[0].Association.PublicIp' --output text
   
   Write-Host "Your API is available at: http://$publicIp:8080/api"
   ```

3. **Test Health Endpoint:**
   ```powershell
   curl http://<public-ip>:8080/api/health
   ```

### Step 5: Access Your API

Your API will be available at:
```
http://<task-public-ip>:8080/api
```

**Note:** The public IP will change if the task restarts. For a stable endpoint, consider:
- Using ALB (adds ~$16/month)
- Using Route 53 with dynamic DNS
- Using a static Elastic IP (not directly supported with Fargate, would need NAT Gateway)

## 🔧 Configuration Details

### ECS Task Configuration

- **CPU:** 0.25 vCPU (256 units)
- **Memory:** 512 MB
- **Image:** Your ECR image
- **Port:** 8080

### Environment Variables

Set automatically via Terraform:
- `ASPNETCORE_ENVIRONMENT=Production`
- `ASPNETCORE_URLS=http://+:8080`
- `AWS_REGION=ca-central-1`
- `S3_BUCKET_NAME` (from S3 output)
- `EMAIL_QUEUE_URL` (from SQS output)
- `AI_GENERATION_QUEUE_URL` (from SQS output)
- `EXPORT_QUEUE_URL` (from SQS output)

### Secrets

RDS connection string is stored in AWS Secrets Manager and injected as:
- `ConnectionStrings__DefaultConnection`

## 🔒 Security

- ECS tasks run in public subnets with public IPs
- RDS is in private subnets (not publicly accessible)
- Security groups restrict traffic:
- ECS: Allows HTTP from internet (port 8080)
  - RDS: Allows PostgreSQL only from ECS

## 📊 Monitoring

### CloudWatch Logs

ECS task logs are sent to:
```
/ecs/sqordia-production
```

View logs:
```powershell
aws logs tail /ecs/sqordia-production --follow --region ca-central-1
```

### ECS Service Metrics

Monitor in AWS Console:
- Task count
- CPU/Memory utilization
- Health check status

## 🔄 Updating the Service

### Update Docker Image

1. **Build and push new image:**
   ```powershell
   docker build -t sqordia-api:latest .
   docker tag sqordia-api:latest 743569700143.dkr.ecr.ca-central-1.amazonaws.com/sqordia-api:latest
   docker push 743569700143.dkr.ecr.ca-central-1.amazonaws.com/sqordia-api:latest
   ```

2. **Force new deployment:**
   ```powershell
   aws ecs update-service --cluster sqordia-cluster-production --service sqordia-api-production --force-new-deployment --region ca-central-1
   ```

### Update Environment Variables

Edit `infrastructure/terraform/ecs.tf` and run:
```powershell
terraform apply
```

## 🐛 Troubleshooting

### Service Won't Start

1. **Check task logs:**
   ```powershell
   aws logs tail /ecs/sqordia-production --follow
   ```

2. **Check task status:**
   ```powershell
   aws ecs describe-tasks --cluster sqordia-cluster-production --tasks <task-id>
   ```

3. **Common issues:**
   - Image not found in ECR → Check image URI
   - Secrets Manager permission → Check IAM role
   - RDS connection failed → Check security groups
   - Health check failing → Check `/api/health` endpoint

### Can't Connect to RDS

1. **Verify security group:**
   - ECS security group should allow outbound to RDS
   - RDS security group should allow inbound from ECS

2. **Check VPC:**
   - ECS tasks and RDS should be in same VPC

### High Costs

- Disable Container Insights (saves ~$5/month)
- Consider EC2 instead (see alternative)

## 💡 Alternative: EC2 Deployment

If you need even lower costs, consider EC2:

**Pros:**
- Lower cost (~$7/month for t3.micro)
- More control

**Cons:**
- More maintenance (OS updates, security patches)
- Manual scaling
- No built-in load balancing

I can create EC2 Terraform configuration if needed.

## 📚 Additional Resources

- [ECS Fargate Documentation](https://docs.aws.amazon.com/AmazonECS/latest/developerguide/AWS_Fargate.html)
- [Application Load Balancer Pricing](https://aws.amazon.com/elasticloadbalancing/pricing/)
- [ECS Best Practices](https://docs.aws.amazon.com/AmazonECS/latest/bestpracticesguide/intro.html)

## ✅ Next Steps

1. Run `terraform apply`
2. Wait for service to stabilize (~5 minutes)
3. Test your API endpoint
4. Monitor costs in AWS Cost Explorer
5. Set up CloudWatch alarms (optional)

