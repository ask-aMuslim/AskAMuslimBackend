-- Cleanup script for OAuth test users
-- Run this if you have partially created test users from OAuth login attempts

-- Find OAuth test users (users with synthetic Facebook emails or test Google emails)
SELECT Id, Email, FirstName, LastName, Role, CreatedAt
FROM AspNetUsers
WHERE Email LIKE 'fb_%@%' OR Email IN (
    -- Add any test Google/Facebook emails here
    'test@gmail.com'
);

-- To delete a specific test user and all related data:
-- Replace 'USER_EMAIL_HERE' with the actual email

/*
DECLARE @UserId uniqueidentifier;

SELECT @UserId = Id FROM AspNetUsers WHERE Email = 'USER_EMAIL_HERE';

-- Delete related entities
DELETE FROM Students WHERE UserId = @UserId;
DELETE FROM Instructors WHERE UserId = @UserId;
DELETE FROM Admins WHERE UserId = @UserId;
DELETE FROM Preachers WHERE UserId = @UserId;
DELETE FROM UserPermissions WHERE UserId = @UserId;
DELETE FROM AspNetUserRoles WHERE UserId = @UserId;

-- Delete the user
DELETE FROM AspNetUsers WHERE Id = @UserId;

PRINT 'User deleted successfully';
*/
