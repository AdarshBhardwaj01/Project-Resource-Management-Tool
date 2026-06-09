# Project & Resource Management Tool (PRM)

A .NET 8 solution for managing employees, projects, resource allocations, and timesheets. It includes a **REST API** backend, a **console client**, JWT authentication, role-based access, scheduled background jobs, and an **AI assistant** for skill matching and project risk summaries.

---

## Table of Contents

- [Overview](#overview)
- [Features by Role](#features-by-role)
- [Architecture](#architecture)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
- [Default Login](#default-login)
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
| **PRM.Api** | ASP.NET Core Web API — business logic, database access, JWT auth, Swagger, background scheduler |
| **PRM.ConsoleUI** | Console client — calls the API over HTTP; role-based menus for Admin, Manager, and Employee |
| **PRM.Business** | Services, validation, AI layer, scheduler logic |
| **PRM.DataAccess** | EF Core `DbContext`, repositories, migrations, seed data |
| **PRM.Models** | Entities, DTOs, enums |
| **PRM.Common** | Shared constants, helpers, custom exceptions |

The console client does **not** access the database directly. All operations go through the API.

---

## Features by Role

### Admin

- Manage employees (add profile, update, deactivate, assign skills, **assign manager to team**)
- Create user accounts (Admin, Manager, Employee) — the only way to onboard users
- Manage projects (create, update, assign managers)
- View all allocations
- Manage users (Admin / Manager / Employee accounts)
- System configuration (max weekly hours, scheduler interval, LLM provider & API key)

### Manager

- **Resource Dashboard** — view **only employees assigned to that manager** (bench and allocated)
- **Allocate Resource** — allocate **only team members** to the manager's own projects (AI or direct)
- **My Projects** — project details, active allocations, **AI Risk Summary**
- **Timesheets** — review team timesheet submissions
- **AI Assistant** — skill match queries across the workforce

### Employee

- Submit weekly timesheets
- View submitted timesheets
- View current project allocations

### All Users

- Login and logout (accounts are created by Admin only — no self-registration)
- Forced password change on first login for Admin-created accounts

---

## Architecture

```
┌─────────────────┐         HTTP (JWT)         ┌─────────────────┐
│  PRM.ConsoleUI  │ ─────────────────────────► │     PRM.Api     │
└─────────────────┘                          └────────┬────────┘
                                                        │
                        ┌───────────────────────────────┼───────────────────────────────┐
                        │                               │                               │
                        ▼                               ▼                               ▼
                 PRM.Business                   PRM.DataAccess                  Background Scheduler
            (services, AI, rules)            (EF Core, repositories)         (utilisation & health)
                        │                               │
                        └───────────────┬───────────────┘
                                        ▼
                                 SQL Server (PRMToolDb)
```

**AI flow:** `ManagerService` → `IAiService` (`PrmAiService`) → Gemini or Groq LLM client. If no API key is configured or the LLM call fails, a **rule-based fallback** is used automatically.

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- **SQL Server** (LocalDB, Express, or full instance)
- Optional: API keys for [Google Gemini](https://ai.google.dev/) or [Groq](https://console.groq.com/) for live AI responses

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

### 3. Start the API

The API applies EF Core migrations and seeds the default admin user and system config on startup.

```bash
dotnet run --project PRM.Api
```

- API base URL: **http://localhost:5000**
- Swagger UI (Development): **http://localhost:5000/swagger**

### 4. Start the console client

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

### 5. Build the solution (optional)

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

## AI Configuration

AI features (skill match, project risk summary, allocate-resource search) read settings from the **System Configuration** table, editable from the Admin panel or via the API.

| Setting | Description | Default |
|---------|-------------|---------|
| **LLM Provider** | `Gemini` (1) or `Groq` (2) | Gemini |
| **LLM API Key** | Provider API key | Empty (fallback mode) |
| **Max Weekly Hours** | Used for utilisation & project health | 40 |
| **Scheduler Interval (hours)** | Background job frequency | 4 |

**Without an API key**, the app uses deterministic rule-based logic (`RuleBasedAiFallback`) so all AI screens remain functional for demos and testing.

**To enable live AI:**

1. Log in as **Admin**
2. Open **System Configuration**
3. Set the LLM provider and paste your API key
4. Save

**Manager AI endpoints:**

| Method | Endpoint | Purpose |
|--------|----------|---------|
| `POST` | `/api/manager/ai/skill-match` | Rank employees by required skills |
| `GET` | `/api/manager/ai/projects/{id}/risk-summary` | AI-generated project risk analysis |

Skill matching only considers employees **with assigned skills**, applies strict keyword matching, and can filter for **fully available** resources.

---

## Background Scheduler

`PrmBackgroundSchedulerService` runs inside the API process and executes on a configurable interval (default: every **4 hours**). Each cycle:

1. **Recomputes employee utilisation and status** based on active allocations
2. **Updates project health status** using allocation coverage and configured max weekly hours

The interval is read from System Configuration at the end of each cycle. Scheduler logic also runs on-demand when allocations change.

---

## API & Swagger

Controllers are grouped by domain:

| Controller | Route prefix | Access |
|------------|--------------|--------|
| `AuthController` | `/api/auth` | Public / authenticated |
| `UsersController` | `/api/users` | Admin |
| `EmployeesController` | `/api/employees` (includes `PUT /assign-manager`) | Admin |
| `ProjectsController` | `/api/projects` | Admin |
| `AllocationsController` | `/api/allocations` | Admin |
| `SystemConfigController` | `/api/system-config` | Admin |
| `ManagerController` | `/api/manager` | Manager |
| `EmployeePortalController` | `/api/employee` | Employee |
| `HealthController` | `/api/health` | Public |

Authenticate via `POST /api/auth/login`, then use the returned JWT in the `Authorization: Bearer <token>` header.

---

## Solution Structure

```
PRMTool/
├── PRM.Api/              REST API, controllers, middleware, background services
├── PRM.ConsoleUI/        Console client, screens, HTTP API clients
├── PRM.Business/         Business services, AI layer, validators, helpers
├── PRM.DataAccess/       DbContext, repositories, migrations, seed
├── PRM.Models/           Entities, DTOs, enums
└── PRM.Common/           Constants, shared helpers, exceptions
```

### Project references

| Project | References |
|---------|------------|
| PRM.ConsoleUI | PRM.Common |
| PRM.Api | PRM.Business, PRM.DataAccess, PRM.Models, PRM.Common |
| PRM.Business | PRM.DataAccess, PRM.Models, PRM.Common |
| PRM.DataAccess | PRM.Models, PRM.Common |
| PRM.Models | — |
| PRM.Common | — |

### Key folders

| Project | Folders |
|---------|---------|
| PRM.Models | `Entities/`, `DTOs/`, `Enums/` |
| PRM.DataAccess | `Context/`, `Repositories/`, `Seed/`, `Migrations/` |
| PRM.Business | `Interfaces/`, `Services/`, `Services/Ai/`, `Validators/`, `Helpers/` |
| PRM.Api | `Controllers/`, `Extensions/`, `BackgroundServices/` |
| PRM.ConsoleUI | `UI/Menus/`, `UI/Screens/`, `UI/Helpers/`, `Services/` |

---

## Design Principles

The solution follows a **layered architecture** with clear separation of concerns:

| Principle | How it is applied |
|-----------|-------------------|
| **Single Responsibility** | Controllers handle HTTP; services hold business rules; repositories handle data access |
| **Open/Closed** | AI providers implement `ILlmClient`; new providers can be added without changing callers |
| **Liskov Substitution** | Repository and service interfaces allow swapping implementations in DI |
| **Interface Segregation** | Focused interfaces (`IEmployeeRepository`, `IAiService`, `IManagerService`, etc.) |
| **Dependency Inversion** | All layers depend on abstractions registered in `BusinessServiceExtensions` / `Program.cs` |

**Patterns used:** Repository, Dependency Injection, DTO mapping, JWT authentication, background hosted service, strategy pattern (LLM clients + rule-based fallback).

---

## NuGet Packages

| Project | Packages |
|---------|----------|
| PRM.DataAccess | EF Core, EF Core SqlServer, EF Core Tools |
| PRM.Business | BCrypt.Net-Next |
| PRM.Api | JWT Bearer, EF Core Design, Swagger (Swashbuckle) |
| PRM.ConsoleUI | Microsoft.Extensions.Configuration, DI, Http |

---

## License

Learn & Code final project — internal / educational use.
