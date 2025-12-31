#!/bin/bash
# Bash script to build and package Lambda functions for Terraform deployment
# Run this script before running terraform apply

set -e

echo "Building Lambda functions for deployment..."

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_PATH="$(cd "$SCRIPT_DIR/../.." && pwd)"
LAMBDA_PATH="$ROOT_PATH/src/Lambda"
TERRAFORM_PATH="$SCRIPT_DIR"

# Function to build and package a Lambda function
build_lambda_function() {
    local function_name=$1
    local project_path=$2
    
    echo ""
    echo "Building $function_name..."
    
    local publish_path="$project_path/publish"
    
    # Clean previous build
    if [ -d "$publish_path" ]; then
        rm -rf "$publish_path"
    fi
    
    # Publish the Lambda function
    cd "$project_path"
    dotnet publish -c Release -o publish
    cd - > /dev/null
    
    # Create ZIP file
    local zip_path="$TERRAFORM_PATH/$function_name.zip"
    if [ -f "$zip_path" ]; then
        rm -f "$zip_path"
    fi
    
    echo "Creating ZIP package: $zip_path"
    cd "$publish_path"
    zip -r "$zip_path" . > /dev/null
    cd - > /dev/null
    
    echo "✓ $function_name built and packaged successfully"
}

# Build Email Handler
EMAIL_HANDLER_PATH="$LAMBDA_PATH/EmailHandler/src/Sqordia.Lambda.EmailHandler"
if [ -d "$EMAIL_HANDLER_PATH" ]; then
    build_lambda_function "email-handler" "$EMAIL_HANDLER_PATH"
else
    echo "Warning: EmailHandler project not found at $EMAIL_HANDLER_PATH"
fi

# Build AI Generation Handler
AI_HANDLER_PATH="$LAMBDA_PATH/AIGenerationHandler/src/Sqordia.Lambda.AIGenerationHandler"
if [ -d "$AI_HANDLER_PATH" ]; then
    build_lambda_function "ai-generation-handler" "$AI_HANDLER_PATH"
else
    echo "Warning: AIGenerationHandler project not found at $AI_HANDLER_PATH"
fi

# Build Export Handler
EXPORT_HANDLER_PATH="$LAMBDA_PATH/ExportHandler/src/Sqordia.Lambda.ExportHandler"
if [ -d "$EXPORT_HANDLER_PATH" ]; then
    build_lambda_function "export-handler" "$EXPORT_HANDLER_PATH"
else
    echo "Warning: ExportHandler project not found at $EXPORT_HANDLER_PATH"
fi

echo ""
echo "✓ All Lambda functions built and packaged successfully!"
echo "You can now run: terraform plan"

