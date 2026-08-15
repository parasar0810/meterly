# Meterly — System Architecture

This document describes the complete architecture of the Meterly SaaS billing platform.

## Technology Overview

| Layer | Technology |
|---|---|
| **Frontend** | Blazor WebAssembly (C#), MudBlazor UI |
| **Backend API** | ASP.NET Core 8, Clean Architecture |
| **Database** | PostgreSQL (via Entity Framework Core) |
| **Auth** | JWT Bearer tokens |
| **Background Jobs** | Hangfire |
| **Containerisation** | Docker + docker-compose |

---

## Architecture Diagram

```mermaid
flowchart TB
    %% Styling
    classDef client fill:#1565C0,stroke:#0D47A1,stroke-width:2px,color:#fff
    classDef api fill:#2E7D32,stroke:#1B5E20,stroke-width:2px,color:#fff
    classDef app fill:#E65100,stroke:#BF360C,stroke-width:2px,color:#fff
    classDef domain fill:#6A1B9A,stroke:#4A148C,stroke-width:2px,color:#fff
    classDef infra fill:#455A64,stroke:#263238,stroke-width:2px,color:#fff
    classDef db fill:#00838F,stroke:#006064,stroke-width:2px,color:#fff
    classDef external fill:#4E342E,stroke:#3E2723,stroke-width:2px,color:#fff,stroke-dasharray:5 5

    %% User entry point
    User((Tenant / Admin)) --> Browser

    %% ── FRONTEND ────────────────────────────────────────────────────────
    subgraph Frontend ["🖥️  Blazor WebAssembly Frontend  (port 5278)"]
        Browser["Browser (WASM Runtime)"]

        subgraph Meterly_Client ["Meterly.Client"]
            UI["MudBlazor UI Components"]
            Pages["Pages\n(Dashboard · Invoices · Customers · Products · Auth)"]
            AuthProv["Custom AuthStateProvider\n(JWT Claims → Blazor Identity)"]
            Http["HttpClient\n(Bearer token injected per request)"]

            UI --> Pages
            Pages --> AuthProv
            Pages --> Http
            AuthProv --> LocalStorage[(localStorage\nJWT Token)]
        end

        Browser -.-> Meterly_Client
    end

    %% ── PRESENTATION / API ──────────────────────────────────────────────
    subgraph BackendAPI ["🌐  Presentation Layer"]
        subgraph Meterly_Api ["Meterly.Api  (port 8080)"]
            Middleware["ExceptionHandlingMiddleware\nCORS Policy"]
            Security["JWT Bearer Authentication"]
            Controllers["Controllers\n(AuthController · InvoicesController\n CustomersController · ProductsController\n HealthController)"]
            Swagger["Swagger / OpenAPI\n(/swagger)"]
            Hangfire_UI["Hangfire Dashboard\n(/hangfire)"]

            Middleware --> Security
            Security --> Controllers
        end
    end

    %% ── APPLICATION / USE-CASES ─────────────────────────────────────────
    subgraph ApplicationCore ["⚙️  Core Business Logic"]
        subgraph Meterly_Application ["Meterly.Application"]
            Validation["FluentValidation\n(Request Validators)"]
            CQRS["MediatR\n(Commands & Queries)"]
            Ports["Interfaces\n(IMeterlyDbContext\n ICurrentUserService\n ITokenGenerator\n IPaymentGateway\n IWebhookDispatcher)"]

            Validation --> CQRS
            CQRS --> Ports
        end

        subgraph Meterly_Domain ["Meterly.Domain"]
            Entities["Entities\n(Tenant · User · Invoice · InvoiceLineItem\n Product · Subscription · Payment\n DeliveryAttempt · LedgerEntry)"]
            Enums["Enums & Value Objects\n(InvoiceStatus · BillingCycle\n Currency · PaymentStatus)"]
            Exceptions["Domain Exceptions\n(NotFoundException · ConflictException\n ValidationException)"]

            Entities -.-> Enums
        end

        Meterly_Application --> Meterly_Domain
    end

    %% ── INFRASTRUCTURE ──────────────────────────────────────────────────
    subgraph InfrastructureLayer ["🔧  Infrastructure & Integrations"]
        subgraph Meterly_Infrastructure ["Meterly.Infrastructure"]
            EFCore["EF Core DbContext\n(MeterlyDbContext + PostgreSQL)"]
            Migrations["EF Core Migrations\n(Schema versioning)"]
            JwtGen["JwtTokenGenerator\n(HS256 signed tokens)"]
            Hasher["BcryptPasswordHasher"]
            LedgerSvc["LedgerService\n(Double-entry accounting)"]
            UserSvc["CurrentUserService\n(Reads JWT claims from HttpContext)"]
            PayGW["MockPaymentGateway\n(Simulated charge / refund)"]
            WebhookDisp["HttpWebhookDispatcher\n(POST to tenant callback URL)"]
            DunningJob["DunningJob (Hangfire)\n(Hourly sweep — retries overdue invoices)"]

            JwtGen --> EFCore
            LedgerSvc --> EFCore
            DunningJob --> EFCore
            PayGW --> EFCore
            WebhookDisp --> EFCore
            EFCore --> Migrations
        end
    end

    %% ── EXTERNAL SYSTEMS ────────────────────────────────────────────────
    subgraph ExternalSystems ["☁️  External / Infrastructure Services"]
        Database[("PostgreSQL\nDatabase")]
        PaymentGateway{{"Mock Payment\nGateway"}}
        Webhooks[("Tenant Webhook\nEndpoints")]
        Docker["Docker / docker-compose\n(API + Client + DB containers)"]
    end

    %% ── CROSS-LAYER CONNECTIONS ─────────────────────────────────────────
    Http           -- "REST / JSON\nHTTP requests"      --> Middleware
    Controllers    -- "IMediator.Send()"                --> Validation

    Ports          -. "Implemented by" .->              EFCore
    Ports          -. "Implemented by" .->              JwtGen
    Ports          -. "Implemented by" .->              UserSvc
    Ports          -. "Implemented by" .->              PayGW
    Ports          -. "Implemented by" .->              WebhookDisp
    Ports          -. "Implemented by" .->              Hasher
    Ports          -. "Implemented by" .->              LedgerSvc

    EFCore         -- "Read / Write"                    --> Database
    DunningJob     -- "Persist job state"               --> Database
    PayGW          -- "Process charge"                  --> PaymentGateway
    WebhookDisp    -- "HTTP POST event"                 --> Webhooks
    Docker         -. "Orchestrates"  .->               Database

    %% Apply styles
    class Meterly_Client client
    class Meterly_Api api
    class Meterly_Application app
    class Meterly_Domain domain
    class Meterly_Infrastructure infra
    class Database db
    class PaymentGateway external
    class Webhooks external
```

---

## Layer Responsibilities

### `Meterly.Client` — Blazor WebAssembly (PWA)
- Single-page application compiled to WebAssembly and served as static files
- Uses **MudBlazor** component library for the UI
- Custom `AuthStateProvider` decodes the JWT and exposes user identity to Blazor's auth system
- All API calls attach the JWT as a `Bearer` token; no server-side sessions
- Configured as a **Progressive Web App (PWA)** — installable on Windows/Mac/mobile

### `Meterly.Api` — ASP.NET Core 8
- Thin HTTP layer: validates JWT, maps routes, dispatches MediatR commands/queries
- Global exception handler returns consistent `ProblemDetails` JSON
- CORS is explicitly configured per allowed origin
- Swagger UI enabled for all environments

### `Meterly.Application` — Use Cases (CQRS)
- Each feature is a `Command` (mutates state) or `Query` (reads state), handled by a `Handler`
- FluentValidation pipelines run before every handler
- All external dependencies are hidden behind interfaces (ports)

### `Meterly.Domain` — Business Rules
- Pure C# with zero infrastructure dependencies
- Entity classes enforce invariants (e.g., you cannot pay an already-paid invoice)

### `Meterly.Infrastructure` — Adapters
- Implements all interfaces defined in Application
- EF Core with the Npgsql PostgreSQL provider
- Hangfire uses the same PostgreSQL database for its job queue
