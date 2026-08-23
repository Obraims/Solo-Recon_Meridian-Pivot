# Independent Learning & Blocker Log

## Technology / Concept

C# / ASP.NET Core Minimal API with SQLite Persistence

## Working Prototype

- **GitHub Repository**: [https://github.com/Obraims/Solo-Recon_Meridian-Pivot.git](https://github.com/Obraims/Solo-Recon_Meridian-Pivot.git)
- **Live Deployment**: [https://stocksync-solo-recon.fly.dev](https://stocksync-solo-recon.fly.dev)

## Resources Consulted

1. **Microsoft .NET Minimal API Documentation**  
   Learned ASP.NET Core route mapping (`app.MapGet`, `app.MapPost`, `app.MapPut`), dependency injection patterns, and middleware pipeline configuration.

2. **Microsoft.Data.Sqlite ADO.NET Guide**  
   Investigated parameterized SQL queries, connection lifecycle management (`SqliteConnection`), and transaction handling (`SqliteCommand`).

3. **C# Language Specification**  
   Applied C# 12 features including records, pattern matching expressions, async/await asynchronous Task workflows, and LINQ collection processing.

## What Broke

1. SQLite database tables did not exist on initial launch because the database file hadn't been initialized automatically.
2. ASP.NET Core static files middleware served cached static files in production container deployment on Fly.io.
3. API route mapping initially returned CORS and MIME type issues when fetched from the static frontend.
4. SQLite connection locking during rapid consecutive inventory update and sync calls.

## What I Tried First

- Executed raw SQL schema setup commands manually before starting the backend server process.
- Added `<meta>` cache control tags in HTML and cleared browser cache manually during testing.
- Wrapped database calls in inline connection blocks without centralized lifecycle control.

## What Fixed It

- Created a dedicated `DatabaseInitializer.cs` class executed on app startup within `Program.cs` (`app.Services.CreateScope()`) using `CREATE TABLE IF NOT EXISTS`.
- Configured global ASP.NET Core static file middleware options to append explicit `Cache-Control: no-cache, no-store, must-revalidate` response headers on production requests.
- Bundled UI styling and scripts cleanly into single-page static delivery to avoid static asset loading and MIME-type issues on containerized hosting.

## Time Lost

~3 hours spent troubleshooting static file middleware hosting, SQLite table initialization timing, and container deployment headers.

## What I Would Do Differently

- Implement database initialization and automated migration scripts from day 1 before writing endpoints.
- Validate static asset delivery behavior early in the containerization pipeline to avoid production caching issues.
