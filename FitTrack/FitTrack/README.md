# FitTrack

FitTrack is an ASP.NET Core MVC fitness application using SQLite, ASP.NET Identity, and Entity Framework Core.

## Run in Visual Studio

1. Open `FitTrack.slnx` in Visual Studio with the .NET 10 SDK installed.
2. Set the `FitTrack` web project as the startup project.
3. Run with HTTPS. The database is `FitTrack.db` in the project folder; startup applies pending EF Core migrations without replacing the existing database.
4. First visit opens the welcome page. Register or sign in, then complete the fitness profile setup to access the dashboard.

You can also run from this folder with `dotnet run --project FitTrack.csproj`.

## AI configuration (optional)

AI features are optional; the exercise library, plans, workout log, profile, and progress calendar continue to work without an AI key.

To configure Gemini, copy `.env.example` to `.env` in this project folder and replace `your_gemini_api_key_here` with a key from Google AI Studio. Keep `.env` private and never put a key in frontend code. The application loads environment variables without overwriting values already set in the process. Gemini is selected when `GEMINI_API_KEY` is configured; otherwise the app uses `OPENAI_API_KEY` if available. Model names can be overridden with `GEMINI_MODEL` or `OPENAI_MODEL`.

Gemini uses Google's OpenAI-compatible Chat Completions endpoint. A temporary HTTP 503 is retried with a short backoff; if the service remains unavailable, FitTrack shows a retry-later message without affecting saved data. HTTP 429 is reported as a provider quota/rate limit issue. See Google's [OpenAI compatibility guide](https://ai.google.dev/gemini-api/docs/openai/).

## Exercise catalog

The Exercise Library retrieves exercise, equipment, muscle, and category information from the public wger REST API (`/api/v2/exerciseinfo/`). Results are cached for eight hours; a previous successful catalog may be used during a temporary API outage. wger API documentation: https://wger.readthedocs.io/en/latest/api/api.html.

## Data and ownership

SQLite persists accounts, profiles, profile photos (under `wwwroot/uploads/profiles`), plans, completed workout logs, and progress measurements. User-owned controller actions scope records to the signed-in account. New database changes are maintained as EF Core migrations in `Migrations/`.
