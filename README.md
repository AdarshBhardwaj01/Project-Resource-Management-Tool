# Project & Resource Management Tool (PRM)

A .NET 8 solution for managing employees, projects, resource allocations, and timesheets. It includes a **REST API** backend, a **console client**, JWT authentication, role-based access, scheduled background jobs with **SMTP email notifications**, and an **AI assistant** for skill matching, team building, and project risk summaries.

---

## Table of Contents

- [Overview](#overview)
- [Features by Role](#features-by-role)
- [Architecture](#architecture)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
- [Default Login](#default-login)
- [Email Configuration](#email-configuration)
- [Timesheet Workflow](#timesheet-workflow)
- [Project Health & Notifications](#project-health--notifications)
- [AI Configuration](#ai-configuration)
- [Background Scheduler](#background-scheduler)
- [API & Swagger](#api--swagger)
- [Solution Structure](#solution-structure)
- [Design Principles](#design-principles)
- [NuGet Packages](#nuget-packages)

---

## Overview

| Component | Description |
|-----------|-------------|
| **PRM.Api** | ASP.NET Core Web API — business logic, database access, JWT auth, Swagger, background scheduler, SMTP email |
| **PRM.ConsoleUI** | Console client — calls the API over HTTP; role-based menus for Admin, Manager, and Employee |
| **PRM.Business** | Services, validation, AI layer, scheduler logic, email notifications |
| **PRM.DataAccess** | EF Core `DbContext`, repositories, migrations, seed data |
| **PRM.Models** | Entities, DTOs, enums |
| **PRM.Common** | Shared constants, helpers, custom exceptions |

The console client does **not** access the database directly. All operations go through the API.

---

## Features by Role

### Admin

- Manage employees (add profile, update, deactivate, assign skills, **assign manager to team**)
- Create user accounts (Admin, Manager, Employee) — the only way to onboard users
- Manage projects (create, update, assign managers, milestones)
- View all allocations
- Manage users (Admin / Manager / Employee accounts)
- System configuration (max weekly hours, scheduler interval, LLM provider & API key)
- **Test email** via API (`POST /api/system-config/test-email`)

### Manager

- **Resource Dashboard** — view employees assigned to that manager; availability shown as `% free` (100% allocated = `0% free`)
- **Allocate Resource** — direct allocation to team members on own projects
- **Find Resource using AI** — org-wide skill search (suggestions only; no auto-allocation)
- **My Projects** — project details, risk flags, milestones, active allocations, **AI Risk Summary**
- **Timesheets** — review team timesheet submissions by week
- **Restore Frozen Timesheets** — unlock a frozen week so an employee can submit again
- **AI Assistant** — Skill Match (org-wide, combined skills on one employee), Team Builder

### Employee

- Submit weekly timesheets (Mon–Fri window; restored weeks require entering the correct Monday date)
- View submitted timesheets and status (PENDING, SUBMITTED, MISSED, FROZEN)
- View current project allocations

### All Users

- Login and logout (accounts are created by Admin only — no self-registration)
- Forced password change on first login for Admin-created accounts

---

## Architecture

```
┌─────────────────┐         HTTP (JWT)         ┌─────────────────────────────────────┐
│  PRM.ConsoleUI  │ ─────────────────────────► │            PRM.Api                  │
└─────────────────┘                            └──────────────────┬──────────────────┘
                                                                    │
                    ┌───────────────────────────────────────────────┼────────────────────────┐
                    │                                               │                        │
                    ▼                                               ▼                        ▼
             PRM.Business                                   PRM.DataAccess          PrmBackgroundSchedulerService
        (services, AI, email)                          (EF Core, repositories)     (hosted background loop)
                    │                                               │                        │
                    │                                               │                        ▼
                    │                                               │              IPrmSchedulerService
                    │                                               │                 ├── resource status
                    │                                               │                 ├── project health + AT RISK email
                    │                                               │                 └── timesheet workflow + reminder/freeze emails
                    │                                               │                        │
                    │                                               │                        ▼
                    │                                               │              SmtpEmailNotificationService (MailKit)
                    └───────────────────────────┬───────────────────┘                        │
                                                ▼                                            ▼
                                         SQL Server (PRMToolDb)                         Gmail / SMTP
```

**AI flow:** `ManagerService` → `IAiService` (`PrmAiService`) → Gemini, Groq, or Gemma LLM client. If no API key is configured or the LLM call fails, a **rule-based fallback** is used automatically.

**Scheduler + email:** There is no separate email worker or message queue. The background hosted service runs one cycle; business services call `IEmailNotificationService` inline with `await` during that cycle. SMTP failures are logged and do not crash the scheduler.

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- **SQL Server** (LocalDB, Express, or full instance)
- Optional: API keys for [Google Gemini](https://ai.google.dev/), [Groq](https://console.groq.com/), or a self-hosted Gemma endpoint
- Optional: Gmail App Password or SMTP credentials for email notifications

---

## Getting Started

### 1. Clone and open the solution

```bash
cd PRMTool
dotnet restore PRMTool.sln
```

### 2. Configure the database connection

Edit `PRM.Api/appsettings.json` and set your SQL Server connection string:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.\\SQLEXPRESS;Database=PRMToolDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}
```

Adjust `Server=` for your environment (e.g. `(localdb)\\mssqllocaldb`).

Migrations are applied automatically on API startup. If you add migrations manually:

```bash
dotnet ef database update --project PRM.DataAccess --startup-project PRM.Api
```

### 3. Configure email (optional, Development)

Add SMTP settings to `PRM.Api/appsettings.Development.json`:

```json
"EmailSettings": {
  "Email": "your-email@gmail.com",
  "Password": "your-gmail-app-password",
  "Host": "smtp.gmail.com",
  "Port": 587
}
```

Run the API with `ASPNETCORE_ENVIRONMENT=Development` so Development settings load. Prefer **User Secrets** for passwords in local dev — do not commit credentials.

### 4. Start the API

The API applies EF Core migrations and seeds the default admin user and system config on startup.

```bash
dotnet run --project PRM.Api
```

- API base URL: **http://localhost:5000**
- Swagger UI (Development): **http://localhost:5000/swagger**

The background scheduler runs **immediately on startup**, then every N hours (see [Background Scheduler](#background-scheduler)).

### 5. Start the console client

In a **second terminal**:

```bash
dotnet run --project PRM.ConsoleUI
```

Ensure `PRM.ConsoleUI/appsettings.json` points to the same API URL:

```json
"ApiSettings": {
  "BaseUrl": "http://localhost:5000"
}
```

### 6. Build the solution (optional)

```bash
dotnet build PRMTool.sln
```

> **Tip:** Stop running API/Console processes before rebuilding to avoid file-lock errors.

---

## Default Login

On first run, a bootstrap admin account is created:

| Field | Value |
|-------|-------|
| Username | `admin` |
| Password | `Admin@1234` |
| Email | `admin@techserve.com` |

You will be prompted to **change the password** on first login.

---

## Email Configuration

Email is sent via **`SmtpEmailNotificationService`** (MailKit) using the `EmailSettings` section:

| Setting | Description |
|---------|-------------|
| `Email` | SMTP username and From address |
| `Password` | SMTP password (Gmail: use App Password) |
| `Host` | e.g. `smtp.gmail.com` |
| `Port` | e.g. `587` (STARTTLS) |

### When emails are sent

| Event | Employee email | Manager email |
|-------|----------------|---------------|
| Timesheet reminder #1 (Mon after missed week) | Submit your timesheet | Team member has not submitted |
| Timesheet reminder #2 (Tue) | Submit your timesheet | Follow-up notification |
| Timesheet frozen (Wed+) | Access frozen | Employee missed; use Restore Frozen Timesheets |
| Project health → **AT RISK** | — | Project AT RISK (once per event) |

Employee and manager receive **separate emails** with role-specific content (not CC on the same message).

### Test email

As Admin, call:

```http
POST /api/system-config/test-email
Authorization: Bearer <token>
Content-Type: application/json

{}
```

Optional body: `{ "toEmail": "someone@example.com" }` (defaults to configured `EmailSettings.Email`).

Recipient addresses for automated emails come from **`Users.Email`** (employee, reporting manager, or project manager).

---

## Timesheet Workflow

### Submission window

- Week runs **Monday–Sunday**; employees submit hours for **Monday–Friday**.
- **Mon–Fri (current week):** status shows **PENDING** if not submitted.
- **From Sunday** onward (for that week): status shows **MISSED** if still not submitted.

### Scheduler actions (previous week)

After last week’s Friday deadline, the background scheduler processes **last week** for each allocated employee:

| Working day after deadline | Action |
|----------------------------|--------|
| Monday | Reminder #1 emails |
| Tuesday | Reminder #2 emails |
| Wednesday+ | Freeze timesheet + frozen/missed emails |

Frozen timesheets are stored with `IsFrozen = true`. The employee cannot submit until a manager restores access.

### Manager restore

1. Manager menu → **Restore Frozen Timesheets**
2. Select employee + week → restore
3. Sets `IsUnlockedByManager = true`, clears freeze
4. Employee submits using the **Monday week start date** (DD-MM-YYYY) for that restored week

The scheduler skips timesheets that are unlocked by a manager so they are not re-frozen on the next cycle.

### Key entities / fields

| Field | Purpose |
|-------|---------|
| `Timesheets.IsFrozen` | Timesheet locked for submission |
| `Timesheets.IsUnlockedByManager` | Manager restored access |
| `Timesheets.ReminderCount` | Tracks reminder 1 / 2 |

---

## Project Health & Notifications

Project health is stored in **`Projects.HealthStatus`** and updated by the **background scheduler** (and when allocations change). The manager UI reads this stored value for the health badge; risk flags and milestone OVERDUE labels are computed live when viewing project detail.

### Health rules (`ProjectHealthCalculator`)

| Status | Condition |
|--------|-----------|
| **AT RISK** | Any milestone past due date and not Done |
| **ATTENTION** | Allocated employees logged fewer hours than expected (recent weeks) |
| **ON TRACK** | Otherwise |

### AT RISK email (once only)

When health **changes to** AT RISK, an email is sent to the **project manager** (`Projects.Manager` → `Users.Email`).

- Tracked by **`Projects.AtRiskNotificationSentAt`** — prevents duplicate emails on every scheduler cycle
- Flag is cleared when health improves so a future AT RISK event can notify again

---

## AI Configuration

AI features read settings from the **System Configuration** table, editable from the Admin panel or via the API.

| Setting | Description | Default |
|---------|-------------|---------|
| **LLM Provider** | `Gemini` (1), `Groq` (2), or `Gemma` (3) | Gemini |
| **LLM API Key** | Provider API key | Empty (fallback mode) |
| **Max Weekly Hours** | Used for utilisation & project health | 40 |
| **Scheduler Interval (hours)** | Background job frequency | 4 |

**Without an API key**, the app uses deterministic rule-based logic (`RuleBasedAiFallback`) so AI screens remain functional for demos and testing.

**To enable live AI:**

1. Log in as **Admin**
2. Open **System Configuration**
3. Set the LLM provider and paste your API key
4. Save

### Manager AI features

| Feature | Behavior |
|---------|----------|
| **Skill Match** (AI Assistant) | Org-wide search; prefers one employee matching all required skills |
| **Find Resource using AI** (Allocate menu) | Org-wide search; suggestions only — no auto-allocation |
| **Team Builder** | Rule-based team suggestions from org bench |
| **Risk Summary** | AI or fallback analysis from project milestones, allocations, risk flags |

**Manager AI endpoints:**

| Method | Endpoint | Purpose |
|--------|----------|---------|
| `POST` | `/api/manager/ai/skill-match` | Rank employees by required skills |
| `POST` | `/api/manager/ai/team-build` | Build a team from requirements |
| `GET` | `/api/manager/ai/projects/{id}/risk-summary` | AI-generated project risk analysis |

---

## Background Scheduler

`PrmBackgroundSchedulerService` is an ASP.NET Core **hosted service** that runs inside the API process:

1. Runs one cycle **immediately** when the API starts
2. Sleeps for **Scheduler Interval (hours)** from System Configuration (default: **4**)
3. Repeats

Each cycle calls `IPrmSchedulerService.RunScheduledTasksAsync()`:

| Step | Service | What it does |
|------|---------|--------------|
| 1 | `PrmSchedulerService` | Recompute employee utilisation and bench/allocated status |
| 2 | `PrmSchedulerService` | Recompute project health; send **AT RISK** email when health newly becomes AT RISK |
| 3 | `TimesheetSchedulerService` | Process last week’s timesheets; send reminders / freeze emails |

The interval is re-read from System Configuration at the end of each cycle. Project health also recomputes on-demand when manager allocations are created or ended.

**Note:** The scheduler interval is in **hours** only (minimum 1 via Admin UI). For immediate testing, restart the API to trigger one cycle.

---

## API & Swagger

Controllers are grouped by domain:

| Controller | Route prefix | Access |
|------------|--------------|--------|
| `AuthController` | `/api/auth` | Public / authenticated |
| `UsersController` | `/api/users` | Admin |
| `EmployeesController` | `/api/employees` | Admin |
| `ProjectsController` | `/api/projects` | Admin |
| `AllocationsController` | `/api/allocations` | Admin |
| `SystemConfigController` | `/api/system-config` | Admin |
| `ManagerController` | `/api/manager` | Manager |
| `EmployeePortalController` | `/api/employee` | Employee |
| `HealthController` | `/api/health` | Public |

### Notable endpoints

| Method | Endpoint | Purpose |
|--------|----------|---------|
| `POST` | `/api/system-config/test-email` | Send SMTP test email (Admin) |
| `GET` | `/api/manager/timesheets/frozen` | List frozen timesheets for manager's team |
| `POST` | `/api/manager/timesheets/frozen/restore` | Restore employee timesheet access for a week |
| `GET` | `/api/manager/timesheets` | Team timesheets by week |
| `POST` | `/api/employee/timesheets` | Submit timesheet |

Authenticate via `POST /api/auth/login`, then use the returned JWT in the `Authorization: Bearer <token>` header.

---

## Solution Structure

```
PRMTool/
├── PRM.Api/              REST API, controllers, middleware, background services
├── PRM.ConsoleUI/        Console client, screens, HTTP API clients
├── PRM.Business/         Business services, AI layer, email, scheduler helpers
├── PRM.DataAccess/       DbContext, repositories, migrations, seed
├── PRM.Models/           Entities, DTOs, enums
├── PRM.Common/           Constants, shared helpers, exceptions
└── PRM.Tests/            Unit tests
```

### Project references

| Project | References |
|---------|------------|
| PRM.ConsoleUI | PRM.Common |
| PRM.Api | PRM.Business, PRM.DataAccess, PRM.Models, PRM.Common |
| PRM.Business | PRM.Models, PRM.Common |
| PRM.DataAccess | PRM.Business, PRM.Models, PRM.Common |
| PRM.Tests | PRM.Business, PRM.Models, PRM.Common |

### Key folders

| Project | Folders |
|---------|---------|
| PRM.Models | `Entities/`, `DTOs/`, `Enums/` |
| PRM.DataAccess | `Context/`, `Repositories/`, `Seed/`, `Migrations/` |
| PRM.Business | `Interfaces/`, `Services/`, `Services/Ai/`, `Helpers/` |
| PRM.Api | `Controllers/`, `Extensions/`, `BackgroundServices/` |
| PRM.ConsoleUI | `UI/Menus/`, `UI/Screens/`, `UI/Helpers/`, `Services/` |

---

## Design Principles

The solution follows a **layered architecture** with clear separation of concerns:

| Principle | How it is applied |
|-----------|-------------------|
| **Single Responsibility** | Controllers handle HTTP; services hold business rules; repositories handle data access |
| **Open/Closed** | AI providers implement `ILlmClient`; email via `IEmailNotificationService` |
| **Liskov Substitution** | Repository and service interfaces allow swapping implementations in DI |
| **Interface Segregation** | Focused interfaces (`IManagerService`, `IEmailNotificationService`, etc.) |
| **Dependency Inversion** | All layers depend on abstractions registered in `BusinessServiceExtensions` |

**Patterns used:** Repository, Dependency Injection, DTO mapping, JWT authentication, background hosted service, strategy pattern (LLM clients + rule-based fallback), SMTP notification service.

---

## NuGet Packages

| Project | Packages |
|---------|----------|
| PRM.DataAccess | EF Core, EF Core SqlServer, EF Core Tools |
| PRM.Business | BCrypt.Net-Next, AutoMapper, MailKit, Microsoft.Extensions.Http |
| PRM.Api | JWT Bearer, EF Core Design, Swagger (Swashbuckle) |
| PRM.ConsoleUI | Microsoft.Extensions.Configuration, DI, Http |
| PRM.Tests | xUnit, Moq |

---

## License

Learn & Code final project — internal / educational use.
