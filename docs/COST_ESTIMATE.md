# AWS Monthly Cost Estimate

## 📊 Complete Cost Breakdown

### Core Services

| Service | Configuration | Monthly Cost | Notes |
|---------|--------------|--------------|-------|
| **ECS Fargate** | 0.5 vCPU, 1024MB RAM | ~$14-18 | Running 24/7 (increased to prevent crashes) |
| **RDS PostgreSQL** | db.t4g.micro, 20GB | $0 (12 months) → ~$15 | Free tier eligible for 12 months |
| **S3 Storage** | Standard, 20GB | ~$0.50 | First 5GB free, then $0.023/GB |
| **SQS** | 3 queues | $0 | 1M requests/month free |
| **Lambda** | 3 functions, 512MB | $0 | 1M requests/month free, 400K GB-seconds free |
| **Secrets Manager** | 1 secret | $0.40 | $0.40 per secret per month |
| **CloudWatch Logs** | 7-day retention | ~$0.50-1 | Pay per GB ingested/stored |
| **Data Transfer** | Outbound | ~$1-2 | First 1GB free, then $0.09/GB |
| **VPC/Networking** | VPC, subnets, etc. | $0 | Free |
| **ECR** | Image storage | ~$0.10 | $0.10 per GB/month (minimal) |

### Monthly Cost Summary

#### First 12 Months (Free Tier)
| Category | Cost |
|----------|------|
| ECS Fargate | $14-18 |
| RDS PostgreSQL | **$0** (Free Tier) |
| S3 Storage | $0.50 |
| Secrets Manager | $0.40 |
| CloudWatch Logs | $0.50-1 |
| Data Transfer | $1-2 |
| ECR | $0.10 |
| **Total** | **~$16.50-22/month** ⚠️ |

#### After 12 Months (RDS Free Tier Expires)
| Category | Cost |
|----------|------|
| ECS Fargate | $14-18 |
| RDS PostgreSQL | **$15** |
| S3 Storage | $0.50 |
| Secrets Manager | $0.40 |
| CloudWatch Logs | $0.50-1 |
| Data Transfer | $1-2 |
| ECR | $0.10 |
| **Total** | **~$32.50-37/month** |

## 💰 Cost Breakdown Details

### ECS Fargate (~$14-18/month)
- **CPU:** 0.5 vCPU (512 units) × $0.04048/vCPU-hour × 730 hours = ~$14.80
- **Memory:** 1024 MB (1 GB) × $0.004445/GB-hour × 730 hours = ~$3.24
- **Total:** ~$18/month (varies by region)
- **Note:** Increased from 512MB to 1024MB to prevent segmentation faults

### RDS PostgreSQL
- **First 12 months:** FREE (Free Tier)
  - db.t4g.micro instance
  - 20GB storage
  - 20GB backup storage
- **After 12 months:** ~$15/month
  - Instance: ~$13/month
  - Storage: ~$2/month (20GB × $0.115/GB)

### S3 Storage (~$0.50/month)
- First 5GB: FREE
- Next 15GB: 15GB × $0.023/GB = $0.35
- Requests: Minimal (mostly free tier)

### Secrets Manager ($0.40/month)
- $0.40 per secret per month
- You have 1 secret (RDS connection string)

### CloudWatch Logs (~$0.50-1/month)
- Ingestion: First 5GB free, then $0.50/GB
- Storage: $0.03/GB/month (7-day retention)
- With optimized log levels, should stay under $1/month

### Data Transfer (~$1-2/month)
- First 1GB outbound: FREE
- Next 9GB: 9GB × $0.09/GB = $0.81
- Varies based on API usage

### SQS & Lambda (FREE)
- **SQS:** 1M requests/month free
- **Lambda:** 
  - 1M requests/month free
  - 400K GB-seconds free
- For low to moderate usage, these stay free

## 📈 Cost Optimization Tips

### Current Optimizations Already Applied
✅ Container Insights disabled (saves ~$5/month)  
✅ CloudWatch Logs retention set to 7 days  
✅ Log levels optimized (Warning default, Information for app)  
✅ RDS using db.t4g.micro (ARM-based, cheaper)  
✅ No ALB (saves ~$16/month)  
✅ Single ECS task (no over-provisioning)

### Future Cost Savings (if needed)
1. **Reduce ECS resources** (if app can handle it):
   - Current: 0.25 vCPU, 512MB
   - Could try: 0.25 vCPU, 256MB (saves ~$0.80/month)

2. **Optimize CloudWatch Logs**:
   - Reduce retention to 3 days (saves ~$0.20/month)
   - Further filter log levels

3. **S3 Lifecycle Policies**:
   - Already configured to move to Glacier after 30 days
   - Expires after 365 days

4. **RDS Optimization** (after free tier):
   - Consider Reserved Instance (saves ~30-40%)
   - Or switch to smaller instance if possible

## 🎯 Budget Comparison

| Budget | Status |
|--------|--------|
| Your Target | $20/month |
| **First 12 Months** | **~$9.50-14/month** ✅ **UNDER BUDGET** |
| **After 12 Months** | **~$24.50-29/month** ⚠️ **SLIGHTLY OVER** |

## 💡 Recommendations

### First 12 Months
- ✅ You're well within budget!
- ✅ Can even add more features if needed
- ✅ Room for growth

### After 12 Months
If you need to stay under $20/month:
1. **Option A:** Use RDS Reserved Instance (1-year) → saves ~$5/month
2. **Option B:** Optimize ECS further (reduce memory if possible)
3. **Option C:** Consider EC2 instead of ECS (saves ~$2-3/month)
4. **Option D:** Accept slightly over budget (~$5/month over)

## 📊 Monthly Cost Tracking

### How to Monitor Costs

1. **AWS Cost Explorer:**
   - Go to AWS Console → Billing → Cost Explorer
   - View daily/monthly costs
   - Set up budgets and alerts

2. **AWS Budgets:**
   - Set budget at $20/month
   - Get alerts at 80% and 100%

3. **Cost Anomaly Detection:**
   - Automatically detect unusual spending

## ✅ Summary

**Current Setup (First 12 Months):**
- **Estimated Cost:** ~$9.50-14/month
- **Your Budget:** $20/month
- **Status:** ✅ **Well within budget!**

**After 12 Months:**
- **Estimated Cost:** ~$24.50-29/month
- **Your Budget:** $20/month
- **Status:** ⚠️ **Slightly over budget (~$5-9/month)**

You have plenty of room in the first year, and can optimize or adjust after the RDS free tier expires.

