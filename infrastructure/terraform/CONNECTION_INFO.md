# RDS Connection Information

## Quick Reference

**Endpoint**: `sqordia-db-production.c326icw2wbit.ca-central-1.rds.amazonaws.com`  
**Port**: `5432`  
**Database**: `SqordiaDb`  
**Username**: `sqordia_admin`  
**Password**: [Set in `TF_VAR_rds_password` environment variable]

## Connection String

```
Host=sqordia-db-production.c326icw2wbit.ca-central-1.rds.amazonaws.com;Port=5432;Database=SqordiaDb;Username=sqordia_admin;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=true
```

## Get Connection Details from Terraform

```bash
# Get RDS endpoint
terraform output rds_endpoint

# Get RDS address
terraform output rds_address

# Get RDS port
terraform output rds_port

# Get all RDS outputs
terraform output | grep rds
```

## Important Notes

1. **SSL Required**: Always use `SSL Mode=Require` in connection strings
2. **Private Subnet**: RDS is in a private subnet - direct connection from your local machine may not work unless you're in the VPC
3. **Password**: The password was set during Terraform deployment via `TF_VAR_rds_password`
4. **Security**: Never commit passwords to Git - use environment variables or secrets management

## Next Steps

1. Test connection: `.\scripts\test-db-connection.ps1`
2. Run migrations: `dotnet ef database update`
3. Update application connection strings
4. See full guide: `docs/DATABASE_CONNECTION.md`

