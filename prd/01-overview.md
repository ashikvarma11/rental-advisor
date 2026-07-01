# PRD — Overview

Project: Rental Market Analyzer & Lease Advisor

Purpose
- Build a one-month, production-adjacent prototype that helps landlords, property managers and renters in Australia compare listings to official median rents, enrich listing parties via ABR, and analyse lease clauses with AI-assisted recommendations.

Primary goals
- Demonstrate full-stack skills: modern Angular frontend + ASP.NET Core backend (.NET 9) + EF Core + SQLite.
- Integrate authoritative Australian data sources (ABS / data.gov.au) and an ABR lookup for enrichment.
- Provide a clear, deployable demo (Docker Compose) showing CSV import -> analysis -> advisor output.

Scope (in)
- Angular 21 frontend for uploads, dashboard, and clause-highlighting UX.
- ASP.NET Core (.NET 9) backend with EF Core and SQLite for persistence.
- Seeded curated ABS/data.gov.au CSVs for median rent statistics.
- One live enrichment API call to ABR (ABN lookup) via backend-only API key.
- Basic LLM/AI stubs for clause extraction and advice (configurable to real LLM later).

Out of scope (for the 1-month prototype)
- Full production-grade scaling (no multi-node DB clustering).  
- Integration with commercial listing vendors (Domain, REA) unless later added.

Key decisions (summary)
- Project chosen: Rental Market Analyzer & Lease Advisor (Australia-focused).
- Frontend: Angular 21. Backend: .NET 9 (ASP.NET Core).
- Database: SQLite (single-node demo).  
- Data: curated ABS/data.gov.au CSVs + one live ABR lookup for enrichment.
- Secrets: API keys only on backend; no frontend secrets.
- Hosting: Docker Compose on single VM (Azure/DigitalOcean) with named volume for SQLite.

Acceptance criteria
- End-to-end demo where a user uploads CSV/listing or lease, sees suburb median comparison, receives ABR-enriched landlord info, and sees highlighted risky lease clauses with recommendations.
- Repo includes README with setup, seed data, Docker Compose, and minimal tests.

Stakeholders
- Primary: project owner (developer portfolio).  
- Secondary: Australian landlords, renters, property managers (demo audience).
