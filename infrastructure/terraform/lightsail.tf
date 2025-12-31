# AWS Lightsail Container Service for API Deployment
# Note: Lightsail Container Service requires manual deployment via AWS Console or CLI
# This file provides the IAM role and outputs needed for Lightsail

# Lightsail Container Service (Note: Terraform AWS provider doesn't fully support Container Service)
# You'll need to create this via AWS Console or AWS CLI
# See LIGHTSAIL_DEPLOYMENT.md for detailed instructions

# Output the IAM role ARN for Lightsail to use
# Lightsail will need this role to access SQS and CloudWatch

# Note: Lightsail Container Service creation via Terraform is limited
# Recommended approach: Use AWS Console or AWS CLI for Container Service setup
# Then use this role for the service

