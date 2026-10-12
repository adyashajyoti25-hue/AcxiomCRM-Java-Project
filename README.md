# AcxiomCRM Enterprise — Production-Grade CRM Web Application

[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download)
[![Architecture](https://img.shields.io/badge/Architecture-Clean%20%2F%20Layered-emerald.svg)]()
[![Database](https://img.shields.io/badge/Database-EF%20Core%20(Sqlite%20%7C%20SqlServer)-orange.svg)]()
[![Security](https://img.shields.io/badge/Security-ASP.NET%20Core%20Identity-indigo.svg)]()
[![Tests](https://img.shields.io/badge/Tests-23%2F23%20Passing-brightgreen.svg)]()

AcxiomCRM is a complete, enterprise-level Customer Relationship Management (CRM) application built according to the **AcxiomCRM Complete Project Assignment** specification. It delivers robust database persistence, enterprise role-based authorization, end-to-end sales pipelines, atomic lead conversions, audit trails, operational reports with CSV export, interactive Chart.js dashboards, and a documented REST API with Swagger UI.

---

## 1. Architecture Overview

The solution follows a clean, decoupled 4-tier layered architecture:

```
AcxiomCRM/
│
├── src/
│   ├── AcxiomCRM.Domain/             # Pure domain entities, enums, business rule specifications
│   │   ├── Entities/                 # Customer, Lead, Opportunity, FollowUp, Activity, AuditLog, Identity
│   │   ├── Enums/                    # LeadStatus, OpportunityStage, FollowUpType, etc.
│   │   └── Constants/                # AppRoles, AuditActions
│   │
│   ├── AcxiomCRM.Infrastructure/     # Data access, persistence, EF Core configurations
│   │   ├── Data/                     # AppDbContext (Fluent API, indexes, cascades, IdentityDbContext)
│   │   └── Data/DbInitializer.cs     # Automated schema creation & role/user/demo data seeding
│   │
│   ├── AcxiomCRM.Application/        # Core business logic, services, DTOs, validations
│   │   ├── DTOs/                     # Strongly-typed input/output models with data annotations
│   │   ├── Interfaces/               # Service contracts (ICustomerService, ILeadService, etc.)
│   │   └── Services/                 # Scoped business implementations & audit dispatchers
│   │
│   └── AcxiomCRM.Web/                # ASP.NET Core MVC UI + REST API Controllers
│       ├── Controllers/              # MVC controllers (Customer, Lead, Opportunity, Dashboard, etc.)
│       ├── Controllers/Api/          # REST API controllers with Swagger OpenAPI annotations
│       ├── Views/                    # Reusable Razor layouts, components, modal dialogs, KPI cards
│       ├── Middleware/               # SecurityHeadersMiddleware, Centralized Exception Handling
│       ├── Services/                 # CurrentUserService (ClaimsPrincipal abstraction)
│       └── wwwroot/                  # Bootstrap 5, Inter font, Chart.js integrations, site.js
│
└── tests/
    └── AcxiomCRM.Tests/              # 23 automated unit, integration, and security tests
        ├── BusinessRuleTests.cs
        ├── CustomerServiceTests.cs
        ├── LeadServiceTests.cs
        ├── OpportunityAndFollowUpServiceTests.cs
        └── LeadConversionAndScopingTests.cs
```

---

## 2. Default Seeded Credentials

When launched in development mode, the database is automatically provisioned with the standard role hierarchy and sample users:

| Role | Email | Password | Access Scope |
| :--- | :--- | :--- | :--- |
| **Admin** | `admin@acxiomcrm.com` | `Admin@12345` | Unrestricted CRM access, User management, Role assignments, Audit logs, All reports |
| **Manager** | `manager@acxiomcrm.com` | `Manager@12345` | Team dashboard, All customers & leads, Pipeline reports, User directory view |
| **Sales Executive** | `executive@acxiomcrm.com` | `Executive@12345` | Assigned customers, leads, opportunities, follow-ups, personal dashboard |
| **Sales Executive** | `sarah@acxiomcrm.com` | `Sarah@12345` | Assigned customers, leads, opportunities, follow-ups, personal dashboard |

---

## 3. Key Modules & Functional Highlights

### Module A: Dashboard & Analytics
- **Role-Aware KPIs:** Total Customers, Total Leads, Open Leads, Total Pipeline Value, Weighted Forecast, Open/Won/Lost Opportunities, Upcoming & Overdue Follow-ups.
- **Interactive Visualizations:** Powered by Chart.js — Lead Status Breakdown (Doughnut), Opportunity Pipeline by Stage (Bar), Monthly Sales Performance (Line).
- **Date Filters:** Today, This Week, This Month, All Time with reactive backend aggregation.

### Module B: Customer Management
- Full CRUD workflows with server-side pagination, search, and status filtering.
- Client-side & server-side duplicate prevention for Email and Phone numbers.
- Detailed customer 360° profile displaying related Opportunities, Follow-ups, and Activities.

### Module C: Lead Management & Atomic Conversion
- Enforced qualification lifecycle: `New` &rarr; `Contacted` &rarr; `Qualified` &rarr; `Converted` (or `Unqualified` / `Lost`).
- **Atomic Conversion:** Qualified leads convert into both a Customer and an Opportunity in an atomic database transaction (`BeginTransactionAsync`), preventing duplicate conversion and preserving relationship foreign keys.

### Module D: Opportunity & Pipeline Management
- Stages: `Qualification`, `Proposal`, `Negotiation`, `Won`, `Lost`.
- Validation: Amount > 0, Probability (0–100%), Expected Close Date cannot be in the past for active deals.
- Dynamic Weighted Forecast: `(Amount * Probability) / 100`.

### Module E: Follow-Up Management
- Statuses: `Planned`, `Completed`, `Missed`, `Cancelled`.
- Validation: Planned dates cannot be scheduled earlier than today.
- Automatic Activity creation: Completing a follow-up automatically creates a completed CRM Activity record with user notes.

### Module F: Activity Management
- Log calls, meetings, emails, and tasks linked to Customers and Leads.

### Module G: User & Role Management (Admin-Only)
- User provisioning, role assignment, account activation/deactivation, lockout status check, and password resets.
- Safety Guardrail: Prevents administrators from accidentally deactivating or revoking their own administrator privileges.

### Module H: Immutable Audit Logging
- Server-side generated timestamps and IP address logging for authentication, record creation, updates, lead conversions, status transitions, and role assignments.

### Module I: Reporting & Data Export
- 8 Operational Reports: Customer Report, Lead Report, Follow-Up Report, Opportunity Report, Sales Pipeline Report, Lead Conversion Report, User Activity Report, Audit Trail Report.
- One-click CSV export with RFC 4180 sanitization against CSV formula injection.

### Module J: REST API & Swagger UI
- Fully versioned API at `/api/` with DTOs, consistent envelope responses (`ApiResponse<T>`), HTTP status codes (200, 201, 400, 401, 403, 404, 409), and interactive Swagger UI at `/swagger`.

---

## 4. Getting Started Locally

### Prerequisites
1. **.NET 8.0 SDK** (Installed or downloaded from Microsoft).
2. Any modern web browser (Edge, Chrome, Firefox).

### Step 1: Clone and Navigate
```powershell
cd c:\Users\suman\Downloads\Animesh-project
```

### Step 2: Database Configuration
The application is configured with **dual database provider support** in `appsettings.json`:
- **SQLite (Default):** Zero-friction local development without requiring a separate SQL Server engine. The file `AcxiomCRM.db` is created automatically.
- **SQL Server:** To switch to Microsoft SQL Server, change `"DatabaseProvider": "SqlServer"` and set your connection string in `appsettings.json`.

### Step 3: Run the Application
```powershell
dotnet run --project src/AcxiomCRM.Web/AcxiomCRM.Web.csproj
```
The application will start listening on:
- Web Application: **`http://localhost:5000`**
- Swagger Documentation: **`http://localhost:5000/swagger`**
- Health Checks: **`http://localhost:5000/health`**

---

## 5. Running Automated Tests

Run the comprehensive xUnit test suite (23 business, validation, scoping, and integration tests):
```powershell
dotnet test tests/AcxiomCRM.Tests/AcxiomCRM.Tests.csproj
```

### Test Coverage Highlights:
- `BusinessRuleTests.cs`: Probability range (0-100), weighted value calculations, positive amount validation.
- `CustomerServiceTests.cs`: Duplicate email prevention, duplicate phone prevention, audit log creation, Sales Executive record scoping.
- `LeadServiceTests.cs`: Validation, lost lead conversion rejection, invalid opportunity amount rejection.
- `OpportunityAndFollowUpServiceTests.cs`: Negative amount rejection, past close date rejection, past follow-up date rejection, atomic follow-up completion.
- `LeadConversionAndScopingTests.cs`: Sales Executive lead scoping, Sales Executive opportunity scoping, atomic Lead-to-Customer conversion transaction.

---

## 6. Docker Deployment

To launch AcxiomCRM along with a dedicated Microsoft SQL Server 2022 container:
```bash
docker-compose up --build -d
```
The application will be live at `http://localhost:5000`.

---

## 7. Security Hardening & Compliance Checklist

- [x] **ASP.NET Core Identity:** PBKDF2 with HMAC-SHA256 password hashing.
- [x] **Account Lockout:** Locks accounts for 15 minutes after 5 consecutive failed login attempts.
- [x] **Anti-Forgery:** Enforced across all state-changing Razor forms (`[ValidateAntiForgeryToken]`).
- [x] **Anti-Mass-Assignment:** Dedicated input DTOs prevent property over-posting.
- [x] **SQL Injection Defense:** All queries execute through parameterized Entity Framework Core expressions.
- [x] **Security Headers:** Enforces `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `X-XSS-Protection`, and `Referrer-Policy`.
- [x] **Authorization:** Enforced server-side via `[Authorize(Roles = ...)]` attributes and record-level filtering (`CurrentUserService`).
- [x] **Audit Trail:** Immutable, server-generated timestamps and IP tracking.
