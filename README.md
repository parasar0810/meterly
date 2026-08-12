# Meterly — Multi-Tenant SaaS Billing & Subscription Platform

A realistic multi-tenant billing platform built with ASP.NET Core, Entity Framework Core, and PostgreSQL. Inspired by Stripe Billing and Chargebee.

## Tech Stack

| Technology | Purpose |
|---|---|
| C# / .NET 8 | Backend language and runtime |
| ASP.NET Core | Web API framework |
| Entity Framework Core | ORM (Object-Relational Mapper) |
| PostgreSQL | Database |
| MediatR | Command/query handling (CQRS-lite) |
| FluentValidation | Input validation |
| JWT | Authentication |
| Hangfire | Background job processing |
| Docker | Containerized development |
| xUnit | Testing |
| Swagger | API documentation |

## Architecture

```
src/
├── Meterly.Domain           → Entities, enums, business rules (zero dependencies)
├── Meterly.Application      → Services, DTOs, validators, use cases
├── Meterly.Infrastructure   → EF Core, PostgreSQL, external services
└── Meterly.Api              → Controllers, middleware, configuration

tests/
├── Meterly.UnitTests        → Business logic tests
└── Meterly.IntegrationTests → API endpoint tests
```

## Quick Start

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)

### 1. Start the Database

```bash
docker-compose up -d
```

This starts PostgreSQL on `localhost:5432` with:
- Database: `meterly_db`
- Username: `meterly_user`
- Password: `meterly_password`

### 2. Run the Application

```bash
dotnet run --project src/Meterly.Api
```

The API starts at:
- **HTTP:** http://localhost:5000
- **Swagger:** http://localhost:5000/swagger

### 3. Run Tests

```bash
dotnet test
```

### 4. Verify It Works

```bash
# Health check
curl http://localhost:5000/api/health

# Test error handling
curl http://localhost:5000/api/health/error-test?type=notfound
```

## Development Phases

- [x] **Phase 1:** Project setup + architecture
- [ ] **Phase 2:** PostgreSQL + EF Core + migrations
- [ ] **Phase 3:** Authentication + multi-tenancy
- [ ] **Phase 4:** Customers
- [ ] **Phase 5:** Plans + subscriptions
- [ ] **Phase 6:** Usage metering
- [ ] **Phase 7:** Invoice generation
- [ ] **Phase 8:** Mock payments
- [ ] **Phase 9:** Idempotency + webhooks
- [ ] **Phase 10:** Double-entry ledger
- [ ] **Phase 11:** Refunds + credits
- [ ] **Phase 12:** Dunning + Hangfire
- [ ] **Phase 13:** Testing
- [ ] **Phase 14:** Docker
- [ ] **Phase 15:** Documentation + cleanup

## Core Flow

```
Tenant → Customer → Subscription → Usage → Invoice → Payment → Ledger
```

## Interview Questions From This Project

<details>
<summary>Phase 1: Project Setup & Architecture</summary>

**Q: Why Clean Architecture?**
A: Separates business logic from infrastructure. I can test billing calculations without a database.

**Q: Why a modular monolith instead of microservices?**
A: Microservices add network complexity, distributed transactions, and deployment overhead. A monolith with clean boundaries is simpler to build, test, and deploy — and can be split later if needed.

**Q: Why PostgreSQL?**
A: Open-source, production-grade, excellent concurrency (MVCC), strong JSON support. Free to run anywhere.

**Q: What is Dependency Injection?**
A: Instead of a class creating its own dependencies (`new DatabaseService()`), they're passed in from outside. This makes code testable and swappable.

**Q: Why a global exception handler?**
A: Every API should return consistent error formats. Without one, unhandled exceptions leak stack traces — a security risk and poor UX.

**Q: Why DTOs instead of exposing entities?**
A: Entities contain database concerns. DTOs are shaped for the API consumer. Changing the database schema shouldn't break the API contract.

</details>

## License

Educational project — built for learning.
