# Smart Contract Lifecycle Management System

A .NET 8 Web API for managing contract creation, approval workflows, version history, audit tracking, and analytics.

## Tech Stack
- ASP.NET Core Web API (.NET 8)
- Entity Framework Core + SQL Server
- JWT authentication and role-based authorization
- Swagger / OpenAPI

## Solution Structure
- `/CLM.API` - API host, controllers, middleware, startup configuration
- `/CLM.Core` - domain entities, DTOs, enums, service/repository interfaces
- `/CLM.Infrastructure` - EF Core data access, repositories, services, background tasks

## Core Features
- User authentication (`register`, `login`) with JWT tokens
- Role-based access (`Admin`, `Manager`, `User`)
- Contract lifecycle management (create, update, delete, versions)
- Two-step approvals (Manager then Admin)
- Audit log tracking
- Contract analytics summary endpoint
- Contract upload/download support
- Background expiry checks with optional email reminders

## API Modules
- `/api/auth`
- `/api/contracts`
- `/api/approvals`
- `/api/auditlogs`
- `/api/analytics`

Swagger UI is available at `/swagger` when running in Development.

## Getting Started
> ⚠️ **Security notice:** This project seeds default credentials for bootstrapping. Do **not** use seeded credentials in production, and rotate them immediately after first startup (including local/shared environments).

### Prerequisites
- .NET SDK 8.0+
- SQL Server

### Configuration
Edit `/CLM.API/appsettings.json`:
- `ConnectionStrings:DefaultConnection`
- `Jwt:Secret`, `Jwt:Issuer`, `Jwt:Audience`
- `Email:*` (only required if enabling email notifications)

### Run
```bash
dotnet restore ./CLM.sln
dotnet run --project ./CLM.API/CLM.API.csproj
```

On startup, the app runs database migrations and seeds default users (if missing):
- `admin@clm.com` / `Admin@123`
- `manager@clm.com` / `Manager@123`

> ⚠️ Change these seeded passwords immediately after initial setup.

## Development Notes
- CORS is configured for `http://localhost:5173` and `http://localhost:3000`.
- Static files are served from the API host.
