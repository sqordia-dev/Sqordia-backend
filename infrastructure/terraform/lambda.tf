# CloudWatch Log Groups for Lambda Functions
resource "aws_cloudwatch_log_group" "email_lambda_logs" {
  name              = "/aws/lambda/${var.project_name}-email-handler-${var.environment}"
  retention_in_days = 7

  tags = {
    Name        = "${var.project_name}-email-lambda-logs"
    Environment = var.environment
  }
}

resource "aws_cloudwatch_log_group" "ai_generation_lambda_logs" {
  name              = "/aws/lambda/${var.project_name}-ai-generation-handler-${var.environment}"
  retention_in_days = 7

  tags = {
    Name        = "${var.project_name}-ai-generation-lambda-logs"
    Environment = var.environment
  }
}

resource "aws_cloudwatch_log_group" "export_lambda_logs" {
  name              = "/aws/lambda/${var.project_name}-export-handler-${var.environment}"
  retention_in_days = 7

  tags = {
    Name        = "${var.project_name}-export-lambda-logs"
    Environment = var.environment
  }
}

# Lambda Function: Email Handler
resource "aws_lambda_function" "email_handler" {
  function_name = "${var.project_name}-email-handler-${var.environment}"
  description   = "Processes email jobs from SQS queue and sends via SES"
  
  # Note: You'll need to build and upload the Lambda package
  # For now, using a placeholder. Replace with actual deployment package
  filename         = "email-handler.zip"
  source_code_hash = fileexists("email-handler.zip") ? filebase64sha256("email-handler.zip") : null
  
  handler     = "Sqordia.Lambda.EmailHandler::Sqordia.Lambda.EmailHandler.Function::FunctionHandler"
  runtime     = var.lambda_runtime
  timeout     = 60 # 1 minute for email sending
  memory_size = 256

  role = aws_iam_role.lambda_execution_role.arn

  environment {
    variables = {
      RDS_ENDPOINT      = aws_db_instance.postgresql.endpoint
      DATABASE_NAME     = var.rds_database_name
      DATABASE_USERNAME = "sqordia_admin"
      SES_FROM_EMAIL    = var.ses_from_email
      SES_FROM_NAME     = "Sqordia"
      ENVIRONMENT       = var.environment
      # AWS_REGION is automatically set by Lambda - don't set it manually
    }
  }

  depends_on = [
    aws_cloudwatch_log_group.email_lambda_logs,
    aws_iam_role_policy_attachment.lambda_execution_policy,
    aws_iam_role_policy.lambda_sqs_policy
  ]

  tags = {
    Name        = "${var.project_name}-email-handler"
    Environment = var.environment
  }
}

# Lambda Function: AI Generation Handler
resource "aws_lambda_function" "ai_generation_handler" {
  function_name = "${var.project_name}-ai-generation-handler-${var.environment}"
  description   = "Processes AI business plan generation jobs from SQS queue"
  
  filename         = "ai-generation-handler.zip"
  source_code_hash = fileexists("ai-generation-handler.zip") ? filebase64sha256("ai-generation-handler.zip") : null
  
  handler     = "Sqordia.Lambda.AIGenerationHandler::Sqordia.Lambda.AIGenerationHandler.Function::FunctionHandler"
  runtime     = var.lambda_runtime
  timeout     = var.lambda_timeout # 5 minutes
  memory_size = var.lambda_memory_size # 512 MB

  role = aws_iam_role.lambda_execution_role.arn

  environment {
    variables = {
      RDS_ENDPOINT         = var.rds_endpoint
      DATABASE_NAME        = var.rds_database_name
      DATABASE_USERNAME    = "sqordia_admin"
      ENVIRONMENT          = var.environment
      DEFAULT_AI_PROVIDER  = "openai"
      # AWS_REGION is automatically set by Lambda - don't set it manually
      # API keys are now stored in the database Settings table (encrypted)
      # Lambda functions will retrieve them from the database
    }
  }

  depends_on = [
    aws_cloudwatch_log_group.ai_generation_lambda_logs,
    aws_iam_role_policy_attachment.lambda_execution_policy
  ]

  tags = {
    Name        = "${var.project_name}-ai-generation-handler"
    Environment = var.environment
  }
}

# Lambda Function: Document Export Handler
resource "aws_lambda_function" "export_handler" {
  function_name = "${var.project_name}-export-handler-${var.environment}"
  description   = "Processes document export jobs (PDF/Word) from SQS queue"
  
  filename         = "export-handler.zip"
  source_code_hash = fileexists("export-handler.zip") ? filebase64sha256("export-handler.zip") : null
  
  handler     = "Sqordia.Lambda.ExportHandler::Sqordia.Lambda.ExportHandler.Function::FunctionHandler"
  runtime     = var.lambda_runtime
  timeout     = 180 # 3 minutes for document generation
  memory_size = 512

  role = aws_iam_role.lambda_execution_role.arn

  environment {
    variables = {
      RDS_ENDPOINT      = aws_db_instance.postgresql.endpoint
      DATABASE_NAME     = var.rds_database_name
      DATABASE_USERNAME = "sqordia_admin"
      S3_BUCKET_NAME    = aws_s3_bucket.documents.id
      ENVIRONMENT       = var.environment
      # AWS_REGION is automatically set by Lambda - don't set it manually
    }
  }

  depends_on = [
    aws_cloudwatch_log_group.export_lambda_logs,
    aws_iam_role_policy_attachment.lambda_execution_policy
  ]

  tags = {
    Name        = "${var.project_name}-export-handler"
    Environment = var.environment
  }
}

# Event Source Mapping: Email Queue → Email Lambda
resource "aws_lambda_event_source_mapping" "email_queue_mapping" {
  event_source_arn = aws_sqs_queue.email_queue.arn
  function_name     = aws_lambda_function.email_handler.arn
  batch_size        = 10
  maximum_batching_window_in_seconds = 5

  depends_on = [
    aws_lambda_function.email_handler,
    aws_iam_role_policy.lambda_sqs_policy
  ]
}

# Event Source Mapping: AI Generation Queue → AI Generation Lambda
resource "aws_lambda_event_source_mapping" "ai_generation_queue_mapping" {
  event_source_arn = aws_sqs_queue.ai_generation_queue.arn
  function_name     = aws_lambda_function.ai_generation_handler.arn
  batch_size        = 1 # Process one at a time for AI generation
  maximum_batching_window_in_seconds = 0

  depends_on = [
    aws_lambda_function.ai_generation_handler,
    aws_iam_role_policy.lambda_sqs_policy
  ]
}

# Event Source Mapping: Export Queue → Export Lambda
resource "aws_lambda_event_source_mapping" "export_queue_mapping" {
  event_source_arn = aws_sqs_queue.export_queue.arn
  function_name     = aws_lambda_function.export_handler.arn
  batch_size        = 5
  maximum_batching_window_in_seconds = 5

  depends_on = [
    aws_lambda_function.export_handler,
    aws_iam_role_policy.lambda_sqs_policy
  ]
}

