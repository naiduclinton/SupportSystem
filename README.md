# SupportTicketing API

ASP.NET Core 8 REST API for the support ticketing system.  
**Stack:** C# · Dapper · PostgreSQL · RabbitMQ · JWT Auth

---

## Project structure

```
SupportTicketing.sln
├── SupportTicketing.Core/           # Domain — entities, interfaces, services (no infra deps)
│   ├── Entities/                    # Ticket, User, Customer, Comment, ...
│   ├── Enums/                       # TicketStatus, Priority, Role, ...
│   ├── Interfaces/                  # IRepository<T>, ITicketService, ISlaService, ...
│   ├── Models/                      # DTOs, request/response, event messages
│   └── Services/                    # TicketService, SlaService, AutomationService
│
├── SupportTicketing.Infrastructure/ # Dapper repos, RabbitMQ bus, AuthService
│   ├── Repositories/
│   ├── Messaging/
│   └── Services/
│
└── SupportTicketing.Api/            # ASP.NET controllers, middleware, DI wiring
    ├── Controllers/
    ├── Program.cs
    ├── appsettings.json
    └── Dockerfile
```

---

## Quick start

### 1. Start infrastructure

```bash
docker-compose up postgres rabbitmq -d
```

The schema from `support_ticketing_schema.sql` is auto-applied on first run.

### 2. Configure secrets

Edit `SupportTicketing.Api/appsettings.json` or use dotnet user-secrets:

```bash
cd SupportTicketing.Api
dotnet user-secrets set "Jwt:Secret" "your-strong-secret-min-32-chars"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;..."
```

### 3. Run the API

```bash
dotnet run --project SupportTicketing.Api
```

Swagger UI: http://localhost:5000/swagger

---

## API endpoints

| Method | Route                              | Auth    | Description                    |
|--------|------------------------------------|---------|--------------------------------|
| POST   | /api/auth/login                    | Public  | Login → JWT tokens             |
| POST   | /api/auth/refresh                  | Public  | Refresh access token           |
| GET    | /api/tickets                       | Any     | Search/filter tickets (paged)  |
| GET    | /api/tickets/{id}                  | Any     | Ticket detail + comments       |
| POST   | /api/tickets                       | Any     | Create ticket                  |
| PATCH  | /api/tickets/{id}/status           | Any     | Update status                  |
| PATCH  | /api/tickets/{id}/assign           | Agent   | Assign to agent/team           |
| POST   | /api/tickets/{id}/comments         | Any     | Add reply or internal note     |
| DELETE | /api/tickets/{id}                  | Admin   | Soft delete                    |
| GET    | /api/users                         | Agent+  | List agents                    |
| GET    | /api/users/workloads               | Admin   | Agent workload stats           |
| GET    | /api/reports/dashboard             | Agent+  | Dashboard summary stats        |
| GET    | /api/reports/sla?from=&to=         | Agent+  | SLA compliance report          |
| GET    | /api/reports/agents                | Agent+  | Per-agent performance          |
| GET    | /api/notifications                 | Any     | Unread notifications           |
| POST   | /api/notifications/mark-all-read   | Any     | Mark all notifications read    |

---

## RabbitMQ events

Exchange: `tickets` (topic)

| Routing key              | Payload                    | Consumer queues        |
|--------------------------|----------------------------|------------------------|
| `ticket.created`         | TicketCreatedEvent         | ticket.notifications   |
| `ticket.status_changed`  | TicketStatusChangedEvent   | ticket.notifications   |
| `ticket.sla_breached`    | SlaBreachEvent             | ticket.sla             |
| `ticket.assigned`        | TicketAssignedEvent        | ticket.notifications   |

RabbitMQ Management UI (dev): http://localhost:15672 (guest/guest)

---

## Design decisions

- **SOLID principles** — each service/repository has a single responsibility; dependencies injected via interfaces
- **Dapper over EF Core** — explicit SQL gives full control over query performance and PostgreSQL-specific features (ENUM casting, JSONB, views)
- **Soft deletes** — `deleted_at` on all critical tables; data is never permanently destroyed
- **RabbitMQ** — async event bus decouples notifications, SLA checks, and CSAT from the request path
- **SLA background job** — `BackgroundService` evaluates breaches every minute; fires events on breach
- **Business hours SLA** — pure C# calculation with configurable start/end times; no external scheduling deps
- **JWT + bcrypt** — stateless auth with short-lived access tokens; refresh tokens tracked server-side
- **Automation engine** — JSONB-stored conditions/actions evaluated at runtime; add rules without deploys
