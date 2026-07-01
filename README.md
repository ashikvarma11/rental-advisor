<div align="center">

<img src="docs/assets/banner.png" alt="RentAdvisor" width="100%">

### Know your rent. Read your lease.

Compare rental listings against real ABS suburb data, verify landlords via the ABR,
and let an AI-assisted reader flag risky lease clauses — before you sign.

**[🚀 Try it live](https://ashikvarma11.github.io/rental-advisor/)**

[![Live Demo](https://img.shields.io/badge/demo-live-8ea37b?style=for-the-badge)](https://ashikvarma11.github.io/rental-advisor/)
[![License: MIT](https://img.shields.io/badge/license-MIT-ff5a1f?style=for-the-badge)](LICENSE)

[![.NET](https://img.shields.io/badge/.NET_9-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-239120?style=flat-square&logo=csharp&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)
[![Angular](https://img.shields.io/badge/Angular-DD0031?style=flat-square&logo=angular&logoColor=white)](https://angular.dev/)
[![TypeScript](https://img.shields.io/badge/TypeScript-3178C6?style=flat-square&logo=typescript&logoColor=white)](https://www.typescriptlang.org/)
[![EF Core](https://img.shields.io/badge/EF_Core-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://learn.microsoft.com/ef/core/)
[![SQLite](https://img.shields.io/badge/SQLite-003B57?style=flat-square&logo=sqlite&logoColor=white)](https://www.sqlite.org/)
[![Groq](https://img.shields.io/badge/Groq_LLM-F55036?style=flat-square&logo=groq&logoColor=white)](https://groq.com/)
[![Docker](https://img.shields.io/badge/Docker-2496ED?style=flat-square&logo=docker&logoColor=white)](https://www.docker.com/)
[![GitHub Pages](https://img.shields.io/badge/GitHub_Pages-222222?style=flat-square&logo=githubpages&logoColor=white)](https://pages.github.com/)
[![Render](https://img.shields.io/badge/Render-46E3B7?style=flat-square&logo=render&logoColor=white)](https://render.com/)

</div>

---

## What it does

**RentAdvisor** is a full-stack rental-market and lease-safety tool built for the Australian rental market:

- 🏘️ **Suburb comparator** — compare a listing's rent against real ABS median rent stats for the suburb
- 🏢 **Landlord/agent lookup** — verify an ABN against the Australian Business Register (ABR)
- 📄 **Lease clause analyzer** — upload a lease PDF/text, and an LLM (or keyword-heuristic fallback) flags clauses worth a second look, with a risk score per clause
- ⚡ **Cold-start aware UX** — a wake-loader pings the free-tier backend on load so the first request doesn't feel broken

> **[👉 Try the live demo](https://ashikvarma11.github.io/rental-advisor/)** — frontend on GitHub Pages, backend on Render (free tier — first load may take ~30s to wake up).

## Tech stack

| Layer | Tech |
|---|---|
| Backend | ASP.NET Core (.NET 9), C#, Entity Framework Core, SQLite |
| Frontend | Angular (standalone components), TypeScript, SASS |
| AI | [Groq](https://groq.com/) LLM API for lease clause risk scoring (falls back to a keyword heuristic if no key is set) |
| Data | ABS median rent CSVs, Australian Business Register (ABR) API |
| Infra | Docker Compose (local), GitHub Actions → GitHub Pages (frontend) + Render (backend) |

## Project structure

```
backend/                    ASP.NET Core API, EF Core models/migrations, CSV importers, Groq clause analyzer
rental-advisor-frontend/    Angular app — home, suburb comparator, lease upload, clause viewer
data/                       Sample CSVs and lease text for import/testing
docker-compose.yml          Runs backend + frontend together
```

## Running locally

### Backend
```powershell
cd backend
dotnet restore
dotnet run
```
Listens on `http://localhost:5050`. See [backend/README.md](backend/README.md) for the Groq API key and seed endpoints.

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
Set `GROQ_API_KEY` in your environment or an `.env` file before running.

## Deployment

Pushes to `master` trigger [`.github/workflows/deploy.yml`](.github/workflows/deploy.yml):
- Frontend builds and deploys to **GitHub Pages**
- Backend deploy is triggered on **Render** via a deploy hook

---

<div align="center">

Made by [ashikvarma11](https://github.com/ashikvarma11)

</div>
