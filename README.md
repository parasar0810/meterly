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
- [x] **Phase 12:** Dunning + Hangfire
- [x] **Phase 13:** Testing
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

**Q: How do you secure webhooks so attackers can't spoof them?**
A: We use HMAC-SHA256. When a webhook is registered, we generate a cryptographically secure random secret. When dispatching, we compute an HMAC hash of the JSON payload using that secret and send it in the `Meterly-Signature` header. The receiver computes the same hash; if they don't match, the request was spoofed or tampered with in transit.

**Q: Why use double-entry accounting instead of just an 'AmountPaid' column?**
A: Double-entry accounting ensures mathematical correctness. Every transaction has two sides (Debit and Credit) that must sum to zero. If they don't, you have a bug. It provides a reliable, immutable audit trail for complex scenarios like partial refunds, chargebacks, and revenue recognition.

**Q: How do you handle a payment failure without losing the customer?**
A: Dunning — a scheduled retry system. When a payment fails, we record the failure, mark the subscription PastDue, and retry on a schedule (1 day, 3 days, 7 days). Each attempt is recorded in `DunningAttempts` for a full audit trail. If all retries fail, the subscription is Suspended, not deleted — the customer can still resolve it by updating their card.

**Q: Why Hangfire instead of a hosted IHostedService?**
A: Hangfire persists job state to PostgreSQL. If the server crashes mid-dunning sweep, the job is not lost — Hangfire marks it as failed and will retry it. A `BackgroundService` running a timer has no persistence; a crash means jobs are lost silently.

**Q: What's the Hangfire dashboard useful for in production?**
A: It shows all queued, processing, succeeded, and failed jobs in real-time. For billing, this is critical — if a dunning job fails (DB outage, gateway timeout), you can see exactly which invoice was affected, retry it manually, and see the full exception stack trace without tailing logs.

**Q: Why does DunningJob use IServiceScopeFactory instead of injecting DbContext directly?**
A: Hangfire creates jobs as singletons, but DbContext is Scoped (per request). Injecting Scoped into Singleton causes a captive dependency bug. IServiceScopeFactory lets us create a fresh scope per job execution, giving us a clean DbContext instance.

**Q: Why bypass the global tenant query filter in DunningJob?**
A: The global filter reads TenantId from ICurrentUserService, which reads from HttpContext. Hangfire jobs run outside HTTP requests — there's no HttpContext. We use `.IgnoreQueryFilters()` and handle tenant scoping manually by querying across all tenants in a single sweep.

**Q: Why don't you use a real PostgreSQL database in the integration tests?**
A: Requiring a real database makes tests flaky and hard to run in CI/CD environments. By replacing PostgreSQL with EF Core InMemory Database inside `WebApplicationFactory`, the tests run instantly entirely in memory, but still test the full ASP.NET Core request pipeline, middleware, MediatR, and controllers. We reserve real DB tests for end-to-end smoke tests.

**Q: In your PayInvoice tests, why do you mock IPaymentGateway but not the database?**
A: `IPaymentGateway` represents an external system boundary (e.g., Stripe) which we don't control. Calling it would cost real money and fail without network. The database, however, is controlled by our code. Using EF Core InMemory lets us verify that our EF queries and state changes work correctly without mocking every `DbSet`, which is an anti-pattern.

**Q: How do you test the dunning schedule without a database or background jobs running?**
A: We extracted the schedule logic into a pure domain function (`ShouldRetryNow`). Pure unit tests can pass in different mock `DateTime` values and attempt counts to verify the business rule (e.g., attempt 4 must wait 7 days) without needing Hangfire or SQL at all.

</details>

## License

Educational project — built for learning.
