## Tech Stack

* Backend:
  * C# with .NET 10
  * xUnit for unit testing
  * RESTful API using ASP.NET Core Minimal API
  * Open API (Swagger) for API documentation
  * Ensure high code quality with Roslyn Analyzers
  * C# code formatting with `dotnet format`
* DB:
  * SQLite
  * Access SQLite via EFCore
  * xUnit for testing the database layer
* Frontend:
  * HTML, CSS, TypeScript
  * No frontend framework, just vanilla TypeScript
  * Vite for bundling and development server
  * Vitest for testing
  * npm for package management
  * Biomejs for linting and formatting

## Project Layout

Prefer the following folder and project structure:

* `HelloCodex.slnx` - solution file at the repository root (note: slnx, not sln)
* `HelloCodex.Api` - ASP.NET Core Minimal API project
* `HelloCodex.Data` - EF Core and SQLite data access project, business logic
  * `Model.cs` - EF Core entity classes
  * `DataContext.cs` - EF Core DbContext class
  * ... - other data access and business logic classes
* `HelloCodex.Web` - Vite frontend with vanilla TypeScript
  * `src/` - TypeScript source files
* `HelloCodex.Api.Tests` - API and endpoint tests
* `HelloCodex.Data.Tests` - database layer tests using xUnit
* `HelloCodex.Web/src/**/*.test.ts` - frontend tests colocated with the TypeScript code they cover

## URL Schema

* .NET API should run on port 8080 on localhost
* API endpoints should be prefixed with `/api`
* Frontend should run on port 8081 on localhost
* API project allows CORS with all origins, all methods, and all headers to enable frontend development without CORS issues

## Documentation and Code Samples

Use the Microsoft Learn CLI (NOT MCP) to find documentation and code samples for any Microsoft SDKs and languages, including C#, .NET, SQL Server, Azure, and more. Only use Context7 for Microsoft documentation if you cannot find the necessary information through the Learn CLI.

For all other topics, use the Context7 CLI to find documentation and code samples.

## Quality Checklist

After changing any code, always ensure:

* Build without warnings and errors. Warnings must be treated as errors and resolved before completing the work.
* Code analysis and/or linting checks pass
* Code is auto-formatted
* All tests pass
