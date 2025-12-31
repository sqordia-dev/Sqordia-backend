# SQS Queue URLs (for API to send messages)
output "email_queue_url" {
  description = "URL of the email SQS queue"
  value       = aws_sqs_queue.email_queue.url
}

output "ai_generation_queue_url" {
  description = "URL of the AI generation SQS queue"
  value       = aws_sqs_queue.ai_generation_queue.url
}

output "export_queue_url" {
  description = "URL of the document export SQS queue"
  value       = aws_sqs_queue.export_queue.url
}

# SQS Queue ARNs
output "email_queue_arn" {
  description = "ARN of the email SQS queue"
  value       = aws_sqs_queue.email_queue.arn
}

output "ai_generation_queue_arn" {
  description = "ARN of the AI generation SQS queue"
  value       = aws_sqs_queue.ai_generation_queue.arn
}

output "export_queue_arn" {
  description = "ARN of the document export SQS queue"
  value       = aws_sqs_queue.export_queue.arn
}

# Lambda Function ARNs
output "email_lambda_arn" {
  description = "ARN of the email handler Lambda function"
  value       = aws_lambda_function.email_handler.arn
}

output "ai_generation_lambda_arn" {
  description = "ARN of the AI generation handler Lambda function"
  value       = aws_lambda_function.ai_generation_handler.arn
}

output "export_lambda_arn" {
  description = "ARN of the export handler Lambda function"
  value       = aws_lambda_function.export_handler.arn
}

# Lambda Function Names
output "email_lambda_function_name" {
  description = "Name of the email handler Lambda function"
  value       = aws_lambda_function.email_handler.function_name
}

output "ai_generation_lambda_function_name" {
  description = "Name of the AI generation handler Lambda function"
  value       = aws_lambda_function.ai_generation_handler.function_name
}

output "export_lambda_function_name" {
  description = "Name of the export handler Lambda function"
  value       = aws_lambda_function.export_handler.function_name
}

# IAM Role ARNs
output "lambda_execution_role_arn" {
  description = "ARN of the Lambda execution IAM role"
  value       = aws_iam_role.lambda_execution_role.arn
}

output "lightsail_sqs_role_arn" {
  description = "ARN of the Lightsail SQS IAM role"
  value       = aws_iam_role.lightsail_sqs_role.arn
}

output "api_cloudwatch_log_group_name" {
  description = "Name of the CloudWatch log group for API logs"
  value       = aws_cloudwatch_log_group.api_logs.name
}

output "api_cloudwatch_log_group_arn" {
  description = "ARN of the CloudWatch log group for API logs"
  value       = aws_cloudwatch_log_group.api_logs.arn
}

# RDS Outputs
output "rds_endpoint" {
  description = "RDS PostgreSQL endpoint"
  value       = aws_db_instance.postgresql.endpoint
}

output "rds_address" {
  description = "RDS PostgreSQL address (hostname only)"
  value       = aws_db_instance.postgresql.address
}

output "rds_port" {
  description = "RDS PostgreSQL port"
  value       = aws_db_instance.postgresql.port
}

output "rds_database_name" {
  description = "RDS database name"
  value       = aws_db_instance.postgresql.db_name
}

output "rds_username" {
  description = "RDS master username"
  value       = aws_db_instance.postgresql.username
  sensitive   = false # Username is not sensitive
}

# S3 Outputs
output "s3_bucket_name" {
  description = "S3 bucket name for documents"
  value       = aws_s3_bucket.documents.id
}

output "s3_bucket_arn" {
  description = "S3 bucket ARN"
  value       = aws_s3_bucket.documents.arn
}

output "s3_bucket_domain_name" {
  description = "S3 bucket domain name"
  value       = aws_s3_bucket.documents.bucket_domain_name
}

# ECS Outputs are now in ecs_outputs.tf
# (ALB removed to save costs - see ecs_outputs.tf for how to get task public IP)

# Note: API keys are now stored in the database Settings table (encrypted)
# Note: lightsail_sqs_role_arn is already defined at line 71

