# ENSEK Technical Test

This is my submission for the ENSEK Remote Technical Test. It is a .NET 9 Web API application that enables users to upload meter reading CSV files, validates them against seeded account data, and stores valid readings in a database.

## Overview

This API accepts CSV files containing customer meter readings, validates them against current accounts, and stores valid entries.

It includes:

- API versioning (v1, v2) — only v1 is implemented,  however the solution is ready for several versions.  
- File validation (format, size and content)  
- Comprehensive validation rules  
- Complete test coverage (unit and integration).  
- A React + TypeScript frontend for uploading files and viewing results  

## Features

- Accepts CSV file uploads `/meter-reading-uploads`
- Validates:
  - Account existence
  - Meter value format (`NNNNN`)
  - No duplicate readings
  - Prevents older readings than existing ones
- Returns:
  - The number of successful and failed readings
  - A list of failed rows with reasons
- Supports API versioning
- Includes unit and integration tests
- Frontend UI built with React + TypeScript
- Minimal GitHub Actions Workflow with build and test

## Technologies Used

### Architecture & Backend

- Clean Architecture  
- C# / .NET 9  
- ASP.NET Core Minimal API  
- EF Core with SQLite  
- API Versioning (v1/v2)  
- Swagger/OpenAPI Documentation  

### Frontend

- React, TypeScript, Axios, Bootstrap

### Testing

- xUnit Tests (unit + integration)  
- InMemoryDatabase WITH SQLite

### Build

- GitHub Actions for CI pipeline — currently it's a workflow to restore packages, build, run tests, and upload results.

## Solution Structure

```
src/
├── Ensek.Api             → Web API with upload endpoint
├── Ensek.Core            → Shared models, DTOs, interfaces
├── Ensek.Infrastructure  → DB context, seeding
└── Ensek.Services        → Business logic/validation

tests/
├── Ensek.UnitTests       → Service validator tests
├── Ensek.IntegrationTests → Full API endpoint tests
└── Ensek.Test.Helpers    → Shared helpers between test cases 

ui/
└── ensek-react-ui        → React with TypeScript to consume the API
```

## How to Run

### Backend

```bash
cd src\Ensek.Api\
dotnet run
```

The API will start on port  http://localhost:5166 by default or https://localhost:7223.  
Access the Swagger UI at: [https://localhost:7223/docs](https://localhost:7223/docs)

### Running Tests

```bash
cd tests\Ensek.UnitTests\
dotnet test

cd tests\Ensek.IntegrationTests
dotnet test
```

### Frontend

```bash
cd ui\ensek-react-ui
npm install
npm start
```

The frontend runs on `http://localhost:3000` by default.

## Deployment

This solution can be deployed to:

- Azure App Services  
- Docker container  
- Any .NET hosting provider  

### CI/CD

#### GitHub Actions (Already Implemented)

A GitHub Actions workflow that:

- Restores NuGet packages  
- Builds the project  
- Runs all tests (`dotnet test`)  
- Uploads test results for visibility  

#### Planned Enhancements

- Full CI/CD pipeline:
  - Add running the migrations to the CI/CD and remove it from the application startup.
  - Trigger builds/tests on every PR or push  
  - Deploy to staging/production on merge to `main`
- Deployment targets:
  - Azure App Service  
  - Docker container registry  
  - GitHub Pages (for frontend)  

## Future Improvements

- Structured logging with **Serilog** 
- Use caching to reduce DB load and improve response times. **Redis**
- Enhanced Swagger documentation (including XML comments for Minimal APIs)  
- Distributed locking to handle concurrent uploads could use **Redis-based Distributed Lock**
- Background processing via **Hangfire**  
- Authentication and rate limiting 
- Performance monitoring with **Datadog**