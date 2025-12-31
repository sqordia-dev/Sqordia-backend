-- Create Admin User with All Permissions (PostgreSQL)
-- This script creates an admin user and assigns the Admin role

-- 1. Create Admin User
INSERT INTO "Users" (
    "Id", 
    "FirstName", 
    "LastName", 
    "Email", 
    "UserName", 
    "PasswordHash", 
    "IsEmailConfirmed", 
    "EmailConfirmedAt", 
    "IsActive", 
    "UserType", 
    "AccessFailedCount", 
    "LockoutEnabled", 
    "LockoutEnd",
    "PhoneNumberVerified",
    "RequirePasswordChange",
    "Provider",
    "PasswordLastChangedAt",
    "Created", 
    "IsDeleted"
)
VALUES (
    '758afb07-3e8c-4259-995f-42f05af13b78'::uuid,
    'Admin',
    'User',
    'admin@sqordia.com',
    'admin@sqordia.com',
    '$2a$11$mQQQUzX12zagdYn5YrbKN.PvvUZ8XHn7DsqjAvoYsXsVXJ6SSdKFa', -- Password: Sqordia2025!
    true, -- IsEmailConfirmed
    NOW() AT TIME ZONE 'UTC', -- EmailConfirmedAt
    true, -- IsActive
    'Entrepreneur', -- UserType (valid values: Entrepreneur, OBNL, Consultant)
    0, -- AccessFailedCount
    true, -- LockoutEnabled
    NULL, -- LockoutEnd (not locked)
    false, -- PhoneNumberVerified
    false, -- RequirePasswordChange
    'local', -- Provider
    NOW() AT TIME ZONE 'UTC', -- PasswordLastChangedAt
    NOW() AT TIME ZONE 'UTC', -- Created
    false -- IsDeleted
);

-- 2. Assign Admin Role to Admin User
INSERT INTO "UserRoles" ("Id", "UserId", "RoleId")
VALUES (
    gen_random_uuid(),
    '758afb07-3e8c-4259-995f-42f05af13b78'::uuid,
    '6FE80855-70FF-4863-92B1-7EE266426DEE'::uuid -- Admin role
);

-- 3. Add Admin User to Default Organization (if organization exists)
INSERT INTO "OrganizationMembers" (
    "Id", 
    "OrganizationId", 
    "UserId", 
    "Role", 
    "IsActive", 
    "JoinedAt", 
    "Created", 
    "IsDeleted"
)
SELECT 
    gen_random_uuid(),
    o."Id",
    '758afb07-3e8c-4259-995f-42f05af13b78'::uuid,
    'Admin',
    true, -- IsActive
    NOW() AT TIME ZONE 'UTC', -- JoinedAt
    NOW() AT TIME ZONE 'UTC', -- Created
    false -- IsDeleted
FROM "Organizations" o
WHERE o."Name" = 'Sqordia Default Organization';

DO $$
BEGIN
    RAISE NOTICE 'Admin user created successfully!';
    RAISE NOTICE 'Email: admin@sqordia.com';
    RAISE NOTICE 'Password: Sqordia2025!';
    RAISE NOTICE 'Role: Admin (with all permissions)';
END $$;

