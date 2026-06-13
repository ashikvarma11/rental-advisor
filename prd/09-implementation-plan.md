## Implementation Plan: Rental Market Analyzer & Lease Advisor

This document captures the step-by-step implementation plan for the MVP, including Azure (free tier) hosting details.

### Goal
Build a backend-first MVP that ingests curated ABS CSVs, offers a rent comparator API, provides lease upload + clause extraction (rule-based + LLM hook), and is hostable on Azure free-tier for demonstration.

### High-level steps
1. EF Core migrations & seeding
2. ABR enrichment client and caching
3. Comparator API and service
4. Lease upload + clause extraction (rule-based)
5. LLM adapter (backend-only) for summarization and QA
6. Angular 21 frontend MVP (Upload, Comparator, LeaseViewer)
7. Tests & CI (GitHub Actions)
8. Docker & Azure free-tier deployment
9. Docs & demo (README, .env.example)

### Detailed steps

1) Database: EF migrations & seeding (1–3h)
- Actions: ensure `Microsoft.EntityFrameworkCore.Design` is present; switch startup to `db.Database.Migrate()`; add initial migration (`dotnet ef migrations add InitialCreate`); run `dotnet ef database update` or rely on automatic `Migrate()` at startup.
- Files: `backend/Program.cs`, `backend/Data/AppDbContext.cs`, migration artifacts in `backend/Migrations/`.

2) ABR enrichment (4–6h)
- Implement `IABRClient`/`ABRClient` to call ABR web services or return cached demo data when no API key is configured. Use `IMemoryCache` for TTL caching.
- Expose `GET /api/enrich/abn/{abn}` via `EnrichController`.

3) Comparator API (6–10h)
- Implement `ComparatorService` and `ComparatorController` to calculate rent vs suburb median, percentile, and trend hints.

4) Lease upload & clause extraction — rule-based (8–12h)
- Implement `LeasesController` to accept uploads and persist `LeaseDocument` entries. Implement `ClauseExtractionService` using heuristics and regex to extract clauses and produce `Clause` records.

5) LLM hook & provider adapter (4–6h)
- Add `ILlmClient` and provider implementations (OpenAI/Azure). Keep keys backend-only and add caching/quotas.

6) Frontend MVP (24–40h)
- Scaffold Angular 21 app in `frontend/` and implement primary components plus API service.

7) Tests & CI (6–12h)
- Add backend unit tests and GitHub Actions workflows to build/test and deploy to Azure.

8) Docker & Azure Free-Tier hosting (2–4h)
- Frontend: Azure Static Web Apps (Free) to host the Angular build artifact.
- Backend: Azure App Service (F1 free tier) using the built-in .NET runtime (`DOTNET|9.0`). Deploy via `az webapp deploy` or GitHub Actions.
- SQLite note: For a reliable demo, seed the DB at startup from `data/*.csv`. For durable persistence consider Azure Files or managed DB.

9) Docs & demo (2–4h)
- Create `.env.example`, update README with run & deploy steps, and add ABR registration notes.

### Quick Azure commands (replace placeholders)

```bash
# Create resource group
az group create --name rental-rg --location australiaeast

# Create Static Web App for the frontend (GitHub repo required)
az staticwebapp create --name rental-frontend --resource-group rental-rg --repo https://github.com/<owner>/<repo> --branch main --app-artifact-location "dist/<app-name>" --location australiaeast

# Create App Service Plan (Free) and Web App for backend
az appservice plan create --name rental-plan --resource-group rental-rg --sku F1 --is-linux
az webapp create --resource-group rental-rg --plan rental-plan --name rental-backend --runtime "DOTNET|9.0"

# Publish backend and deploy
dotnet publish backend -c Release -o ./publish
az webapp deploy --resource-group rental-rg --name rental-backend --src-path ./publish

# Configure secrets / app settings
az webapp config appsettings set --resource-group rental-rg --name rental-backend --settings ABR__ApiKey=${ABR_KEY} AI__ApiKey=${AI_KEY}
```

### Verification checklist
1. `cd backend && dotnet ef database update` then `dotnet run` -> `GET /health` returns 200.
2. Run seed import -> `SuburbStats` and `Listings` present.
3. `GET /api/compare/suburb?...` returns expected comparator output.
4. Upload lease -> `GET /api/leases/{id}/clauses` returns clauses.
5. `docker-compose up --build` and Azure deploy reproduce demo.

### Notes & tradeoffs
- Free-tier App Service has limited storage; rely on startup seeding for demo. For production or reliable persistence use Azure Files or a managed DB.
- ABR and LLM keys must remain backend-only and should be stored in Key Vault or GitHub Secrets for CI.

---

This plan was exported from the session implementation plan and includes Azure free‑tier hosting guidance.
