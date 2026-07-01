# PRD — Architecture & Hosting

Repository layout (recommended)
- `/frontend` — Angular 21 application (source + Dockerfile).
- `/backend` — ASP.NET Core (.NET 9) API (source + Dockerfile).
- `/data` — seed CSVs and demo datasets.
- `/prd` — this PRD folder.
- `docker-compose.yml` — local/dev orchestration for demo deployment.

Service responsibilities
- Frontend: UI, file upload, dashboard, charts — no secrets. Calls backend API only.
- Backend: business logic, data access (EF Core + SQLite), ABR proxy, LLM connectors, auth (optional), background processing.

Persistence: SQLite (demo)
- Store SQLite file in a Docker volume or host bind mount (e.g. `dbdata` volume).  
- EF Core connection string example for container: `Data Source=/app/Data/rental.db`.
- Note: SQLite is single-process friendly; ensure backend maintains proper connection lifetime and uses `Pooling=False` patterns where required.

Docker Compose (example)
```yaml
version: '3.8'
services:
  backend:
    build: ./backend
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ABR__ApiKey=${ABR_API_KEY}
    volumes:
      - dbdata:/app/Data
    ports:
      - "5000:80"

  frontend:
    build: ./frontend
    ports:
      - "80:80"
    depends_on:
      - backend

volumes:
  dbdata:
```

Hosting options
- Quick demo (recommended): single VM (Azure VM, DigitalOcean droplet) running `docker-compose up -d`. Set env vars on the host or provide a `.env` file outside git.
- Container registry + VM deploy: build images in CI, push to ACR/DockerHub, then pull on host and `docker-compose pull`.
- Managed options: Azure App Service / Container Apps or Azure Web App + Azure Files for SQLite — note Azure Files required for SQLite file persistence or switch to managed Postgres for production.

Operational notes
- Back up `dbdata` volume regularly for demo reproducibility.  
- For production readiness, migrate to PostgreSQL or managed DB; do not use SQLite for multi-instance deployments.
