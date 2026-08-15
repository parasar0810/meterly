# Phase 14: Docker Containerization

This phase focuses on containerizing the entire application stack: the .NET 8 Web API, the Blazor WebAssembly frontend, and the PostgreSQL database, orchestrated seamlessly via Docker Compose.

## User Review Required

> [!IMPORTANT]
> Please review the proposed Docker setup. Once approved, I will implement the Dockerfiles and update the docker-compose configuration.

## Open Questions

- **Nginx routing:** For the frontend, I am planning to serve the Blazor WebAssembly files using a lightweight Nginx alpine image. Is this acceptable?
- **Ports:** I plan to map the API to port `8080` and the Client to port `80` (or `3000`) on the host. Do you have specific port preferences for the docker-compose setup?

## Proposed Changes

### Meterly.Api

#### [NEW] src/Meterly.Api/Dockerfile
- Multi-stage build for the .NET 8 API.
- **Build Stage:** Use `mcr.microsoft.com/dotnet/sdk:8.0`. Restore packages, compile, and publish.
- **Runtime Stage:** Use `mcr.microsoft.com/dotnet/aspnet:8.0`. Copy published output and expose port 8080.
- Ensure the API reads the connection string from environment variables in Docker.

#### [NEW] src/Meterly.Api/.dockerignore
- Exclude `bin/`, `obj/`, `.git/`, etc. to optimize the build context.

### Meterly.Client

#### [NEW] src/Meterly.Client/Dockerfile
- Multi-stage build for the Blazor WebAssembly client.
- **Build Stage:** Use `mcr.microsoft.com/dotnet/sdk:8.0`. Publish the Blazor project.
- **Runtime Stage:** Use `nginx:alpine`. Copy the published `wwwroot` files to Nginx's HTML directory.
- Configure Nginx to serve the app and correctly handle SPA routing (fallback to `index.html`).

#### [NEW] src/Meterly.Client/nginx.conf
- Custom Nginx configuration to ensure proper MIME types for Blazor WebAssembly (`.wasm`, `.dll`) and route all requests to `index.html` for client-side routing.

#### [NEW] src/Meterly.Client/.dockerignore
- Exclude `bin/`, `obj/`, etc.

### Infrastructure

#### [MODIFY] [docker-compose.yml](file:///c:/Users/KIIT0001/Desktop/meterly/docker-compose.yml)
- Currently, only `db` (PostgreSQL) is defined.
- Add `api` service:
  - Build from `src/Meterly.Api/Dockerfile`.
  - Pass the PostgreSQL connection string via environment variables (`ConnectionStrings__DefaultConnection`).
  - Pass JWT Secret.
  - Depends on `db`.
- Add `client` service:
  - Build from `src/Meterly.Client/Dockerfile`.
  - Expose ports to access the frontend.

## Verification Plan

### Manual Verification
- Run `docker-compose up --build -d` to build and start the entire stack.
- Check API health and Swagger at `http://localhost:8080/swagger`.
- Check the Frontend dashboard at `http://localhost:80` (or configured port).
- Verify the frontend successfully makes API calls to the backend via Docker routing.
