# Quickstart: Barberslop Development Guide

**Feature**: 001-barber-client-booking  
**Phase**: 1 — Design  
**Date**: 2026-06-04

---

## Prerequisites

| Tool | Version | Install |
|---|---|---|
| .NET SDK | 10.x | https://dot.net |
| Docker Desktop | 4.x+ | https://www.docker.com/products/docker-desktop (Windows) |
| PowerShell | 7.x+ | Included in Windows 11; install via `winget install Microsoft.PowerShell` |
| Azure Developer CLI (`azd`) | latest | `winget install Microsoft.Azd` |
| Entity Framework CLI | latest | `dotnet tool install -g dotnet-ef` |

> All tools run on Windows. No POSIX-only tooling is required.

---

## 1. Clone and Restore

```powershell
git clone https://github.com/dilillo/Barberslop.git
cd Barberslop
dotnet restore
```

---

## 2. Run Locally with .NET Aspire

Start the Aspire AppHost, which automatically starts a PostgreSQL container and the web app:

```powershell
dotnet run --project src/Barberslop.AppHost
```

- The Aspire dashboard opens at **https://localhost:15888**
- The web app is available at **https://localhost:7001** (port configured in `launchSettings.json`)
- The PostgreSQL container is provisioned automatically on port `5432`

Aspire applies EF Core migrations automatically on web app startup (via `MigrateAsync()` in the startup pipeline).

### User Secrets for External Providers (optional for local dev)

Email and SMS reminders require API keys. Skip this step for local dev — reminders will be logged but not dispatched:

```powershell
cd src/Barberslop.Web
dotnet user-secrets set "SendGrid:ApiKey" "YOUR_SENDGRID_KEY"
dotnet user-secrets set "Twilio:AccountSid" "YOUR_ACCOUNT_SID"
dotnet user-secrets set "Twilio:AuthToken" "YOUR_AUTH_TOKEN"
dotnet user-secrets set "Twilio:FromNumber" "+15551234567"
```

---

## 3. Database Migrations

Migrations are managed with EF Core CLI:

```powershell
# Add a new migration
cd src/Barberslop.Web
dotnet ef migrations add <MigrationName> --output-dir Data/Migrations

# Apply migrations manually (normally done automatically at startup)
dotnet ef database update
```

The connection string for local dev is injected by Aspire. To run migrations against a specific database manually:

```powershell
dotnet ef database update --connection "Host=localhost;Port=5432;Database=barberslop;Username=postgres;******"
```

---

## 4. Running Tests

### Unit Tests

```powershell
cd tests/Barberslop.UnitTests
dotnet test
```

Unit tests cover:
- Availability engine slot calculation (`AvailabilityServiceTests`)
- Booking-limit enforcement (`BookingLimitPolicyTests`)
- Invitation state machine transitions (`InvitationStateMachineTests`)
- Reminder dispatch scheduling (`ReminderSchedulerTests`)
- Disinvite/reinvite constraint logic (`DisinviteConstraintTests`)

### Integration Tests

Integration tests require Docker Desktop running (Testcontainers starts a real PostgreSQL container):

```powershell
cd tests/Barberslop.IntegrationTests
dotnet test
```

Integration tests cover:
- Full booking form round-trip (POST → redirect → confirm page)
- Database persistence and EF Core query correctness
- Authorization enforcement (barber vs. client role boundaries)
- Acceptance scenarios from `spec.md` (User Stories 1–3)
- Edge cases: double-booking prevention, vacation conflict detection

### All Tests with Coverage

```powershell
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
```

Coverage reports are written to `TestResults/coverage.cobertura.xml`. Open with ReportGenerator:

```powershell
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:Html
start coverage-report/index.html
```

---

## 5. Deploy to Azure with `azd`

### First-Time Setup

```powershell
azd auth login
azd init   # Select "Use existing project" and point to this repo root
azd up     # Provision + deploy to Azure
```

`azd up` provisions:
- Azure Container Registry
- Azure Container Apps environment
- Azure Database for PostgreSQL Flexible Server
- Managed identity + Key Vault for secrets
- Application Insights (via Aspire OpenTelemetry export)

### Subsequent Deployments

```powershell
azd deploy   # Deploy code changes without re-provisioning infrastructure
```

### GitHub Actions CI/CD

The repository includes two workflows in `.github/workflows/`:

| Workflow | Trigger | Description |
|---|---|---|
| `ci.yml` | Every PR | Build + run all tests |
| `deploy-azure.yml` | Push to `main` | `azd up` to Azure (requires OIDC secrets in repo) |

**Required GitHub Secrets for `deploy-azure.yml`**:

| Secret | Description |
|---|---|
| `AZURE_CLIENT_ID` | Federated identity client ID |
| `AZURE_TENANT_ID` | Azure tenant ID |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription ID |

Configure federated credentials using:

```powershell
azd pipeline config --provider github
```

This command registers the OIDC federation and sets the required GitHub secrets automatically.

---

## 6. Bootstrap Theme and Barber-Pole Assets

### Theme Variables

The barber color palette is defined in `src/Barberslop.Web/wwwroot/css/barberslop.css`:

```css
:root {
  --barber-red:   #8B1A1A;
  --barber-cream: #FFF5E1;
  --barber-navy:  #1A2744;
  --barber-black: #111111;

  /* Bootstrap token overrides */
  --bs-primary:          var(--barber-red);
  --bs-primary-rgb:      139, 26, 26;
  --bs-body-bg:          var(--barber-cream);
  --bs-body-color:       var(--barber-black);
  --bs-link-color:       var(--barber-navy);
}
```

Import order in `_Layout.cshtml`:
1. Bootstrap 5 CDN CSS
2. `/css/barberslop.css` (overrides + barber-specific components)

### Barber-Pole Partial

The animated SVG barber pole is encapsulated in `Pages/Shared/_BarberPole.cshtml`.

**Usage**:
```html
<partial name="_BarberPole" />
```

The partial renders an inline SVG with a CSS keyframe animation (`@keyframes barberSpin`) that rotates the stripe pattern. The animation is paused when `prefers-reduced-motion: reduce` is set (WCAG 2.1 compliance).

**Placement guidelines**:
- Navigation bar: 32 × 32 px icon before the site name
- Page headers: 48 × 48 px decorative pole beside the `<h1>`
- Loading state spinner: replace Bootstrap spinner with the pole animation during async slot loading

### Adding Bootstrap Components

Use standard Bootstrap 5 class names. The theme overrides apply automatically via CSS custom properties. Do not add additional CSS frameworks.

---

## 7. Project Structure Quick Reference

```text
Barberslop.sln
src/
  Barberslop.AppHost/          ← Aspire AppHost (entry point for local run)
  Barberslop.ServiceDefaults/  ← Shared Aspire service configuration
  Barberslop.Web/              ← Razor Pages web application
    Pages/                     ← Razor Pages (.cshtml + .cshtml.cs)
    Features/                  ← Vertical-slice services and handlers
    Domain/                    ← Domain models, enums, interfaces
    Data/                      ← EF Core DbContext, migrations
    Infrastructure/             ← Reminder channel adapters
    wwwroot/css/                ← Bootstrap override theme
    wwwroot/images/             ← barber-pole.svg and static assets
tests/
  Barberslop.UnitTests/        ← Fast, no I/O domain tests
  Barberslop.IntegrationTests/ ← Docker-backed integration tests
.github/workflows/
  ci.yml                       ← PR build + test
  deploy-azure.yml             ← Production deploy via azd
specs/001-barber-client-booking/
  spec.md                      ← Feature specification
  plan.md                      ← This implementation plan
  research.md                  ← Technology decisions
  data-model.md                ← Entity model
  contracts/                   ← UI/service contracts
  tasks.md                     ← Implementation task list (created by /speckit.tasks)
```
