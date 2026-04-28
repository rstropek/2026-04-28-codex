# Initial HelloCodex Application Skeleton

## Summary

Create the greenfield solution using .NET 10, EF Core SQLite, xUnit, Vite vanilla TypeScript, Vitest, and Biome. Standardize spelling on Questionnaire / Questionnaires everywhere, and use Vite’s dev-server proxy so
frontend code calls same-origin /api.

## Key Changes

- Create HelloCodex.slnx with:
    - HelloCodex.Api from dotnet new webapi
    - HelloCodex.Data as a class library for EF Core model/context
    - HelloCodex.Api.Tests and HelloCodex.Data.Tests from dotnet new xunit
    - HelloCodex.Web from npm create vite@latest HelloCodex.Web -- --template vanilla-ts
- Add EF Core SQLite data model:
    - Questionnaire entity with surrogate Id, Code max length 50, Description max length 200
    - DataContext with DbSet<Questionnaire>
    - initial EF migration creating Questionnaires
    - update database to create /Users/rstropek/live/2026-04-28-codex/HelloCodex/Questionnaires.db
- Configure API:
    - GET /api/ping returns plain pong
    - API listens on localhost port 8080
    - no CORS setup needed for the frontend dev flow
    - SQLite connection string lives in appsettings.json as ConnectionStrings:QuestionnairesDatabase
- Configure frontend:
    - Vite dev server on localhost port 8081
    - Vite proxy maps /api to http://localhost:8080
    - fixed 1080px centered page, no responsive work
    - top menu with inert Questionnaires item
    - fetch /api/ping and display the response
- Add quality tooling:
    - Roslyn analyzers / warnings-as-errors for .NET projects
    - Vitest dummy frontend test
    - Biome lint/format config and npm scripts

## Public Interfaces

- HTTP endpoint: GET /api/ping returns pong.
- Frontend API call path: /api/ping.
- Database table: Questionnaires(Id, Code, Description).
- Config key: ConnectionStrings:QuestionnairesDatabase, defaulting to Data Source=/Users/rstropek/live/2026-04-28-codex/HelloCodex/Questionnaires.db.

## Test Plan

- Add one dummy true == true test in each test area:
    - HelloCodex.Api.Tests
    - HelloCodex.Data.Tests
    - HelloCodex.Web/src/**/*.test.ts
- Verify:
    - dotnet build passes without warnings/errors
    - dotnet test passes
    - dotnet format passes
    - npm run build passes
    - Vitest passes
    - Biome lint/format checks pass
    - database migration applies and creates Questionnaires.db

## Assumptions

- Use Questionnaires spelling everywhere, including database file name.
- Use a single SQLite connection string in appsettings.json.
- Keep the API minimal; no questionnaire CRUD endpoints yet.
- Vite proxy is for local development. If the frontend is later hosted separately from the API, production will need equivalent proxy/routing or CORS.