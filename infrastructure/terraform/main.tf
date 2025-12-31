terraform {
  required_version = ">= 1.0"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }

  # Optional: Use S3 backend for remote state (recommended for team collaboration)
  # Uncomment and configure when ready
  # backend "s3" {
  #   bucket = "sqordia-terraform-state"
  #   key    = "terraform.tfstate"
  #   region = "ca-central-1"
  # }
}

provider "aws" {
  region = var.aws_region

  default_tags {
    tags = {
      Project     = "Sqordia"
      Environment = var.environment
      ManagedBy   = "Terraform"
    }
  }
}

