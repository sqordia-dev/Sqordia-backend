# GitHub Secrets Setup Guide

This guide explains the GitHub secrets you need to configure for AWS deployment via GitHub Actions.

## Required GitHub Secrets

### 1. **AWS_ACCESS_KEY_ID** (Required)
**Purpose**: AWS access key for IAM user with deployment permissions

**How to create**:
1. Go to AWS Console → IAM → Users
2. Create a new user or select existing user
3. Attach policies: `AmazonLightsailFullAccess`, `RDSFullAccess`, `S3FullAccess`, `SESFullAccess`, `LambdaFullAccess`, `SQSFullAccess`, `CloudWatchLogsFullAccess`
4. Create access key (Access key type: Application running outside AWS)
5. Copy the Access Key ID

**Value**: Your AWS access key ID (e.g., `AKIAIOSFODNN7EXAMPLE`)

---

### 2. **AWS_SECRET_ACCESS_KEY** (Required - sensitive)
**Purpose**: AWS secret access key for IAM user

**Value**: Your AWS secret access key (keep this secure, never commit to Git)

---

### 3. **AWS_REGION** (Optional)
**Purpose**: AWS region for deployment

**Default**: `ca-central-1` (Canada Central)

**Value**: `ca-central-1`

---

### 4. **RDS_ENDPOINT** (Required)
**Purpose**: RDS PostgreSQL endpoint

**How to get**:
1. Go to AWS Console → RDS → Databases
2. Select your database instance
3. Copy the endpoint (e.g., `sqordia-db.xxxxx.ca-central-1.rds.amazonaws.com`)

**Value**: Your RDS endpoint

---

### 5. **RDS_DATABASE_NAME** (Required)
**Purpose**: PostgreSQL database name

**Value**: `SqordiaDb` (or your database name)

---

### 6. **RDS_USERNAME** (Required)
**Purpose**: RDS PostgreSQL master username

**Value**: `sqordia_admin` (or your database username)

---

### 7. **RDS_PASSWORD** (Required - sensitive)
**Purpose**: RDS PostgreSQL master password

**How to set**:
1. Go to AWS Console → RDS → Databases
2. Select your database instance
3. Modify → Change master password
4. Set a strong password

**Value**: Your database password (keep this secure)

---

### 8. **JWT_SECRET** (Required - sensitive)
**Purpose**: JWT token signing secret (minimum 32 characters)

**How to generate**:
```bash
# Generate a secure random key
openssl rand -base64 32
```

**Requirements**: 
- Minimum 32 characters
- Should be unique and kept secret
- Store securely

---

### 9. **OPENAI_API_KEY** (Optional)
**Purpose**: OpenAI API key for AI features

**How to get**: 
1. Sign up at https://platform.openai.com
2. Create an API key in your dashboard
3. Copy the key (starts with `sk-`)

---

### 10. **CLAUDE_API_KEY** (Optional)
**Purpose**: Anthropic Claude API key for AI features

**How to get**: 
1. Sign up at https://console.anthropic.com
2. Create an API key
3. Copy the key

---

### 11. **GEMINI_API_KEY** (Optional)
**Purpose**: Google Gemini API key for AI features

**How to get**:
1. Go to https://aistudio.google.com/apikey
2. Create an API key
3. Copy the key

---

### 12. **SES_FROM_EMAIL** (Required)
**Purpose**: AWS SES sender email address

**How to set**:
1. Go to AWS Console → SES → Verified identities
2. Verify your email address or domain
3. Copy the verified email

**Value**: Your verified SES email (e.g., `noreply@sqordia.com`)

---

### 13. **S3_BUCKET_NAME** (Required)
**Purpose**: S3 bucket name for file storage

**How to create**:
1. Go to AWS Console → S3 → Create bucket
2. Bucket name: `sqordia-documents-production` (or your preferred name)
3. Region: `ca-central-1`
4. Create bucket

**Value**: Your S3 bucket name

---

### 14. **GOOGLE_OAUTH_CLIENT_ID** (Optional)
**Purpose**: Google OAuth client ID for authentication

**How to get**:
1. Go to Google Cloud Console
2. Create OAuth 2.0 credentials
3. Copy the Client ID

---

### 15. **GOOGLE_OAUTH_CLIENT_SECRET** (Optional - sensitive)
**Purpose**: Google OAuth client secret

**How to get**:
1. Same as above, copy the Client Secret

---

### 16. **GOOGLE_OAUTH_REDIRECT_URI** (Optional)
**Purpose**: OAuth redirect URI

**For Production**:
- **Value**: `https://your-domain.com/api/v1/auth/google/callback`

**For Localhost Development**:
- **HTTP**: `http://localhost:5241/api/v1/auth/google/callback`
- **HTTPS**: `https://localhost:7148/api/v1/auth/google/callback`

**Note**: You need to configure both redirect URIs in your Google Cloud Console OAuth credentials to support both local development and production.

---

## How to Add Secrets to GitHub

1. Go to your GitHub repository
2. Navigate to **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret**
4. Enter the secret name and value
5. Click **Add secret**

---

## Database Settings Table (Encrypted)

API keys (OpenAI, Claude, Gemini) are stored in the database Settings table (encrypted) instead of AWS Secrets Manager to save costs ($0/month vs $1.20/month).

**To store API keys**:
1. After deployment, use the Settings API:
   ```bash
   POST /api/v1/settings/secrets/AI:OpenAI:ApiKey
   POST /api/v1/settings/secrets/AI:Claude:ApiKey
   POST /api/v1/settings/secrets/AI:Gemini:ApiKey
   ```

2. Or use the database directly (values will be encrypted automatically)

```bash
# Store OpenAI API key
aws secretsmanager put-secret-value \
  --secret-id sqordia/openai-api-key/production \
  --secret-string "your-api-key" \
  --region ca-central-1

# Store Claude API key
aws secretsmanager put-secret-value \
  --secret-id sqordia/claude-api-key/production \
  --secret-string "your-api-key" \
  --region ca-central-1

# Store Gemini API key
aws secretsmanager put-secret-value \
  --secret-id sqordia/gemini-api-key/production \
  --secret-string "your-api-key" \
  --region ca-central-1
```

---

## Security Best Practices

1. **Never commit secrets to Git** - Always use GitHub Secrets or AWS Secrets Manager
2. **Rotate secrets regularly** - Update API keys and passwords periodically
3. **Use least privilege** - Grant only necessary permissions to IAM users
4. **Enable MFA** - Use multi-factor authentication for AWS accounts
5. **Monitor access** - Review CloudTrail logs regularly

---

## Troubleshooting

### Error: "Access Denied"
- Verify IAM user has correct policies attached
- Check that access keys are valid and not expired

### Error: "Database connection failed"
- Verify RDS endpoint is correct
- Check security group allows connections from GitHub Actions IPs
- Verify database username and password

### Error: "S3 bucket not found"
- Verify bucket name is correct
- Check bucket exists in the specified region
- Verify IAM user has S3 permissions

---

For more information, see [docs/GITHUB_ACTIONS_SETUP.md](GITHUB_ACTIONS_SETUP.md).
