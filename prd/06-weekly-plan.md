# PRD — 4-Week Implementation Plan

Goal: Deliver a deployable, demo-ready prototype within one month (4 weeks). SQLite and curated CSVs + ABR lookup.

Week 1 — Foundation
- Scaffold monorepo: `/frontend` (Angular 21) and `/backend` (ASP.NET Core .NET 9). Add Dockerfiles and `docker-compose.yml`.
- Define EF Core data model: Listing, Transaction, SuburbStats, LeaseDocument, Clause, User.
- Implement CSV importer and seed `SuburbStats` from curated ABS/data.gov.au CSVs in `/data`.
- Add ABR client configuration (env-only) and a stub endpoint `GET /api/enrich/abn/{abn}`.

Deliverables (W1)
- Repo skeleton, sample CSVs, basic backend with EF Core migrations, and README instructions to run locally.

Week 2 — Ingestion & Core APIs
- Implement listing ingestion endpoints (CSV upload + validation).  
- Implement rent comparator API to compute listing vs median by suburb/SA2.  
- Implement lease upload endpoint and a simple rule-based clause extractor; wire LLM stub (backend-only) for improved extraction.
- Implement ABR enrichment call (backend) and basic cache.

Deliverables (W2)
- Endpoints for upload/import, comparator, and ABR enrichment with test data.

Week 3 — Analysis & Advisor UX
- Build frontend views: upload flow, suburb comparison dashboard, lease clause highlighter.
- Implement advisor logic: flag risky clauses, provide pricing/yield suggestions, and ATO/GST hints (static rules + LLM-generated text).
- Improve LLM integration for clearer advice (kept as configurable provider via env).

Deliverables (W3)
- Interactive UI demonstrating full analysis flow with sample dataset.

Week 4 — Polish, tests & Demo
- Add unit and integration tests for core APIs and key UI flows.
- Package Docker Compose demo with seeded DB and sample listings.  
- Write `README.md` with setup, demo script, and screenshots; prepare an example demo PR/recording.

Acceptance criteria (final)
- E2E demo: upload listing CSV -> see suburb median comparison -> ABR-enriched landlord info -> highlighted lease clauses -> advisor recommendations.
- Repo contains `docker-compose.yml`, seed data, `README.md`, and basic tests that run in CI.

## Planned enhancements (post-4-week, not yet scheduled)

1. Risk dashboard with charts
   - Replace the stacked clause-card list with a dashboard view: charts/visualizations summarizing clause risk (e.g. risk distribution, category breakdown) using a proper charting library.
   - Clicking a chart segment/item filters or scrolls to the corresponding clause(s) below.

2. Free hosting on Azure
   - Deploy frontend + backend to Azure using only always-free tier resources (e.g. Azure Static Web Apps free tier for Angular, Azure App Service free (F1) tier or Azure Container Apps free grant for the .NET API).
   - Groq API must remain reachable from the hosted backend (outbound HTTPS allowed on chosen tier; no VNet/firewall blocking it).
   - Hard constraint: zero ongoing cost. Must stay within always-free tier limits — no pay-as-you-go resources, no paid SKUs, no services that bill after a trial credit expires.

3. Futuristic UI with GSAP animations throughout
   - Extend the existing GSAP usage app-wide: scroll-triggered animations (ScrollTrigger), page/section transitions, micro-interactions on hover/click, animated dashboard elements.
   - Applies across all pages (home, upload, clauses/dashboard, comparator), not just the current hero/list entrance animations.
