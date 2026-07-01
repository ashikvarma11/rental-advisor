# Backend (ASP.NET Core .NET 9)

This folder contains the backend API for the Rental Market Analyzer prototype.

Quick run (local with dotnet):

1. Ensure .NET 9 SDK is installed.
2. Restore and run:

```powershell
cd backend
dotnet restore
dotnet run
```

The API listens on `http://localhost:5050` in local dev (see `Properties/launchSettings.json`).

ABR API key
- Set `ABR__ApiKey` as an environment variable for the backend (an ABR ABN Lookup GUID from https://abr.business.gov.au/Tools/WebServices). The `Enrich` endpoint calls the live ABR API when the key is configured. Do NOT store keys in the frontend.

Groq API key (AI clause analysis)
- Set `Groq__ApiKey` as an environment variable to enable LLM-based lease clause risk scoring (free tier, sign up at https://console.groq.com). Optionally set `Groq__Model` (defaults to `llama-3.3-70b-versatile`).
- Without a key, `POST /api/leases/{id}/extract-clauses` falls back to the built-in keyword heuristic — no error, just lower-quality scoring.
- Local dev (PowerShell): `$env:Groq__ApiKey = "your-key"` before `dotnet run`, or use `dotnet user-secrets set "Groq:ApiKey" "your-key"`.

Seeder endpoints
- `POST /api/import/suburbstats/seed` — read `data/abs_median_rent.csv` and seed `SuburbStats` if empty.
- `POST /api/import/listings/import-sample` — import `data/sample_listings.csv` into `Listings`.
- `POST /api/import/listings/upload` — upload a listings CSV (multipart form field `file`); validates rows and returns `{ created, errors }`.

