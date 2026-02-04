# Admin Permissions Fix - Summary

## Problem

Most APIs were returning 403 (Forbidden) even for the super admin user. This was caused by two critical issues:

### Issue 1: Role Name Mismatch

- **Seeded Role**: The database was seeding roles using the UserRole enum's `.ToString()` method, which produces `"Admin"`
- **Authorization Check**: The authorization logic was checking for `Roles.Administrator` constant, which is `"Administrator"`
- **Impact**: All authorization checks failed because users had role "Admin" but the system was looking for "Administrator"

### Issue 2: Super Admin Flag Not Set

- **Problem**: The `IsSuperAdmin` flag was hardcoded to `false` for all admin users in `ApplicationDbContextInitialiser.cs`
- **Impact**: The first admin from appsettings (the super admin) was not marked as such

## Changes Made

### 1. ApplicationDbContextInitialiser.cs

- **Line 105**: Changed role seeding to use `Domain.Constants.Roles.Administrator` instead of `"Admin"`
- **Line 887**: Added `isSuperAdmin` parameter to `CreateRoleSpecificEntity()` method
- **Line 925**: Changed `IsSuperAdmin = false` to `IsSuperAdmin = isSuperAdmin` (using parameter)
- **Line 170-179**: Modified the loop to pass `isSuperAdmin = true` for the first admin (from AdminSeed configuration)
- **Added**: `GetRoleName()` helper method to map `UserRole` enum values to proper role constants
- **Line 159**: Changed `userRole.ToString()` to `GetRoleName(userRole)` for proper role assignment

### 2. IdentityService.cs

- **Added**: `GetRoleName()` helper method to consistently map `UserRole` enum to role constants
- **Line 140**: Fixed `RegisterUserAsync()` to use `GetRoleName(role)` instead of `role.ToString()`
- **Line 329**: Fixed `LoginWithGoogleAsync()` to use `GetRoleName(user.Role)`
- **Line 443**: Fixed `LoginWithFacebookAsync()` to use `GetRoleName(user.Role)`

## Migration Steps

### Option 1: Fresh Database (Recommended for Development)

1. Drop the existing database
2. Run the application - it will create a new database with correct roles
3. The seeded admin from appsettings will automatically have `IsSuperAdmin = true`

### Option 2: Update Existing Database

Run the provided SQL script: `fix-admin-permissions.sql`

This script will:

1. Rename the "Admin" role to "Administrator" in `AspNetRoles`
2. Update all user-role assignments in `AspNetUserRoles`
3. Set `IsSuperAdmin = true` for the admin user with email from appsettings (administrator@ask-a-muslim.com)
4. Verify the changes with SELECT statements

## Testing

After applying the fix:

1. **Login as super admin**:
   - Email: `administrator@ask-a-muslim.com` (from appsettings.json)
   - Password: `Administrator1!` (from appsettings.json)

2. **Verify access**:
   - All API endpoints should now be accessible
   - No more 403 errors for the super admin
   - The authorization logic at line 527-530 of `IdentityService.cs` checks for "Administrator" role and grants full access

## Key Files Modified

- `src/Infrastructure/Data/ApplicationDbContextInitialiser.cs`
- `src/Infrastructure/Identity/IdentityService.cs`
- `fix-admin-permissions.sql` (new file for database migration)

## Root Cause Analysis

The issue stemmed from an inconsistency between:

- **UserRole enum** (Domain/Enums/UserRole.cs) which has `Admin = 1`
- **Roles constants** (Domain/Constants/Roles.cs) which has `Administrator = "Administrator"`

When `.ToString()` was called on `UserRole.Admin`, it produced "Admin" instead of the expected "Administrator", causing all authorization checks to fail.

The fix introduces a proper mapping function (`GetRoleName()`) that consistently maps enum values to the correct role constants throughout the application.
