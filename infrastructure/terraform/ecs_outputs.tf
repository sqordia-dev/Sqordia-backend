# ECS Outputs (without ALB)

output "ecs_cluster_name" {
  description = "Name of the ECS cluster"
  value       = aws_ecs_cluster.main.name
}

output "ecs_service_name" {
  description = "Name of the ECS service"
  value       = aws_ecs_service.api.name
}

output "ecs_task_execution_role_arn" {
  description = "ARN of the ECS task execution role"
  value       = aws_iam_role.ecs_execution.arn
}

output "ecs_task_role_arn" {
  description = "ARN of the ECS task role"
  value       = aws_iam_role.ecs_task.arn
}

# Note: To get the public IP of your ECS task, use the helper script:
# .\scripts\get-ecs-task-ip.ps1
#
# Or manually:
# aws ecs list-tasks --cluster sqordia-cluster-production --service-name sqordia-api-production --region ca-central-1
# aws ecs describe-tasks --cluster sqordia-cluster-production --tasks <task-id> --region ca-central-1
# Then check the 'attachments' section for the public IP

