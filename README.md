# Rental Advisor

A full-stack app for comparing rental listings and analyzing lease clauses for risk, with suburb rent stats and optional LLM-based clause scoring.

## Stack
- **Backend:** ASP.NET Core (.NET 9), EF Core, SQLite
- **Frontend:** Angular
- **AI:** Groq API for lease clause risk analysis (optional, falls back to keyword heuristic)

## Structure
- `backend/` — API, EF Core models/migrations, CSV importers, Groq clause analyzer
- `rental-advisor-frontend/` — Angular app (home, comparator, lease upload, clauses)
- `data/` — sample CSVs and lease text used for import/testing
- `docker-compose.yml` — runs backend + frontend together

## Running locally

### Backend
```powershell
cd backend
dotnet restore
dotnet run
```
Listens on `http://localhost:5050`. See [backend/README.md](backend/README.md) for API keys (ABR, Groq) and seed endpoints.

### Frontend
```powershell
cd rental-advisor-frontend
npm install
npm start
```

### Docker
```powershell
docker-compose up --build
```
Set `ABR_API_KEY` and `GROQ_API_KEY` in your environment or an `.env` file before running.
