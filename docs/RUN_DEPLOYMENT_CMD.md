# Running Deployment from CMD (Command Prompt)

## 🚀 Option 1: Run PowerShell Script from CMD

Open **Command Prompt (CMD)** and run:

```cmd
cd "C:\Users\User\OneDrive - McGill University\Documents\GitHub\Sqordia-backend"
powershell.exe -ExecutionPolicy Bypass -File .\scripts\deploy-ecs.ps1
```

## 🚀 Option 2: Run Terraform Commands Directly in CMD

If you prefer to run commands directly in CMD:

```cmd
cd "C:\Users\User\OneDrive - McGill University\Documents\GitHub\Sqordia-backend\infrastructure\terraform"

terraform init
terraform plan
terraform apply
```

## 📋 Step-by-Step Deployment in CMD

### Step 1: Open CMD
- Press `Win + R`
- Type `cmd` and press Enter
- Or search "Command Prompt" in Windows

### Step 2: Navigate to Project
```cmd
cd "C:\Users\User\OneDrive - McGill University\Documents\GitHub\Sqordia-backend"
```

### Step 3: Run Deployment

**Option A: Use PowerShell Script**
```cmd
powershell.exe -ExecutionPolicy Bypass -File .\scripts\deploy-ecs.ps1
```

**Option B: Run Terraform Manually**
```cmd
cd infrastructure\terraform
terraform init
terraform plan
terraform apply
```

## ✅ After Deployment

Get your API URL:
```cmd
powershell.exe -ExecutionPolicy Bypass -File .\scripts\get-ecs-task-ip.ps1
```

## 🔧 Troubleshooting

### "terraform is not recognized"
- Make sure Terraform is installed
- Restart CMD after installing Terraform
- Verify: `terraform version`

### "ExecutionPolicy" error
- Use the `-ExecutionPolicy Bypass` flag (shown above)
- Or run PowerShell as Administrator and set policy:
  ```powershell
  Set-ExecutionPolicy RemoteSigned -Scope CurrentUser
  ```

### Path with spaces
- Always use quotes around paths with spaces
- Example: `cd "C:\Users\User\OneDrive - McGill University\..."`

