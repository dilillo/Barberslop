# Contract: Availability Query

**Feature**: 001-barber-client-booking  
**Phase**: 1 — Design  
**Date**: 2026-06-04

This document describes the UI page and internal service contracts for the availability engine — the component that computes bookable time slots.

---

## Pages

### `GET /Schedule/Availability`

Renders the availability configuration page for a barber.

**Access**: `RequireBarber`

**Rendered Model** (`AvailabilityPageModel`):

| Property | Type | Notes |
|---|---|---|
| `WeeklyRules` | `IReadOnlyList<AvailabilityRuleSummary>` | dayOfWeek, startTime, endTime, effectiveFrom, effectiveTo |
| `VacationPeriods` | `IReadOnlyList<VacationPeriodSummary>` | startDate, endDate, reason |
| `TemporaryBlocks` | `IReadOnlyList<TemporaryUnavailabilitySummary>` | startAt, endAt, reason |
| `TimeZoneId` | `string` | Barber's configured timezone |

---

### `POST /Schedule/Availability/AddRule`

Adds a weekly recurring availability rule.

**Access**: `RequireBarber`

**Form Fields**:

| Field | Type | Required | Validation |
|---|---|---|---|
| `DayOfWeek` | `int` (0–6) | Yes | Sunday=0 |
| `StartTime` | `TimeOnly` | Yes | HH:mm format |
| `EndTime` | `TimeOnly` | Yes | > StartTime |
| `EffectiveFrom` | `DateOnly` | Yes | ≥ today |
| `EffectiveTo` | `DateOnly` | No | null = indefinite |
| `TimeZoneId` | `string` | Yes | IANA timezone |

**Validation Errors**:

| Code | Description |
|---|---|
| `INVALID_TIME_RANGE` | EndTime ≤ StartTime |
| `OVERLAPPING_RULE` | Overlaps an existing rule for same day and effective period |
| `INVALID_TIMEZONE` | TimeZoneId is not a recognized IANA timezone |

---

### `POST /Schedule/Availability/AddVacation`

Blocks a vacation period.

**Access**: `RequireBarber`

**Form Fields**:

| Field | Type | Required | Validation |
|---|---|---|---|
| `StartDate` | `DateOnly` | Yes | ≥ today |
| `EndDate` | `DateOnly` | Yes | ≥ StartDate |
| `Reason` | `string` | No | max 200 |

---

### `POST /Schedule/Availability/AddBlock`

Adds a temporary unavailability window.

**Access**: `RequireBarber`

**Form Fields**:

| Field | Type | Required | Validation |
|---|---|---|---|
| `StartAt` | `DateTimeOffset` | Yes | In future |
| `EndAt` | `DateTimeOffset` | Yes | > StartAt |
| `Reason` | `string` | No | max 200 |

---

## Internal Service Contract: `IAvailabilityService`

The availability engine is consumed internally by the booking and schedule pages. It is not exposed as an HTTP API endpoint — it is an in-process service boundary.

### `GetAvailableSlotsAsync`

```csharp
Task<IReadOnlyList<TimeSlot>> GetAvailableSlotsAsync(
    Guid barberProfileId,
    Guid serviceOfferingId,
    DateOnly date,
    CancellationToken cancellationToken = default);
```

**Returns**: Ordered list of `TimeSlot` records (startAt, endAt) representing bookable windows for the specified date.

**Algorithm summary**:
1. Load `AvailabilityRule` records for `barberProfileId` matching `date.DayOfWeek` and effective date range.
2. Load `VacationPeriod` records overlapping `date`.
3. Load `TemporaryUnavailability` records overlapping `date`.
4. Load confirmed/pending `Appointment` records for `barberProfileId` on `date`.
5. Build free windows by subtracting all blocked intervals from availability rule windows.
6. Filter free windows where duration ≥ service `durationMinutes`.
7. Return ordered `TimeSlot` list.

**Performance contract**: Returns in < 200 ms for a single-day query under normal load (target for p95 ≤ 2 s total page response including DB I/O and render).

---

### `GetFirstAvailableSlotAsync`

```csharp
Task<TimeSlot?> GetFirstAvailableSlotAsync(
    Guid barberProfileId,
    Guid serviceOfferingId,
    DateTimeOffset searchFrom,
    int searchHorizonDays = 90,
    CancellationToken cancellationToken = default);
```

**Returns**: The earliest `TimeSlot` at or after `searchFrom`, or `null` if no slot found within `searchHorizonDays`.

---

### `TimeSlot` Record

```csharp
record TimeSlot(DateTimeOffset StartAt, DateTimeOffset EndAt);
```
