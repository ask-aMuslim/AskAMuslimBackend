-- Fix Admin Permissions Script
-- This script fixes the role mismatch and sets the super admin flag

-- Step 1: Update role name from 'Admin' to 'Administrator' in AspNetRoles
UPDATE AspNetRoles
SET Name = 'Administrator', NormalizedName = 'ADMINISTRATOR'
WHERE Name = 'Admin';

-- Step 2: Update user roles from 'Admin' to 'Administrator' in AspNetRoles (if there are separate entries)
IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE Name = 'Administrator')
BEGIN
    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
    SELECT NEWID(), 'Administrator', 'ADMINISTRATOR', NEWID()
    WHERE NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE Name = 'Administrator');
END

-- Step 3: Update AspNetUserRoles to reference the correct role
-- First, get the new Administrator role ID
DECLARE @AdministratorRoleId NVARCHAR(450);
DECLARE @OldAdminRoleId NVARCHAR(450);

SELECT @AdministratorRoleId = Id FROM AspNetRoles WHERE Name = 'Administrator';
SELECT @OldAdminRoleId = Id FROM AspNetRoles WHERE Name = 'Admin';

-- Update user role assignments if there's an old 'Admin' role
IF @OldAdminRoleId IS NOT NULL AND @AdministratorRoleId IS NOT NULL AND @OldAdminRoleId != @AdministratorRoleId
BEGIN
    UPDATE AspNetUserRoles
    SET RoleId = @AdministratorRoleId
    WHERE RoleId = @OldAdminRoleId;
    
    -- Delete the old 'Admin' role
    DELETE FROM AspNetRoles WHERE Id = @OldAdminRoleId;
END

-- Step 4: Set IsSuperAdmin = true for the admin from appsettings (administrator@ask-a-muslim.com)
UPDATE Admins
SET IsSuperAdmin = 1
WHERE UserId IN (
    SELECT Id FROM AspNetUsers WHERE Email = 'administrator@ask-a-muslim.com'
);

-- Alternative: If you want to set the first admin as super admin regardless of email
-- UPDATE Admins
-- SET IsSuperAdmin = 1
-- WHERE Id = (SELECT TOP 1 Id FROM Admins ORDER BY JoinedDate);

-- Step 5: Verify the changes
SELECT 'Roles:' as Info;
SELECT Name, NormalizedName FROM AspNetRoles;

SELECT 'Super Admins:' as Info;
SELECT u.Email, u.FirstName, u.LastName, a.IsSuperAdmin, a.JoinedDate
FROM Admins a
JOIN AspNetUsers u ON a.UserId = u.Id
WHERE a.IsSuperAdmin = 1;

SELECT 'All Admins with their roles:' as Info;
SELECT u.Email, u.FirstName, u.LastName, r.Name as RoleName, a.IsSuperAdmin
FROM AspNetUsers u
JOIN AspNetUserRoles ur ON u.Id = ur.UserId
JOIN AspNetRoles r ON ur.RoleId = r.Id
LEFT JOIN Admins a ON u.Id = a.UserId
WHERE r.Name = 'Administrator';
