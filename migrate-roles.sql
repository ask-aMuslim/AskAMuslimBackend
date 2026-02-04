SET QUOTED_IDENTIFIER ON;
GO

-- Migrate users from 'Admin' role to 'Administrator' role
DECLARE @AdministratorRoleId NVARCHAR(450);
DECLARE @OldAdminRoleId NVARCHAR(450);

SELECT @AdministratorRoleId = Id FROM AspNetRoles WHERE Name = 'Administrator';
SELECT @OldAdminRoleId = Id FROM AspNetRoles WHERE Name = 'Admin';

-- Update user role assignments
UPDATE AspNetUserRoles 
SET RoleId = @AdministratorRoleId 
WHERE RoleId = @OldAdminRoleId;

-- Delete the old 'Admin' role
DELETE FROM AspNetRoles WHERE Id = @OldAdminRoleId;

-- Set IsSuperAdmin = 1 for the first admin
UPDATE Admins
SET IsSuperAdmin = 1
WHERE UserId IN (
    SELECT Id FROM Users WHERE Email = 'administrator@ask-a-muslim.com'
);

-- Verify changes
SELECT 'Roles after migration:' as Info;
SELECT Name, NormalizedName FROM AspNetRoles;

SELECT 'Admin users with Administrator role:' as Info;
SELECT u.Email, u.FirstName, u.LastName, r.Name as RoleName, a.IsSuperAdmin
FROM Users u
JOIN AspNetUserRoles ur ON u.Id = ur.UserId
JOIN AspNetRoles r ON ur.RoleId = r.Id
LEFT JOIN Admins a ON u.Id = a.UserId
WHERE r.Name = 'Administrator';
GO
