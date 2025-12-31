# IAM Role for Lambda Functions
resource "aws_iam_role" "lambda_execution_role" {
  name = "${var.project_name}-lambda-execution-role-${var.environment}"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Action = "sts:AssumeRole"
        Effect = "Allow"
        Principal = {
          Service = "lambda.amazonaws.com"
        }
      }
    ]
  })

  tags = {
    Name        = "${var.project_name}-lambda-execution-role"
    Environment = var.environment
  }
}

# Basic Lambda execution policy (CloudWatch Logs)
resource "aws_iam_role_policy_attachment" "lambda_execution_policy" {
  role       = aws_iam_role.lambda_execution_role.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole"
}

# Policy for Lambda to read from SQS
resource "aws_iam_role_policy" "lambda_sqs_policy" {
  name = "${var.project_name}-lambda-sqs-policy-${var.environment}"
  role = aws_iam_role.lambda_execution_role.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "sqs:ReceiveMessage",
          "sqs:DeleteMessage",
          "sqs:GetQueueAttributes",
          "sqs:ChangeMessageVisibility"
        ]
        Resource = [
          aws_sqs_queue.email_queue.arn,
          aws_sqs_queue.ai_generation_queue.arn,
          aws_sqs_queue.export_queue.arn
        ]
      }
    ]
  })
}

# Policy for Lambda to send emails via SES
resource "aws_iam_role_policy" "lambda_ses_policy" {
  name = "${var.project_name}-lambda-ses-policy-${var.environment}"
  role = aws_iam_role.lambda_execution_role.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "ses:SendEmail",
          "ses:SendRawEmail"
        ]
        Resource = "*"
      }
    ]
  })
}

# Policy for Lambda to access RDS
resource "aws_iam_role_policy" "lambda_rds_policy" {
  name = "${var.project_name}-lambda-rds-policy-${var.environment}"
  role = aws_iam_role.lambda_execution_role.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "rds-db:connect"
        ]
        Resource = "*"
        Condition = {
          StringEquals = {
            "rds-db:db-user" = ["sqordia_admin"]
          }
        }
      }
    ]
  })
}

# Policy for Lambda to access S3 (for document exports)
resource "aws_iam_role_policy" "lambda_s3_policy" {
  name = "${var.project_name}-lambda-s3-policy-${var.environment}"
  role = aws_iam_role.lambda_execution_role.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "s3:PutObject",
          "s3:GetObject",
          "s3:DeleteObject"
        ]
        Resource = "${aws_s3_bucket.documents.arn}/*"
      },
      {
        Effect = "Allow"
        Action = [
          "s3:ListBucket"
        ]
        Resource = aws_s3_bucket.documents.arn
      }
    ]
  })
}

# IAM Role Policy for Lightsail to send messages to SQS
resource "aws_iam_role" "lightsail_sqs_role" {
  name = "${var.project_name}-lightsail-sqs-role-${var.environment}"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Action = "sts:AssumeRole"
        Effect = "Allow"
        Principal = {
          Service = "lightsail.amazonaws.com"
        }
      }
    ]
  })

  tags = {
    Name        = "${var.project_name}-lightsail-sqs-role"
    Environment = var.environment
  }
}

resource "aws_iam_role_policy" "lightsail_sqs_policy" {
  name = "${var.project_name}-lightsail-sqs-policy-${var.environment}"
  role = aws_iam_role.lightsail_sqs_role.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "sqs:SendMessage",
          "sqs:GetQueueUrl",
          "sqs:GetQueueAttributes"
        ]
        Resource = [
          aws_sqs_queue.email_queue.arn,
          aws_sqs_queue.ai_generation_queue.arn,
          aws_sqs_queue.export_queue.arn
        ]
      }
    ]
  })
}

# Policy for Lightsail to write to CloudWatch Logs
resource "aws_iam_role_policy" "lightsail_cloudwatch_policy" {
  name = "${var.project_name}-lightsail-cloudwatch-policy-${var.environment}"
  role = aws_iam_role.lightsail_sqs_role.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "logs:CreateLogGroup",
          "logs:CreateLogStream",
          "logs:PutLogEvents",
          "logs:DescribeLogStreams"
        ]
        Resource = [
          aws_cloudwatch_log_group.api_logs.arn,
          "${aws_cloudwatch_log_group.api_logs.arn}:*"
        ]
      }
    ]
  })
}

