# Meterly Architecture Diagram

Here is the complete architectural diagram for the Meterly SaaS platform. 

> [!TIP]
> **How to use this with Excalidraw:**
> 1. Copy the code block below (everything between the ` ```mermaid ` and ` ``` ` tags).
> 2. Open [Excalidraw](https://excalidraw.com/).
> 3. Click the **More Tools** menu (the icon with a `+` or grid) or press `Shift + 1`.
> 4. Select **Mermaid to Excalidraw**.
> 5. Paste the code into the text area and click **Insert**.

```mermaid
flowchart TB
    %% Styling
    classDef client fill:#1565C0,stroke:#0D47A1,stroke-width:2px,color:#fff,rx:5px,ry:5px
    classDef api fill:#2E7D32,stroke:#1B5E20,stroke-width:2px,color:#fff,rx:5px,ry:5px
    classDef app fill:#E65100,stroke:#BF360C,stroke-width:2px,color:#fff,rx:5px,ry:5px
    classDef domain fill:#6A1B9A,stroke:#4A148C,stroke-width:2px,color:#fff,rx:5px,ry:5px
    classDef infra fill:#455A64,stroke:#263238,stroke-width:2px,color:#fff,rx:5px,ry:5px
    classDef db fill:#00838F,stroke:#006064,stroke-width:2px,color:#fff,rx:50px,ry:20px
    classDef external fill:#4E342E,stroke:#3E2723,stroke-width:2px,color:#fff,stroke-dasharray: 5 5,rx:5px,ry:5px

    %% Users
    User((Tenant / Admin)) --> Browser

    %% Frontend Subgraph
    subgraph Frontend [Blazor WebAssembly Frontend]
        Browser["Browser (WASM Runtime)"]
        
        subgraph Meterly_Client [Meterly.Client]
            UI["MudBlazor UI Components"]
            Pages["Pages (Dashboard, Invoices, Customers, Auth)"]
            AuthProv["Custom Auth State Provider (JWT)"]
            Http["API Client (HttpClient)"]
            
            UI --> Pages
            Pages --> AuthProv
            Pages --> Http
            AuthProv --> LocalStorage[(Local Storage)]
        end
        Browser -.-> Meterly_Client
    end

    %% API Layer Subgraph
    subgraph BackendAPI [Presentation Layer]
        subgraph Meterly_Api [Meterly.Api]
            Controllers["Controllers (Auth, Invoices, Customers, Products)"]
            Middleware["Middleware (Exception Handling, CORS)"]
            Security["JWT Bearer Authentication"]
            
            Middleware --> Security
            Security --> Controllers
        end
    end

    %% Application Layer Subgraph
    subgraph ApplicationCore [Core Business Logic]
        subgraph Meterly_Application [Meterly.Application]
            CQRS["MediatR (Commands & Queries)"]
            Validation["FluentValidation (Request Validation)"]
            Ports["Interfaces (IMeterlyDbContext, ICurrentUserService)"]
            
            Validation --> CQRS
            CQRS --> Ports
        end
        
        subgraph Meterly_Domain [Meterly.Domain]
            Entities["Entities (Tenant, User, Invoice, Product, Subscription, Ledger)"]
            Enums["Enums & Value Objects"]
            Exceptions["Domain Exceptions"]
            
            Entities -.-> Enums
        end
        
        Meterly_Application --> Meterly_Domain
    end

    %% Infrastructure Layer Subgraph
    subgraph InfrastructureLayer [Infrastructure & Integrations]
        subgraph Meterly_Infrastructure [Meterly.Infrastructure]
            EFCore["EF Core (PostgreSQL Provider)"]
            Services["Services (TokenGenerator, LedgerService, CurrentUserService)"]
            Jobs["Hangfire Jobs (DunningJob, etc.)"]
            Adapters["External Adapters (Webhooks, Payments)"]
            
            Services --> EFCore
            Jobs --> EFCore
            Adapters --> EFCore
        end
    end

    %% External Systems Subgraph
    subgraph ExternalSystems [External Services]
        Database[(PostgreSQL Database)]
        PaymentGateway{{"Mock Payment Gateway"}}
        Webhooks[("External Client Webhooks")]
    end

    %% Connections across layers
    Http -- "REST / JSON" --> Middleware
    
    Controllers -- "Dispatch Commands/Queries" --> Validation
    
    Ports -. "Implemented by" .-> EFCore
    Ports -. "Implemented by" .-> Services
    Ports -. "Implemented by" .-> Adapters
    
    EFCore -- "Read/Write" --> Database
    Jobs -- "State / Scheduling" --> Database
    
    Adapters -- "Process Payments" --> PaymentGateway
    Adapters -- "Dispatch Events" --> Webhooks

    %% Apply Classes
    class Meterly_Client client
    class Meterly_Api api
    class Meterly_Application app
    class Meterly_Domain domain
    class Meterly_Infrastructure infra
    class Database db
    class PaymentGateway external
    class Webhooks external
```
