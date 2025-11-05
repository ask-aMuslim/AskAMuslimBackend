# Quick Start Guide - Authentication

## Running the Application

1. **Restore packages:**

   ```powershell
   dotnet restore
   ```

2. **Build the solution:**

   ```powershell
   dotnet build
   ```

3. **Run the application:**

   ```powershell
   cd src\Web
   dotnet run
   ```

4. **Access Swagger UI:**
   Open browser: `https://localhost:<port>/api`

## Quick Test with cURL

### Register a new student:

```powershell
curl -X POST "https://localhost:<port>/authentication/register" `
  -H "Content-Type: application/json" `
  -d '{
    "email": "test@example.com",
    "password": "Test123!",
    "firstName": "Test",
    "lastName": "User",
    "role": 2
  }'
```

### Login:

```powershell
curl -X POST "https://localhost:<port>/authentication/login" `
  -H "Content-Type: application/json" `
  -d '{
    "email": "test@example.com",
    "password": "Test123!"
  }'
```

### Access protected endpoint:

```powershell
curl -X GET "https://localhost:<port>/todoitems" `
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

## Testing with Pre-seeded Users

Use these credentials to test immediately:

**Administrator:**

- Email: `administrator@localhost`
- Password: `Administrator1!`

**Student:**

- Email: `student@localhost`
- Password: `Student1!`

**Instructor:**

- Email: `instructor@localhost`
- Password: `Instructor1!`

**NonMuslim:**

- Email: `nonmuslim@localhost`
- Password: `NonMuslim1!`

## Database Connection

Ensure your SQL Server is running. The default connection string is:

```
Server=.;Database=AskAMuslimBackendDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=true
```

Update in `src/Web/appsettings.Development.json` if needed.
