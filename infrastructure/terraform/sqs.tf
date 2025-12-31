# SQS Queue for Email Jobs
resource "aws_sqs_queue" "email_queue" {
  name                      = "${var.project_name}-email-queue-${var.environment}"
  visibility_timeout_seconds = var.visibility_timeout_seconds
  message_retention_seconds  = var.message_retention_seconds
  receive_wait_time_seconds   = 20 # Long polling

  # Dead-letter queue configuration
  redrive_policy = jsonencode({
    deadLetterTargetArn = aws_sqs_queue.email_dlq.arn
    maxReceiveCount     = var.max_receive_count
  })

  tags = {
    Name        = "${var.project_name}-email-queue"
    QueueType   = "email"
    Environment = var.environment
  }
}

# Dead-letter queue for Email
resource "aws_sqs_queue" "email_dlq" {
  name                      = "${var.project_name}-email-dlq-${var.environment}"
  message_retention_seconds = 1209600 # 14 days

  tags = {
    Name        = "${var.project_name}-email-dlq"
    QueueType   = "email-dlq"
    Environment = var.environment
  }
}

# SQS Queue for AI Generation Jobs
resource "aws_sqs_queue" "ai_generation_queue" {
  name                      = "${var.project_name}-ai-generation-queue-${var.environment}"
  visibility_timeout_seconds = var.visibility_timeout_seconds
  message_retention_seconds  = var.message_retention_seconds
  receive_wait_time_seconds   = 20 # Long polling

  # Dead-letter queue configuration
  redrive_policy = jsonencode({
    deadLetterTargetArn = aws_sqs_queue.ai_generation_dlq.arn
    maxReceiveCount     = var.max_receive_count
  })

  tags = {
    Name        = "${var.project_name}-ai-generation-queue"
    QueueType   = "ai-generation"
    Environment = var.environment
  }
}

# Dead-letter queue for AI Generation
resource "aws_sqs_queue" "ai_generation_dlq" {
  name                      = "${var.project_name}-ai-generation-dlq-${var.environment}"
  message_retention_seconds = 1209600 # 14 days

  tags = {
    Name        = "${var.project_name}-ai-generation-dlq"
    QueueType   = "ai-generation-dlq"
    Environment = var.environment
  }
}

# SQS Queue for Document Export Jobs
resource "aws_sqs_queue" "export_queue" {
  name                      = "${var.project_name}-export-queue-${var.environment}"
  visibility_timeout_seconds = var.visibility_timeout_seconds
  message_retention_seconds  = var.message_retention_seconds
  receive_wait_time_seconds   = 20 # Long polling

  # Dead-letter queue configuration
  redrive_policy = jsonencode({
    deadLetterTargetArn = aws_sqs_queue.export_dlq.arn
    maxReceiveCount     = var.max_receive_count
  })

  tags = {
    Name        = "${var.project_name}-export-queue"
    QueueType   = "export"
    Environment = var.environment
  }
}

# Dead-letter queue for Document Export
resource "aws_sqs_queue" "export_dlq" {
  name                      = "${var.project_name}-export-dlq-${var.environment}"
  message_retention_seconds = 1209600 # 14 days

  tags = {
    Name        = "${var.project_name}-export-dlq"
    QueueType   = "export-dlq"
    Environment = var.environment
  }
}

