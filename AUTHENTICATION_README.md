# Authentication System Documentation

## Overview

This authentication system implements custom Login and Registration functionality for the AskAMuslimBackend application using CQRS pattern and JWT tokens.

## User Roles

The system supports four user roles:

1. **Admin** - Administrator with full system access
2. **Student** - Regular student users
3. **Instructor** - Course instructors
4. **NonMuslim** - Non-Muslim users who want to learn

## API Endpoints

### 1. Register

**Endpoint:** `POST /authentication/register`

**Request Body:**

```json
{
  "email": "user@example.com",
  "password": "Password123!",
  "firstName": "John",
  "lastName": "Doe",
  "role": 2
}
```

**Role Values:**

- `1` = Admin
- `2` = Student
- `3` = Instructor
- `4` = NonMuslim

**Success Response (200 OK):**

```json
{
  "userId": "guid-here",
  "email": "user@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "role": 2,
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2025-11-06T10:30:00Z"
}
```

**Error Response (400 Bad Request):**

```json
["User with this email already exists."]
```

### 2. Login

**Endpoint:** `POST /authentication/login`

**Request Body:**

```json
{
  "email": "user@example.com",
  "password": "Password123!"
}
```

**Success Response (200 OK):**

```json
{
  "userId": "guid-here",
  "email": "user@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "role": 2,
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2025-11-06T10:30:00Z"
}
```

**Error Response (401 Unauthorized):**
Invalid email or password.

## Using JWT Token

After successful login or registration, use the returned JWT token in subsequent API requests:

**Header:**

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

## Seeded Test Users

The following test users are automatically created during database initialization:

| Email                   | Password        | Role       | First Name | Last Name  |
| ----------------------- | --------------- | ---------- | ---------- | ---------- |
| administrator@localhost | Administrator1! | Admin      | Admin      | User       |
| student@localhost       | Student1!       | Student    | John       | Student    |
| nonmuslim@localhost     | NonMuslim1!     | NonMuslim  | Jane       | NonMuslim  |
| instructor@localhost    | Instructor1!    | Instructor | Bob        | Instructor |

## Configuration

### JWT Settings (appsettings.json)

```json
{
  "JwtSettings": {
    "SecretKey": "YourSuperSecretKeyForDevelopmentPurposeOnly123456!",
    "Issuer": "AskAMuslimBackend",
    "Audience": "AskAMuslimBackend"
  }
}
```

**Important:** Change the `SecretKey` in production to a secure random key!

## Architecture

### CQRS Implementation

**Commands:**

- `RegisterCommand` - Handles user registration
- `LoginCommand` - Handles user authentication

**Location:**

- Commands: `src/Application/Authentication/Commands/`
- Validators: Each command has a corresponding validator using FluentValidation
- Handlers: Handlers are in the same file as the command

### Domain Layer

- **UserRole Enum:** `src/Domain/Enums/UserRole.cs`
- **Roles Constants:** `src/Domain/Constants/Roles.cs`

### Infrastructure Layer

- **ApplicationUser:** `src/Infrastructure/Identity/ApplicationUser.cs`
  - Extended with `FirstName`, `LastName`, and `Role` properties
- **IdentityService:** `src/Infrastructure/Identity/IdentityService.cs`
  - Implements JWT token generation
  - Handles user registration and login logic

### Web/Presentation Layer

- **Endpoints:** `src/Web/Endpoints/Authentication.cs`
  - Maps register and login routes
  - Uses minimal API pattern

## Security Features

1. **Password Validation:** Minimum 6 characters (configurable in Identity options)
2. **Email Validation:** Email format validation using FluentValidation
3. **JWT Tokens:** 24-hour expiration (configurable)
4. **Role-based Authorization:** Claims-based with role support
5. **Email Confirmation:** Auto-confirmed for development (should be changed for production)

## Testing with Swagger/Postman

1. Navigate to `/api` in your browser to access Swagger UI
2. Use the Register endpoint to create a new user
3. Copy the returned JWT token
4. Click "Authorize" button in Swagger
5. Enter: `Bearer {your-token-here}`
6. Now you can access protected endpoints

## Future Enhancements

Consider adding:

- [ ] Email verification workflow
- [ ] Password reset functionality
- [ ] Refresh token mechanism
- [ ] Two-factor authentication
- [ ] Account lockout after failed attempts
- [ ] Password complexity requirements
- [ ] Social login (Google, Facebook, etc.)
- [ ] User profile management endpoints

## Troubleshooting

### Common Issues

1. **401 Unauthorized**: Token expired or invalid. Login again to get a new token.
2. **400 Bad Request**: Check validation errors in the response body.
3. **Role not working**: Ensure role value is between 1-4.
4. **Database errors**: Ensure database is initialized with `InitialiseDatabaseAsync()`.

## Example Usage (C# Client)

```csharp
// Register
var registerRequest = new RegisterCommand
{
    Email = "newuser@example.com",
    Password = "Password123!",
    FirstName = "Jane",
    LastName = "Doe",
    Role = UserRole.Student
};

var registerResponse = await httpClient.PostAsJsonAsync("/authentication/register", registerRequest);
var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthenticationResponse>();

// Login
var loginRequest = new LoginCommand
{
    Email = "newuser@example.com",
    Password = "Password123!"
};

var loginResponse = await httpClient.PostAsJsonAsync("/authentication/login", loginRequest);
var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthenticationResponse>();

// Use token for subsequent requests
httpClient.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", authResponse.Token);
```
