variable "aws_region" {
  description = "AWS region for resources"
  type        = string
  default     = "ca-central-1"
}

variable "environment" {
  description = "Environment name (dev, staging, production)"
  type        = string
  default     = "production"
}

variable "project_name" {
  description = "Project name for resource naming"
  type        = string
  default     = "sqordia"
}

variable "lambda_runtime" {
  description = "Lambda runtime for .NET functions"
  type        = string
  default     = "dotnet8"
}

variable "lambda_timeout" {
  description = "Lambda function timeout in seconds"
  type        = number
  default     = 300 # 5 minutes for AI generation
}

variable "lambda_memory_size" {
  description = "Lambda function memory size in MB"
  type        = number
  default     = 512
}

variable "max_receive_count" {
  description = "Maximum number of times a message can be received before moving to DLQ"
  type        = number
  default     = 3
}

variable "visibility_timeout_seconds" {
  description = "SQS visibility timeout (should be >= Lambda timeout)"
  type        = number
  default     = 360 # 6 minutes
}

variable "message_retention_seconds" {
  description = "SQS message retention period in seconds"
  type        = number
  default     = 1209600 # 14 days
}

variable "rds_endpoint" {
  description = "RDS PostgreSQL endpoint (will be set from RDS module output)"
  type        = string
  default     = ""
}

variable "rds_database_name" {
  description = "RDS database name"
  type        = string
  default     = "SqordiaDb"
}

variable "rds_password" {
  description = "RDS PostgreSQL master password (set via terraform.tfvars or TF_VAR_rds_password)"
  type        = string
  sensitive   = true
  # No default - must be provided
}

variable "s3_bucket_name" {
  description = "S3 bucket name for document exports (will be set from S3 module output)"
  type        = string
  default     = ""
}

variable "ses_from_email" {
  description = "SES sender email address"
  type        = string
  default     = "noreply@sqordia.com"
}

# Note: API keys are now stored in the database Settings table (encrypted)
# No need for Secrets Manager variables

