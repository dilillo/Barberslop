# Implementation Plan: Barber Client Booking Platform

**Branch**: `copilot/create-plan-for-barber-solution` | **Date**: 2026-06-04 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/001-barber-client-booking/spec.md`

## Summary

Build the Barberslop booking platform — a multi-barber, multi-client appointment scheduling application. Clients self-register, discover barbers, and request to join a barber's clientele; barbers configure their services, schedule, and per-client booking limits. The system books, reminders, and cancels appointments across a reliable availability engine. The technical approach uses .NET 10 with ASP.NET Core Razor Pages for the web UI, .NET Aspire for local orchestration and Azure-hosted deployment, PostgreSQL (EF Core/Npgsql) for persistence, Bootstrap with a classic barber theme (deep red/cream/navy, barber-pole motifs), xUnit for unit tests, and Testcontainers-backed integration tests. GitHub Actions deploy to Azure via the Aspire Azure Developer CLI (`azd`) pipeline.

## Technical Context

**Language/Version**: C# on .NET 10

**Primary Dependencies**: ASP.NET Core Razor Pages, .NET Aspire (AppHost + ServiceDefaults), Entity Framework Core 9 with Npgsql provider, Bootstrap 5, xUnit 2, Testcontainers.PostgreSQL, FluentValidation, SendGrid (email), Twilio (SMS)

**Storage**: PostgreSQL 16

**Testing**: xUnit 2 (unit tests); ASP.NET Core WebApplicationFactory + Testcontainers (integration tests); coverlet for coverage

**Target Platform**: Azure Container Apps (via .NET Aspire publish/`azd up`)

**Project Type**: Web application — ASP.NET Core Razor Pages served by Aspire-orchestrated topology

**Performance Goals**: Availability search response p95 ≤ 2 s (NFR-002). Booking confirmation p95 ≤ 2 s (SC-002).

**Constraints**: Windows-compatible contributor workflow; no POSIX-only tooling; input validation on all forms; authorization boundary between barber and client roles; OWASP Top 10 mitigated

**Scale/Scope**: Multi-barber/multi-client platform — invitation lifecycle, appointment booking, service catalog, availability engine, reminder dispatch on four channels

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- [x] Code quality approach keeps the design simple, maintainable, and consistent with SOLID where useful. Vertical slices enforce cohesion and prevent cross-cutting entanglement.
- [x] Naming conventions: C# camelCase for locals/parameters, PascalCase for types/properties/methods — consistently applied across all slices.
- [x] Automated testing strategy defined: unit tests cover booking-limit enforcement, availability calculation, invitation state machine, and reminder scheduling logic. Integration tests exercise DB persistence, form submissions, and end-to-end workflows per acceptance scenario. Coverage gate: ≥ 90% for delivered behavior.
- [x] Public interface documentation: Razor Page routes and form contracts documented in `contracts/`. Developer quickstart in `quickstart.md`. Behavior changes noted in `data-model.md`.
- [x] Security: FluentValidation validates all form inputs; EF Core parameterized queries prevent SQL injection; ASP.NET Core authorization policies enforce barber/client role boundaries; contact data access scoped to authorized actors; CSRF protection via antiforgery tokens (default in Razor Pages); reminder channel failures handled without data loss.
- [x] Vertical Slice architecture: each major workflow (booking, invitation, schedule, reminders) is a self-contained slice with its own page, handler, service, domain rules, and tests.
- [x] Performance target declared: p95 ≤ 2 s for availability search and booking operations; no premature optimization otherwise.
- [x] Semantic versioning: initial release tagged `1.0.0`; future breaking contract changes follow `MAJOR.MINOR.PATCH`.
- [x] Windows compatibility: all scripts use PowerShell; `azd`, .NET CLI, and Docker Desktop run on Windows; no POSIX-only dependency.

## Project Structure

### Documentation (this feature)

```text
specs/001-barber-client-booking/
├── plan.md              # This file
├── research.md          # Phase 0 research
├── data-model.md        # Phase 1 entity model
├── quickstart.md        # Phase 1 local/test/deploy guide
├── contracts/           # Phase 1 UI/page contracts
│   ├── booking.md
│   ├── invitation.md
│   ├── availability.md
│   └── reminder-events.md
└── tasks.md             # Phase 2 task list (created by /speckit.tasks)
```

### Source Code (repository root)

```text
Barberslop.sln

src/
├── Barberslop.AppHost/          # .NET Aspire AppHost — wires all services
├── Barberslop.ServiceDefaults/  # Aspire shared service defaults (telemetry, health)
├── Barberslop.Web/              # ASP.NET Core Razor Pages web app
│   ├── Pages/
│   │   ├── Shared/              # _Layout.cshtml, _BarberPole partial
│   │   ├── Booking/             # Book, Confirm, Cancel pages
│   │   ├── Invitation/          # Request, Accept, Disinvite pages
│   │   ├── Schedule/            # Availability, Vacation pages
│   │   ├── Services/            # Service catalog pages
│   │   └── Account/             # Register, Login pages
│   ├── Features/                # Vertical-slice handlers and services
│   │   ├── Booking/
│   │   ├── Invitation/
│   │   ├── Schedule/
│   │   ├── Services/
│   │   └── Reminders/
│   ├── Domain/                  # Core domain models and interfaces
│   ├── Data/                    # EF Core DbContext and migrations
│   ├── Infrastructure/          # Reminder channel adapters (email, SMS, push, calendar)
│   └── wwwroot/
│       ├── css/
│       │   └── barberslop.css   # Bootstrap overrides + barber theme
│       └── images/
│           └── barber-pole.svg  # Reusable barber-pole asset

tests/
├── Barberslop.UnitTests/        # xUnit unit tests (domain logic, services)
└── Barberslop.IntegrationTests/ # xUnit + WebApplicationFactory + Testcontainers

.github/
└── workflows/
    ├── ci.yml                   # Build + test on every PR
    └── deploy-azure.yml         # azd up to Azure on merge to main
```

**Structure Decision**: Aspire multi-project layout. `Barberslop.AppHost` orchestrates the web app and PostgreSQL container locally and publishes Bicep/Azure resources via `azd`. `Barberslop.Web` owns all Razor Pages, vertical-slice feature handlers, EF Core data layer, and reminder infrastructure adapters. Tests live in two xUnit projects: one for pure domain/unit tests, one for integration tests backed by Testcontainers.

## Complexity Tracking

> No constitution violations required. No entries needed.
