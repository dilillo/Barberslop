---

description: "Task list for Barber Client Booking Platform implementation"
---

# Tasks: Barber Client Booking Platform

**Input**: Design documents from `specs/001-barber-client-booking/`

**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅, quickstart.md ✅

**Tests**: Included — the constitution requires ≥ 90% coverage; research.md defines the full xUnit + Testcontainers testing strategy.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Solution scaffolding, Aspire topology, CI/CD workflows, and shared UI assets

- [ ] T001 Scaffold solution with all projects: `Barberslop.sln`, `src/Barberslop.AppHost/`, `src/Barberslop.ServiceDefaults/`, `src/Barberslop.Web/`, `tests/Barberslop.UnitTests/`, `tests/Barberslop.IntegrationTests/` per implementation plan
- [ ] T002 Configure .NET Aspire AppHost to orchestrate the web app and PostgreSQL container resource in `src/Barberslop.AppHost/Program.cs`
- [ ] T003 [P] Configure `Barberslop.ServiceDefaults` with OpenTelemetry, health checks, and service discovery defaults in `src/Barberslop.ServiceDefaults/Extensions.cs`
- [ ] T004 [P] Add Bootstrap 5 and barber theme CSS (deep red/cream/navy, barber-pole motifs) in `src/Barberslop.Web/wwwroot/css/barberslop.css` and `src/Barberslop.Web/wwwroot/images/barber-pole.svg`
- [ ] T005 [P] Create GitHub Actions CI workflow (build + test on every PR) in `.github/workflows/ci.yml`
- [ ] T006 [P] Create GitHub Actions Azure deploy workflow (`azd up` on merge to main, OIDC credentials) in `.github/workflows/deploy-azure.yml`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [ ] T007 Configure ASP.NET Core Identity with `Barber` and `Client` roles, registration/login pages, and antiforgery token middleware in `src/Barberslop.Web/Program.cs` and `src/Barberslop.Web/Pages/Account/`
- [ ] T008 [P] Create all domain entity classes — `BarberProfile`, `ClientProfile`, `FamilyMember`, `ServiceOffering`, `AvailabilityRule`, `VacationPeriod`, `TemporaryUnavailability`, `InvitationRequest`, `Appointment`, `ReminderDispatch` — with enums (`InvitationStatus`, `AppointmentStatus`, `DisinviteReason`, `ReminderChannel`, `ReminderStatus`, `BookingActorRole`, `InitiatorType`) in `src/Barberslop.Web/Domain/`
- [ ] T009 Create `BarberDbContext` with EF Core entity configurations and relationship mappings (FK constraints, value conversions, indexes) in `src/Barberslop.Web/Data/BarberDbContext.cs`
- [ ] T010 Add initial EF Core migration and configure automatic `MigrateAsync()` startup in `src/Barberslop.Web/Data/Migrations/` and `src/Barberslop.Web/Program.cs`
- [ ] T011 [P] Create shared `_Layout.cshtml` with Bootstrap 5 barber theme, navigation (role-adaptive), and barber-pole partial in `src/Barberslop.Web/Pages/Shared/_Layout.cshtml` and `src/Barberslop.Web/Pages/Shared/_BarberPole.cshtml`
- [ ] T012 [P] Register FluentValidation with ASP.NET Core DI and configure validation error display convention in `src/Barberslop.Web/Program.cs`
- [ ] T013 [P] Configure `Barberslop.UnitTests` xUnit project with xUnit 2 and coverlet dependencies in `tests/Barberslop.UnitTests/Barberslop.UnitTests.csproj`
- [ ] T014 [P] Configure `Barberslop.IntegrationTests` xUnit project with `WebApplicationFactory`, `Testcontainers.PostgreSQL`, and coverlet dependencies in `tests/Barberslop.IntegrationTests/Barberslop.IntegrationTests.csproj`

**Checkpoint**: Foundation ready — user story implementation can now begin

---

## Phase 3: User Story 1 — Book Appointments Within Availability Rules (Priority: P1) 🎯 MVP

**Goal**: Clients and barbers can book appointments for offered services by choosing a specific slot or "first available," with availability, duration, and per-client booking limits enforced.

**Independent Test**: Create a barber with weekly availability rules and a service, confirm a client with an active invitation can book a valid slot and is blocked when the per-client limit is reached or the slot is unavailable.

### Tests for User Story 1

- [ ] T015 [P] [US1] Unit tests for `IAvailabilityService` slot calculation (weekly rules, vacation overlap, temporary blocks, service duration, no double-booking) in `tests/Barberslop.UnitTests/AvailabilityServiceTests.cs`
- [ ] T016 [P] [US1] Unit tests for booking-limit enforcement policy (`BookingLimitPolicyTests`) in `tests/Barberslop.UnitTests/BookingLimitPolicyTests.cs`
- [ ] T017 [P] [US1] Integration tests for full booking HTTP round-trip: client books slot, barber books on behalf of client, limit-exceeded rejection, slot-unavailable rejection in `tests/Barberslop.IntegrationTests/BookingWorkflowTests.cs`

### Implementation for User Story 1

- [ ] T018 [P] [US1] Implement `IAvailabilityService` with `GetAvailableSlotsAsync` and `GetFirstAvailableSlotAsync` per the availability contract algorithm (load rules → subtract vacation/blocks/appointments → filter by service duration) in `src/Barberslop.Web/Features/Booking/AvailabilityService.cs`
- [ ] T019 [US1] Implement `IBookingService` with `CreateAppointmentAsync` enforcing active-invitation check, booking-limit check (`FR-014`), slot-availability check, and `AppointmentStatus` transitions in `src/Barberslop.Web/Features/Booking/BookingService.cs`
- [ ] T020 [P] [US1] Implement `GET /Booking/Book` Razor Page (renders service list, available slots, family-member selector) and `POST /Booking/Book` handler (validates and delegates to `IBookingService`) in `src/Barberslop.Web/Pages/Booking/Book.cshtml` and `src/Barberslop.Web/Pages/Booking/Book.cshtml.cs`
- [ ] T021 [US1] Implement `GET /Booking/Confirm` Razor Page displaying appointment summary and registered reminder channels in `src/Barberslop.Web/Pages/Booking/Confirm.cshtml` and `src/Barberslop.Web/Pages/Booking/Confirm.cshtml.cs`
- [ ] T022 [US1] Implement `POST /Booking/Cancel` handler and `GET /Booking/Cancelled` Razor Page displaying next 3 rebooking slots and first-available link (`FR-018`) in `src/Barberslop.Web/Pages/Booking/Cancelled.cshtml` and `src/Barberslop.Web/Pages/Booking/Cancelled.cshtml.cs`
- [ ] T023 [P] [US1] Implement barber service catalog `GET /Services` and `POST /Services/Add` / `POST /Services/Deactivate` Razor Pages (name, duration, price, active flag) in `src/Barberslop.Web/Pages/Services/`
- [ ] T024 [P] [US1] Add FluentValidation validators for booking form (`BookPageModel`, `CancelPageModel`) with all error codes from booking contract in `src/Barberslop.Web/Features/Booking/BookingValidators.cs`
- [ ] T025 [US1] Add family-member management pages (`GET /Account/FamilyMembers`, `POST /Account/FamilyMembers/Add`) so clients can link family members before booking (`FR-015`) in `src/Barberslop.Web/Pages/Account/FamilyMembers.cshtml` and `src/Barberslop.Web/Pages/Account/FamilyMembers.cshtml.cs`

**Checkpoint**: User Story 1 fully functional and independently testable — MVP deliverable

---

## Phase 4: User Story 2 — Manage Barber-Client Relationship Lifecycle (Priority: P2)

**Goal**: Clients self-register, discover barbers, request to join clientele, and receive acceptance notifications; barbers accept/reject requests, disinvite clients with a reason, and re-invite only from the barber side.

**Independent Test**: Complete self-registration → barber discovery → invitation request → barber acceptance → client notification → barber disinvitation → blocked re-request flow without using advanced scheduling features.

### Tests for User Story 2

- [ ] T026 [P] [US2] Unit tests for `InvitationStatus` state machine transitions (Pending → Accepted → Active, Active → Disinvited, re-invite guard) in `tests/Barberslop.UnitTests/InvitationStateMachineTests.cs`
- [ ] T027 [P] [US2] Unit tests for disinvite/reinvite constraint logic (blocked request after disinvite, barber-only reinvite) in `tests/Barberslop.UnitTests/DisinviteConstraintTests.cs`
- [ ] T028 [P] [US2] Integration tests for full invitation lifecycle HTTP round-trips: discover, request, accept, notification, disinvite, blocked re-request in `tests/Barberslop.IntegrationTests/InvitationLifecycleTests.cs`

### Implementation for User Story 2

- [ ] T029 [US2] Implement `IInvitationService` (request, accept, reject, disinvite, reinvite) with all `InvitationStatus` state transitions and invariants (one non-Disinvited record per barber-client pair) in `src/Barberslop.Web/Features/Invitation/InvitationService.cs`
- [ ] T030 [P] [US2] Implement `GET /Invitation/Discover` Razor Page with barber search by salon name, barber name, and geographic area filters in `src/Barberslop.Web/Pages/Invitation/Discover.cshtml` and `src/Barberslop.Web/Pages/Invitation/Discover.cshtml.cs`
- [ ] T031 [P] [US2] Implement `POST /Invitation/Request` handler and `GET /Invitation/Requested` confirmation page in `src/Barberslop.Web/Pages/Invitation/Request.cshtml` and `src/Barberslop.Web/Pages/Invitation/Requested.cshtml`
- [ ] T032 [US2] Implement `GET /Invitation/Pending` barber review page and `POST /Invitation/Accept` / `POST /Invitation/Reject` handlers (acceptance triggers client notification) in `src/Barberslop.Web/Pages/Invitation/Pending.cshtml` and `src/Barberslop.Web/Pages/Invitation/Pending.cshtml.cs`
- [ ] T033 [US2] Implement `GET /Invitation/Clients` active-client list and `POST /Invitation/Disinvite` handler (records `DisinviteReason`, warns about outstanding appointments) in `src/Barberslop.Web/Pages/Invitation/Clients.cshtml` and `src/Barberslop.Web/Pages/Invitation/Clients.cshtml.cs`
- [ ] T034 [US2] Implement `POST /Invitation/Reinvite` handler (barber-only, requires `Disinvited` status) in `src/Barberslop.Web/Pages/Invitation/Clients.cshtml.cs`
- [ ] T035 [P] [US2] Add FluentValidation validators for invitation forms (`RequestPageModel`, `DisinvitePageModel`) with all error codes from invitation contract in `src/Barberslop.Web/Features/Invitation/InvitationValidators.cs`
- [ ] T036 [US2] Implement client self-registration Razor Page (name, email, phone, E.164 validation, `FR-001`) in `src/Barberslop.Web/Pages/Account/Register.cshtml` and `src/Barberslop.Web/Pages/Account/Register.cshtml.cs`

**Checkpoint**: User Stories 1 AND 2 both independently functional and testable

---

## Phase 5: User Story 3 — Configure Services, Schedules, and Follow-Up Experience (Priority: P3)

**Goal**: Barbers define weekly schedules, vacation periods, temporary blocks, service catalog details, and per-client booking limits; standing appointments supported; clients receive multi-channel reminders and immediate rebooking options on cancellation.

**Independent Test**: Configure service catalog and weekly schedule, add vacation, create a standing appointment, cancel a booking, and confirm reminders are dispatched on all four channels with rebooking options presented.

### Tests for User Story 3

- [ ] T037 [P] [US3] Unit tests for reminder dispatch scheduling (`ReminderSchedulerTests`): correct `ReminderDispatch` rows created per trigger point and lead time in `tests/Barberslop.UnitTests/ReminderSchedulerTests.cs`
- [ ] T038 [P] [US3] Unit tests for `IAvailabilityService` vacation/temporary-block exclusion (schedule conflict edge cases) in `tests/Barberslop.UnitTests/AvailabilityConflictTests.cs`
- [ ] T039 [P] [US3] Integration tests for schedule configuration round-trips (add rule, add vacation, add block, verify slot exclusion) and reminder dispatch hosted service processing in `tests/Barberslop.IntegrationTests/ScheduleAndReminderTests.cs`

### Implementation for User Story 3

- [ ] T040 [US3] Implement `IScheduleService` (add/remove availability rules, vacation periods, temporary blocks) with non-overlap invariant enforcement in `src/Barberslop.Web/Features/Schedule/ScheduleService.cs`
- [ ] T041 [P] [US3] Implement `GET /Schedule/Availability` and `POST /Schedule/Availability/AddRule` / `AddVacation` / `AddBlock` Razor Pages per availability contract in `src/Barberslop.Web/Pages/Schedule/Availability.cshtml` and `src/Barberslop.Web/Pages/Schedule/Availability.cshtml.cs`
- [ ] T042 [US3] Implement `IReminderDispatchService.ScheduleRemindersAsync` to create `ReminderDispatch` rows for all four channels at the configured lead times when an appointment transitions to `Confirmed` in `src/Barberslop.Web/Features/Reminders/ReminderDispatchService.cs`
- [ ] T043 [P] [US3] Implement `SendGridReminderChannel` (HTML email with iCalendar attachment, SendGrid API) in `src/Barberslop.Web/Infrastructure/SendGridReminderChannel.cs`
- [ ] T044 [P] [US3] Implement `TwilioReminderChannel` (plain-text SMS with cancellation short-link, Twilio API) in `src/Barberslop.Web/Infrastructure/TwilioReminderChannel.cs`
- [ ] T045 [P] [US3] Implement `ICalendarReminderChannel` (RFC 5545 `.ics` generation, delivered via email attachment) in `src/Barberslop.Web/Infrastructure/ICalendarReminderChannel.cs`
- [ ] T046 [P] [US3] Implement `PushNotificationReminderChannel` placeholder (logs intent, returns `Success = false` / `FailureReason = "Push not configured"`) in `src/Barberslop.Web/Infrastructure/PushNotificationReminderChannel.cs`
- [ ] T047 [US3] Implement `ReminderDispatchHostedService` (60-second poll, per-channel dispatch, 3-attempt exponential back-off at 30 s / 2 min / 10 min, `Failed` terminal state) in `src/Barberslop.Web/Features/Reminders/ReminderDispatchHostedService.cs`
- [ ] T048 [US3] Add standing appointment support (`isStanding`, `standingRecurrence` RRULE) to `IBookingService.CreateAppointmentAsync` (`FR-016`) in `src/Barberslop.Web/Features/Booking/BookingService.cs`
- [ ] T049 [P] [US3] Add FluentValidation validators for schedule forms (`AddRuleModel`, `AddVacationModel`, `AddBlockModel`) per availability contract validation rules in `src/Barberslop.Web/Features/Schedule/ScheduleValidators.cs`

**Checkpoint**: All three user stories independently functional and testable

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that apply across all user stories

- [ ] T050 [P] Add PostGIS / NetTopologySuite to `BarberDbContext` for `geoLocation` column and configure geographic barber search query in `src/Barberslop.Web/Data/BarberDbContext.cs` and `src/Barberslop.Web/Features/Invitation/DiscoverService.cs`
- [ ] T051 [P] Define and register `RequireBarber` and `RequireClient` authorization policies and apply `[Authorize(Policy = "...")]` to all Razor Pages per contract access rules in `src/Barberslop.Web/Program.cs`
- [ ] T052 [P] Configure external provider secrets (`SendGrid:ApiKey`, `Twilio:AccountSid`, `Twilio:AuthToken`, `Twilio:FromNumber`) via ASP.NET Core user-secrets for local dev and Azure Key Vault reference for production in `src/Barberslop.Web/`
- [ ] T053 [P] Security hardening review: verify antiforgery tokens on all POST handlers, parameterized EF Core queries, contact data scoped to authorized actors, and OWASP Top 10 mitigations across `src/Barberslop.Web/`
- [ ] T054 [P] Add EF Core query indexes for hot paths (barber availability lookup, appointment count per client-barber, pending invitation list) in `src/Barberslop.Web/Data/BarberDbContext.cs`
- [ ] T055 Run coverlet coverage measurement across both test projects and verify ≥ 90% line coverage gate; record results in `specs/001-barber-client-booking/quickstart.md`
- [ ] T056 [P] Verify all contributor-facing scripts and commands (`dotnet run`, `dotnet test`, `dotnet ef migrations add`, `azd up`) work correctly on Windows per `QC-006`
- [ ] T057 Tag initial release as `1.0.0` per semantic versioning convention

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 completion — BLOCKS all user stories
- **User Stories (Phases 3–5)**: All depend on Phase 2 completion
  - Stories can proceed sequentially in priority order (P1 → P2 → P3) or in parallel if staffed
- **Polish (Phase 6)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start immediately after Phase 2 — no dependency on US2 or US3
- **User Story 2 (P2)**: Can start immediately after Phase 2 — no dependency on US1 or US3 (US2 sets up `InvitationRequest.Active` status that US1 guards against, so US1 integration tests may mock it)
- **User Story 3 (P3)**: Can start immediately after Phase 2 — extends US1 schedule features and US2 notification path but is independently testable

### Within Each User Story

- Tests before implementation
- Domain entities (Phase 2) before services
- Services before Razor Page handlers
- Core implementation before cross-story integration

### Parallel Opportunities

- All Phase 1 `[P]` tasks can run simultaneously
- All Phase 2 `[P]` tasks can run simultaneously
- Once Phase 2 is done, all three user stories can be worked by separate developers
- All `[P]` tasks within a story phase can run simultaneously
- All reminder channel implementations (T043–T046) are independent and fully parallel

---

## Parallel Example: User Story 1

```
# Tests (launch all together):
T015 — AvailabilityServiceTests.cs
T016 — BookingLimitPolicyTests.cs
T017 — BookingWorkflowTests.cs

# Services (sequential — service depends on domain):
T018 — AvailabilityService.cs
T019 — BookingService.cs  (depends on T018)

# Pages (parallel after services):
T020 — Book.cshtml / Book.cshtml.cs
T021 — Confirm.cshtml / Confirm.cshtml.cs
T022 — Cancelled.cshtml / Cancelled.cshtml.cs
T023 — Pages/Services/
T024 — BookingValidators.cs
T025 — FamilyMembers.cshtml
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL — blocks all stories)
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: Test User Story 1 independently; demo core booking flow
5. Proceed to Phase 4 (US2) if MVP validation passes

### Incremental Delivery

1. Setup + Foundational → infrastructure ready
2. User Story 1 → test independently → **MVP demo**
3. User Story 2 → test independently → invitation lifecycle added
4. User Story 3 → test independently → schedule, reminders, and rebooking added
5. Polish → coverage gate, security hardening, geographic search, Windows validation

### Parallel Team Strategy

With multiple developers after Phase 2 completes:
- Developer A: User Story 1 (booking engine)
- Developer B: User Story 2 (invitation lifecycle)
- Developer C: User Story 3 (schedule + reminders)

Each story ships independently without breaking the others.

---

## Notes

- `[P]` tasks operate on different files with no dependencies — safe to run in parallel
- `[USn]` label maps each task to a specific user story for traceability
- Each user story phase is independently completable and testable
- Commit after each task or logical group
- Stop at each **Checkpoint** to validate the story independently before moving on
- All paths assume the structure defined in `plan.md`; adjust if scaffolding differs
