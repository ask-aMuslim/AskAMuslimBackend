-- Check SuperAdmin status and permissions

-- 1. Check if user is marked as SuperAdmin
SELECT 
    u.Id as UserId,
    u.Email,
    u.FirstName,
    u.LastName,
    u.Role,
    a.IsSuperAdmin,
    a.Id as AdminId
FROM Users u
LEFT JOIN Admins a ON a.UserId = u.Id
WHERE u.Role = 0 -- 0 = Admin role
ORDER BY u.CreatedAt;

-- 2. Check UserPermissions table for SuperAdmin
SELECT 
    up.UserId,
    u.Email,
    up.Permissions,
    up.GrantedDate,
    up.Notes,
    -- Check if permissions value equals AllPermissions (-1 or max long value)
    CASE 
        WHEN up.Permissions = -1 THEN 'Has AllPermissions'
        ELSE 'Custom Permissions: ' + CAST(up.Permissions AS VARCHAR)
    END as PermissionStatus
FROM UserPermissions up
INNER JOIN Users u ON u.Id = up.UserId
WHERE u.Role = 0; -- Admin role

-- 3. Check if AllPermissions value is correct (-1 represents all bits set)
SELECT 
    up.UserId,
    u.Email,
    up.Permissions as PermissionsValue,
    CAST(up.Permissions AS BINARY(8)) as PermissionsBinary,
    -- Test specific permission bits
    CASE WHEN (up.Permissions & 1) = 1 THEN 'YES' ELSE 'NO' END as HasViewDashboard,
    CASE WHEN (up.Permissions & 2) = 2 THEN 'YES' ELSE 'NO' END as HasViewAnalytics
FROM UserPermissions up  
INNER JOIN Users u ON u.Id = up.UserId
WHERE u.Role = 0;
