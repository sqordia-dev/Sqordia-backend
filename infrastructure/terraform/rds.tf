# RDS PostgreSQL Database Instance
resource "aws_db_instance" "postgresql" {
  identifier = "${var.project_name}-db-${var.environment}"

  # Engine configuration
  engine         = "postgres"
  # engine_version = "16.2" # Let AWS use default version (comment out to use latest available)
  instance_class = "db.t4g.micro" # Free tier eligible (ARM-based, more cost-effective)

  # Database configuration
  db_name  = var.rds_database_name
  username = "sqordia_admin"
  password = var.rds_password # Set via terraform.tfvars or environment variable

  # Storage configuration
  allocated_storage     = 20 # Free tier includes 20GB
  max_allocated_storage = 100 # Auto-scaling up to 100GB
  storage_type         = "gp3"
  storage_encrypted    = true

  # Network configuration
  db_subnet_group_name   = aws_db_subnet_group.main.name
  publicly_accessible    = false # Keep database private
  vpc_security_group_ids = [aws_security_group.rds.id]

  # Backup configuration
  backup_retention_period = 7 # 7 days of backups (free tier includes 20GB)
  backup_window          = "03:00-04:00" # UTC
  maintenance_window     = "mon:04:00-mon:05:00" # UTC

  # Performance and availability
  multi_az               = false # Single-AZ for cost savings (can enable later)
  performance_insights_enabled = false # Disable for cost savings
  deletion_protection    = false # Set to true in production after initial setup

  # Monitoring
  enabled_cloudwatch_logs_exports = ["postgresql", "upgrade"]
  monitoring_interval             = 0 # Disable enhanced monitoring for cost savings

  # Tags
  tags = {
    Name        = "${var.project_name}-postgresql-${var.environment}"
    Environment = var.environment
    Project     = var.project_name
  }

  # Skip final snapshot for easier cleanup (set to false in production)
  skip_final_snapshot = true

  # Apply changes immediately
  apply_immediately = true
}

# DB Subnet Group (required for RDS)
resource "aws_db_subnet_group" "main" {
  name       = "${var.project_name}-db-subnet-group-${var.environment}"
  subnet_ids = [aws_subnet.private_1.id, aws_subnet.private_2.id]

  tags = {
    Name        = "${var.project_name}-db-subnet-group"
    Environment = var.environment
    Project     = var.project_name
  }
}

# Security Group for RDS
resource "aws_security_group" "rds" {
  name        = "${var.project_name}-rds-sg-${var.environment}"
  description = "Security group for RDS PostgreSQL database"
  vpc_id      = aws_vpc.main.id

  # Allow PostgreSQL access from within VPC
  # Note: In production, restrict to specific security groups for Lightsail/Lambda
  ingress {
    description = "PostgreSQL from VPC"
    from_port   = 5432
    to_port     = 5432
    protocol    = "tcp"
    cidr_blocks = [aws_vpc.main.cidr_block] # Allow from entire VPC (refine in production)
  }

  egress {
    description = "Allow all outbound"
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = {
    Name        = "${var.project_name}-rds-sg"
    Environment = var.environment
    Project     = var.project_name
  }
}

# VPC for RDS (minimal VPC setup)
resource "aws_vpc" "main" {
  cidr_block           = "10.0.0.0/16"
  enable_dns_hostnames = true
  enable_dns_support   = true

  tags = {
    Name        = "${var.project_name}-vpc-${var.environment}"
    Environment = var.environment
    Project     = var.project_name
  }
}

# Internet Gateway (for potential future use)
resource "aws_internet_gateway" "main" {
  vpc_id = aws_vpc.main.id

  tags = {
    Name        = "${var.project_name}-igw-${var.environment}"
    Environment = var.environment
    Project     = var.project_name
  }
}

# Private Subnets for RDS (multi-AZ for high availability)
resource "aws_subnet" "private_1" {
  vpc_id            = aws_vpc.main.id
  cidr_block        = "10.0.1.0/24"
  availability_zone = "${var.aws_region}a"

  tags = {
    Name        = "${var.project_name}-private-subnet-1-${var.environment}"
    Environment = var.environment
    Project     = var.project_name
    Type        = "private"
  }
}

resource "aws_subnet" "private_2" {
  vpc_id            = aws_vpc.main.id
  cidr_block        = "10.0.2.0/24"
  availability_zone = "${var.aws_region}b"

  tags = {
    Name        = "${var.project_name}-private-subnet-2-${var.environment}"
    Environment = var.environment
    Project     = var.project_name
    Type        = "private"
  }
}

# Route Table for Private Subnets
resource "aws_route_table" "private" {
  vpc_id = aws_vpc.main.id

  tags = {
    Name        = "${var.project_name}-private-rt-${var.environment}"
    Environment = var.environment
    Project     = var.project_name
  }
}

# Associate Private Subnets with Route Table
resource "aws_route_table_association" "private_1" {
  subnet_id      = aws_subnet.private_1.id
  route_table_id = aws_route_table.private.id
}

resource "aws_route_table_association" "private_2" {
  subnet_id      = aws_subnet.private_2.id
  route_table_id = aws_route_table.private.id
}

