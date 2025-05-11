# ENSEK Technical Test

This is my submission for the ENSEK Remote Technical Test.

## Technologies Used

- C# / .NET 9
- ASP.NET Core Minimal API
- EF Core with SQLite
- Clean Architecture
- xUnit Tests (unit + integration)
- API Versioning (v1/v2)
- Swagger/OpenAPI Documentation


## Solution Structure

src/
├── Ensek.Api → Web API with upload endpoint
├── Ensek.Core → Shared models, DTOs, interfaces
├── Ensek.Infrastructure → DB context, seeding
└── Ensek.Services → Business logic/validation

tests/
├── Ensek.UnitTests → Service validator tests
└── Ensek.IntegrationTests → Full API endpoint tests

## How to Run

```bash
dotnet restore
dotnet build
dotnet run --project src/Ensek.Api
```

Open https://localhost:7223/docs to access Swagger UI

## How to Test
Run unit/integration tests:

```bash
dotnet test
```