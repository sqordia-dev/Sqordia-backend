# Terraform Installation Guide

## 🚀 Quick Install (Windows)

### Option 1: Using winget (Recommended)

Open PowerShell as Administrator and run:

```powershell
winget install --id HashiCorp.Terraform --accept-package-agreements --accept-source-agreements
```

After installation, **close and reopen PowerShell** to refresh the PATH.

### Option 2: Manual Installation

1. Download Terraform:
   - Go to: https://www.terraform.io/downloads
   - Download Windows 64-bit version

2. Extract and add to PATH:
   - Extract the zip file
   - Copy `terraform.exe` to a folder (e.g., `C:\terraform`)
   - Add that folder to your system PATH:
     - Search "Environment Variables" in Windows
     - Edit "Path" variable
     - Add the folder containing terraform.exe

3. Verify installation:
   ```powershell
   terraform version
   ```

## ✅ Verify Installation

After installation, open a **new PowerShell window** and run:

```powershell
terraform version
```

You should see something like:
```
Terraform v1.6.0
```

## 🚀 After Installation

Once Terraform is installed, you can proceed with deployment:

```powershell
cd "C:\Users\User\OneDrive - McGill University\Documents\GitHub\Sqordia-backend\infrastructure\terraform"
terraform init
terraform plan
terraform apply
```

Or use the deployment script:

```powershell
cd "C:\Users\User\OneDrive - McGill University\Documents\GitHub\Sqordia-backend"
.\scripts\deploy-ecs.ps1
```

## 🔧 Troubleshooting

### "terraform is not recognized"

1. **Close and reopen PowerShell** (PATH needs to refresh)
2. Verify installation:
   ```powershell
   Get-Command terraform
   ```
3. If still not found, manually add to PATH (see Option 2 above)

### Installation fails

- Try running PowerShell as Administrator
- Or download manually from terraform.io

