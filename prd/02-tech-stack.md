# PRD — Tech Stack & Rationale

Frontend
- Angular 21: modern, component-driven UI suitable for dashboards and complex forms.  
- Serve built files via nginx (or static file host) in Docker for simple deployment.

Backend
- ASP.NET Core (.NET 9): chosen by decision for the project; stable and supports EF Core and modern DI patterns.
- EF Core for data access; SQLite provider for demo (easy migration to Postgres later).

AI / ML
- LLMs: integrate via backend-only connectors (OpenAI/Azure/Anthropic compatible).  
- Clause extraction: initial rule-based + LLM augmentation (configurable to call a real LLM in production).
- Embeddings/Vector DB: optional for similarity search (Pinecone/Milvus/Weaviate) — out of scope for MVP but noted as future enhancement.

Data & Integrations
- ABS / data.gov.au CSVs for median rent / suburb stats (curated and stored in repo/data for reproducibility).  
- ABR ABN lookup for live enrichment (backend proxy endpoint and API key in env only).

Dev / Ops
- Docker & Docker Compose for local/dev orchestration and simple single-host deployment.
- GitHub Actions for CI/CD (build, test, push images).  
- Secrets: dotnet user-secrets for local dev; GitHub Actions / Azure Key Vault for production secrets.

Why these choices
- Fast to implement in 4 weeks while demonstrating production-like architecture and deployment practices.  
- Components map directly to portfolio goals: interactive Angular UI, solid .NET backend, and AI/ML extensibility.
