# PRD — Decision Log (chronological)

This file records decisions taken during planning and their rationale for traceability.

1) Project selection
- Decision: Build "Rental Market Analyzer & Lease Advisor" focused on Australian markets.
- Rationale: Practical, high-impact, demonstrates data integration, AI, and full-stack skills.

2) Target audience & regional focus
- Decision: Specifically target Australian users; integrate at least one Australian dataset or regulation.
- Rationale: User plans to move to Australia; portfolio demonstrates local relevance.

3) Tech stack
- Decision: Frontend Angular 21; Backend .NET 9 (ASP.NET Core); EF Core + SQLite for demo.
- Rationale: Matches user expertise and shows modern full-stack capabilities; .NET 9 chosen per user.

4) Data sources
- Decision: Use curated ABS/data.gov.au CSVs for median rents + one live ABR ABN lookup for enrichment.
- Rationale: Government datasets are open and reproducible; ABR adds live enrichment without large commercial contracts.

5) Secrets handling
- Decision: Do not store API keys in frontend. Backend-only configuration with env vars / secrets manager.
- Rationale: Security best practice; frontend secrets are unsafe.

6) Database
- Decision: SQLite for one-month demo.
- Rationale: Zero-config, fast to implement, and easy to seed and include in Docker volumes.

7) Hosting
- Decision: Docker Compose on a single VM for demo; document migration path to managed services or PostgreSQL.
- Rationale: Quick to deploy, reproducible, and aligns with 1-month timeline.

8) Implementation plan
- Decision: 4-week weekly plan (foundation -> ingestion -> analysis -> polish + demo).
- Rationale: Keeps scope realistic while delivering a polished, deployable demo.

9) Next actions (as of this document)
- Scaffold monorepo skeleton and implement backend endpoints with env-only secrets and ABR proxy.

10) Planned enhancements added (2026-07-01)
- Decision: Add three post-4-week enhancements to the plan (not yet scheduled/implemented): (a) chart-based risk dashboard replacing the stacked clause list, with click-through to filtered clauses; (b) Azure hosting restricted strictly to always-free tier resources with Groq API reachability preserved and zero cost as a hard constraint; (c) app-wide futuristic GSAP animations (scroll-triggered, transitions, micro-interactions) beyond the current hero/list entrance effects.
- Rationale: User wants these captured as committed future scope in the PRD before implementation begins.
