-- ============================================================
-- STEP 1: Check what admin users exist
-- ============================================================
SELECT u.Id, u.Email, u.UserName, u.FirstName, u.LastName, u.Role, u.EmailConfirmed, u.IsActive, a.IsSuperAdmin
FROM AspNetUsers u
LEFT JOIN Admins a ON u.Id = a.UserId
WHERE u.Email LIKE '%administrator%' OR u.Role = 0; -- Role 0 is Admin

-- ============================================================
-- STEP 2: DELETE the existing admin user to allow re-seeding
-- WARNING: This will delete the admin user and their related data!
-- Run this if you see administrator@ask-a-muslim.com above
-- ============================================================

-- Delete Admin entity first (foreign key constraint)
DELETE FROM Admins 
WHERE UserId IN (SELECT Id FROM AspNetUsers WHERE Email LIKE '%administrator%');

-- Delete user roles
DELETE FROM AspNetUserRoles 
WHERE UserId IN (SELECT Id FROM AspNetUsers WHERE Email LIKE '%administrator%');

-- Delete user claims if any
DELETE FROM AspNetUserClaims 
WHERE UserId IN (SELECT Id FROM AspNetUsers WHERE Email LIKE '%administrator%');

-- Delete the user
DELETE FROM AspNetUsers 
WHERE Email LIKE '%administrator%';

-- ============================================================
-- STEP 3: Verify deletion
-- ============================================================
SELECT COUNT(*) AS RemainingAdmins
FROM AspNetUsers 
WHERE Email LIKE '%administrator%';
-- Should return 0

-- ============================================================
-- STEP 4: Restart your application
-- The seeder will now create the SuperAdmin with correct credentials:
-- Email: administrator@ask-a-muslim.com
-- Password: P@ssw0rd
-- ============================================================
