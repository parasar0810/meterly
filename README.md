# Meterly - Multi-Tenant SaaS Billing and Subscription Platform

> A production-grade, multi-tenant SaaS billing platform built with ASP.NET Core, Blazor WebAssembly, Entity Framework Core, and PostgreSQL. Inspired by Stripe Billing and Chargebee.

## Tech Stack

| Technology | Purpose |
|---|---|
| **C# / .NET 8** | Language and runtime |
| **ASP.NET Core 8** | Web API backend |
| **Blazor WebAssembly** | SPA frontend (compiled C# to WASM) |
| **MudBlazor** | UI component library |
| **Entity Framework Core** | ORM with PostgreSQL |
| **PostgreSQL** | Primary database |
| **MediatR** | CQRS command/query handling |
| **FluentValidation** | Input validation pipelines |
| **JWT Bearer** | Authentication and multi-tenancy |
| **Hangfire** | Persistent background jobs (dunning) |
| **Docker** | Containerised development |
| **xUnit** | Unit and integration testing |
| **Swagger / OpenAPI** | API documentation |
| **PWA** | Installable as a Windows/Mac desktop app |

---

## Architecture

```mermaid
flowchart TB
    classDef client fill:#1565C0,stroke:#0D47A1,stroke-width:2px,color:#fff
    classDef api fill:#2E7D32,stroke:#1B5E20,stroke-width:2px,color:#fff
    classDef app fill:#E65100,stroke:#BF360C,stroke-width:2px,color:#fff
    classDef domain fill:#6A1B9A,stroke:#4A148C,stroke-width:2px,color:#fff
    classDef infra fill:#455A64,stroke:#263238,stroke-width:2px,color:#fff
    classDef db fill:#00838F,stroke:#006064,stroke-width:2px,color:#fff
    classDef external fill:#4E342E,stroke:#3E2723,stroke-width:2px,color:#fff,stroke-dasharray:5 5

    User((Tenant / Admin)) --> Browser

    subgraph Frontend [Blazor WebAssembly Frontend - port 5278]
        Browser[Browser WASM Runtime]
        subgraph Meterly_Client [Meterly.Client]
            UI[MudBlazor UI Components]
            Pages[Pages - Dashboard, Invoices, Customers, Products, Auth]
            AuthProv[Custom AuthStateProvider]
            Http[HttpClient - Bearer token per request]
            UI --> Pages
            Pages --> AuthProv
            Pages --> Http
            AuthProv --> LocalStorage[(localStorage JWT)]
        end
        Browser -.-> Meterly_Client
    end

    subgraph BackendAPI [Presentation Layer]
        subgraph Meterly_Api [Meterly.Api - port 8080]
            Middleware[ExceptionHandlingMiddleware and CORS]
            Security[JWT Bearer Authentication]
            Controllers[Controllers - Auth, Invoices, Customers, Products, Health]
            Middleware --> Security
            Security --> Controllers
        end
    end

    subgraph ApplicationCore [Core Business Logic]
        subgraph Meterly_Application [Meterly.Application]
            Validation[FluentValidation Pipelines]
            CQRS[MediatR Commands and Queries]
            Ports[Interfaces - IMeterlyDbContext, ITokenGenerator, IPaymentGateway, IWebhookDispatcher]
            Validation --> CQRS
            CQRS --> Ports
        end
        subgraph Meterly_Domain [Meterly.Domain]
            Entities[Entities - Tenant, User, Invoice, Product, Subscription, Payment, LedgerEntry]
            Enums[Enums - InvoiceStatus, BillingCycle, Currency]
            Exceptions[Domain Exceptions]
            Entities -.-> Enums
        end
        Meterly_Application --> Meterly_Domain
    end

    subgraph InfraLayer [Infrastructure and Integrations]
        subgraph Meterly_Infrastructure [Meterly.Infrastructure]
            EFCore[EF Core DbContext - PostgreSQL]
            JwtGen[JwtTokenGenerator]
            LedgerSvc[LedgerService - Double-entry accounting]
            PayGW[MockPaymentGateway]
            WebhookDisp[HttpWebhookDispatcher]
            DunningJob[DunningJob via Hangfire - hourly sweep]
        end
    end

    subgraph ExternalSystems [External Services]
        Database[(PostgreSQL Database)]
        PaymentGateway{{Mock Payment Gateway}}
        Webhooks[(Tenant Webhook Endpoints)]
    end

    Http        -- REST/JSON         --> Middleware
    Controllers -- IMediator.Send()  --> Validation
    Ports       -. Implemented by .-> EFCore
    Ports       -. Implemented by .-> JwtGen
    Ports       -. Implemented by .-> PayGW
    Ports       -. Implemented by .-> WebhookDisp
    Ports       -. Implemented by .-> LedgerSvc
    EFCore      -- Read/Write        --> Database
    DunningJob  -- Job state         --> Database
    PayGW       -- Process charge    --> PaymentGateway
    WebhookDisp -- HTTP POST event   --> Webhooks

    class Meterly_Client client
    class Meterly_Api api
    class Meterly_Application app
    class Meterly_Domain domain
    class Meterly_Infrastructure infra
    class Database db
    class PaymentGateway external
    class Webhooks external
```

> See [ARCHITECTURE.md](./ARCHITECTURE.md) for full diagram with detailed layer descriptions.

---

## Project Structure

`
src/
├── Meterly.Domain           → Entities, enums, domain exceptions (zero dependencies)
├── Meterly.Application      → MediatR commands/queries, validators, interfaces
├── Meterly.Infrastructure   → EF Core, PostgreSQL, JWT, Hangfire, adapters
├── Meterly.Api              → Controllers, middleware, Swagger, CORS
└── Meterly.Client           → Blazor WebAssembly SPA (MudBlazor UI + PWA)

tests/
├── Meterly.UnitTests        → Pure business logic tests
└── Meterly.IntegrationTests → Full HTTP pipeline tests (in-memory DB)
`

---

## Quick Start

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)

### 1. Start the Database

`ash
docker-compose up -d
`

Starts PostgreSQL on localhost:5432 with database meterly_db, user meterly_user, password meterly_password.

### 2. Run the Backend API

`ash
dotnet run --project src/Meterly.Api --urls "http://localhost:8080"
`

- API: http://localhost:8080
- Swagger UI: http://localhost:8080/swagger
- Hangfire Dashboard: http://localhost:8080/hangfire

### 3. Run the Frontend

`ash
dotnet run --project src/Meterly.Client
`

- App: http://localhost:5278

### 4. Run in Visual Studio

1. Open Meterly.sln
2. Right-click the Solution → Configure Startup Projects → Multiple startup projects
3. Set Meterly.Api to Start and Meterly.Client to Start
4. Press Ctrl+F5

### 5. Run Tests

`ash
dotnet test
`

---

## Core Billing Flow

`
Tenant registers → Creates Customers → Sets up Products/Plans
→ Creates Subscriptions → Generates Invoices → Processes Payments
→ Ledger entries written → Webhooks dispatched → Dunning on failure
`

---

## Development Phases

- [x] **Phase 1:** Project setup + Clean Architecture
- [x] **Phase 2:** PostgreSQL + EF Core + migrations
- [x] **Phase 3:** Authentication + multi-tenancy (JWT)
- [x] **Phase 4:** Customers
- [x] **Phase 5:** Plans + subscriptions
- [x] **Phase 6:** Usage metering
- [x] **Phase 7:** Invoice generation
- [x] **Phase 8:** Mock payments
- [x] **Phase 9:** Idempotency + webhooks
- [x] **Phase 10:** Double-entry ledger
- [x] **Phase 11:** Refunds + credits
- [x] **Phase 12:** Dunning + Hangfire
- [x] **Phase 13:** Testing (unit + integration)
- [x] **Phase 14:** Docker
- [x] **Phase 15:** Blazor WebAssembly frontend (PWA) + Documentation

---

## License

Educational project — built for learning.
