# CloudWatch Log Group for API Application Logs
resource "aws_cloudwatch_log_group" "api_logs" {
  name              = "/aws/sqordia/api"
  retention_in_days = 7

  tags = {
    Name        = "${var.project_name}-api-logs"
    Environment = var.environment
    Service     = "API"
  }
}

