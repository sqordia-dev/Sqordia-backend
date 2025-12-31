# ECS Fargate Cluster and Service

# ECS Cluster
resource "aws_ecs_cluster" "main" {
  name = "${var.project_name}-cluster-${var.environment}"

  # Container Insights disabled to save costs (~$5/month)
  # Uncomment to enable:
  # setting {
  #   name  = "containerInsights"
  #   value = "enabled"
  # }

  tags = {
    Name        = "${var.project_name}-ecs-cluster-${var.environment}"
    Environment = var.environment
    Project     = var.project_name
  }
}

# CloudWatch Log Group for ECS Tasks
resource "aws_cloudwatch_log_group" "ecs_tasks" {
  name              = "/ecs/${var.project_name}-${var.environment}"
  retention_in_days = 7 # Keep logs for 7 days to control costs

  tags = {
    Name        = "${var.project_name}-ecs-logs-${var.environment}"
    Environment = var.environment
    Project     = var.project_name
  }
}

# ECS Task Definition
resource "aws_ecs_task_definition" "api" {
  family                   = "${var.project_name}-api-${var.environment}"
  network_mode             = "awsvpc"
  requires_compatibilities = ["FARGATE"]
  cpu                      = "512"   # 0.5 vCPU (increased for better performance)
  memory                   = "1024"  # 1024 MB RAM (increased to prevent segmentation faults)
  execution_role_arn       = aws_iam_role.ecs_execution.arn
  task_role_arn            = aws_iam_role.ecs_task.arn

  container_definitions = jsonencode([
    {
      name  = "api"
      image = "${data.aws_caller_identity.current.account_id}.dkr.ecr.${var.aws_region}.amazonaws.com/sqordia-api:latest"

      portMappings = [
        {
          containerPort = 8080
          protocol      = "tcp"
        }
      ]

      environment = [
        {
          name  = "ASPNETCORE_ENVIRONMENT"
          value = "Production"
        },
        {
          name  = "ASPNETCORE_URLS"
          value = "http://+:8080"
        },
        {
          name  = "AWS_REGION"
          value = var.aws_region
        },
        {
          name  = "AwsStorage__Region"
          value = var.aws_region
        },
        {
          name  = "AwsStorage__BucketName"
          value = aws_s3_bucket.documents.id
        },
        {
          name  = "S3_BUCKET_NAME"
          value = aws_s3_bucket.documents.id
        },
        {
          name  = "EMAIL_QUEUE_URL"
          value = aws_sqs_queue.email_queue.url
        },
        {
          name  = "AI_GENERATION_QUEUE_URL"
          value = aws_sqs_queue.ai_generation_queue.url
        },
        {
          name  = "EXPORT_QUEUE_URL"
          value = aws_sqs_queue.export_queue.url
        }
      ]

      secrets = [
        {
          name      = "ConnectionStrings__DefaultConnection"
          valueFrom = aws_secretsmanager_secret.rds_connection_string.arn
        }
      ]

      logConfiguration = {
        logDriver = "awslogs"
        options = {
          "awslogs-group"         = aws_cloudwatch_log_group.ecs_tasks.name
          "awslogs-region"        = var.aws_region
          "awslogs-stream-prefix" = "ecs"
        }
      }

      # Health check removed - .NET containers typically don't have curl/wget
      # ECS will monitor the service health through service-level health checks
      # If you need container-level health checks, install curl/wget in your Dockerfile
      # healthCheck = {
      #   command     = ["CMD-SHELL", "curl -f http://localhost:8080/api/health || exit 1"]
      #   interval    = 30
      #   timeout     = 5
      #   retries     = 3
      #   startPeriod = 120
      # }
    }
  ])

  tags = {
    Name        = "${var.project_name}-api-task-${var.environment}"
    Environment = var.environment
    Project     = var.project_name
  }
}

# ECS Service
resource "aws_ecs_service" "api" {
  name            = "${var.project_name}-api-${var.environment}"
  cluster         = aws_ecs_cluster.main.id
  task_definition = aws_ecs_task_definition.api.arn
  desired_count   = 1
  launch_type      = "FARGATE"

  network_configuration {
    subnets          = [aws_subnet.public_1.id, aws_subnet.public_2.id]
    security_groups  = [aws_security_group.ecs_service.id]
    assign_public_ip = true # Required for Fargate tasks in public subnets (allows direct access)
  }

  depends_on = [
    aws_secretsmanager_secret.rds_connection_string
  ]

  tags = {
    Name        = "${var.project_name}-api-service-${var.environment}"
    Environment = var.environment
    Project     = var.project_name
  }
}

# Data source for current AWS account ID
data "aws_caller_identity" "current" {}

