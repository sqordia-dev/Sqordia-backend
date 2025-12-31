# Security Groups for ECS

# Security Group for ECS Service
resource "aws_security_group" "ecs_service" {
  name        = "${var.project_name}-ecs-service-${var.environment}"
  description = "Security group for ECS Fargate service"
  vpc_id      = aws_vpc.main.id

  # Allow inbound HTTP from internet (direct access)
  ingress {
    description = "Allow HTTP from internet"
    from_port   = 8080
    to_port     = 8080
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  # Allow outbound to RDS
  egress {
    description     = "Allow outbound to RDS"
    from_port       = 5432
    to_port         = 5432
    protocol        = "tcp"
    security_groups = [aws_security_group.rds.id]
  }

  # Allow outbound HTTPS for AWS services
  egress {
    description = "Allow HTTPS outbound"
    from_port   = 443
    to_port     = 443
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  # Allow outbound HTTP for package downloads (if needed)
  egress {
    description = "Allow HTTP outbound"
    from_port   = 80
    to_port     = 80
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = {
    Name        = "${var.project_name}-ecs-service-sg-${var.environment}"
    Environment = var.environment
    Project     = var.project_name
  }
}

# Update RDS Security Group to allow traffic from ECS
resource "aws_security_group_rule" "rds_from_ecs" {
  type                     = "ingress"
  from_port                = 5432
  to_port                  = 5432
  protocol                 = "tcp"
  source_security_group_id = aws_security_group.ecs_service.id
  security_group_id        = aws_security_group.rds.id
  description              = "Allow PostgreSQL from ECS service"
}

