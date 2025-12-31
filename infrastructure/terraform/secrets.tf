# Secrets Manager for RDS Connection String

# Secrets Manager Secret for RDS Connection String
resource "aws_secretsmanager_secret" "rds_connection_string" {
  name        = "${var.project_name}-rds-connection-string-${var.environment}"
  description = "RDS PostgreSQL connection string for ECS tasks"

  tags = {
    Name        = "${var.project_name}-rds-connection-secret-${var.environment}"
    Environment = var.environment
    Project     = var.project_name
  }
}

# Secrets Manager Secret Version
# Store as PostgreSQL connection string format for .NET
resource "aws_secretsmanager_secret_version" "rds_connection_string" {
  secret_id = aws_secretsmanager_secret.rds_connection_string.id
  secret_string = "Host=${aws_db_instance.postgresql.address};Port=${aws_db_instance.postgresql.port};Database=${aws_db_instance.postgresql.db_name};Username=${aws_db_instance.postgresql.username};Password=${var.rds_password};SSL Mode=Require;Trust Server Certificate=true"

  depends_on = [aws_db_instance.postgresql]
}

