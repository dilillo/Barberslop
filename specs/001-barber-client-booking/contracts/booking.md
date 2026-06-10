# Contract: Booking Workflow

**Feature**: 001-barber-client-booking  
**Phase**: 1 — Design  
**Date**: 2026-06-04

This document describes the UI page contracts for the booking workflow: creating, confirming, and cancelling appointments.

---

## Pages

### `GET /Booking/Book`

Initiates the appointment booking flow for a client or barber.

**Access**: `RequireClient` or `RequireBarber`

**Query Parameters**:

| Parameter | Type | Required | Description |
|---|---|---|---|
| `barberId` | `Guid` | Yes (if client) | Target barber |
| `serviceId` | `Guid` | No | Pre-selected service |
| `date` | `DateOnly` | No | Pre-selected date; if omitted, shows calendar |
| `firstAvailable` | `bool` | No | If true, finds first available slot |

**Rendered Model** (`BookPageModel`):

| Property | Type | Notes |
|---|---|---|
| `BarberName` | `string` | |
| `Services` | `IReadOnlyList<ServiceOption>` | id, name, durationMinutes, price |
| `AvailableSlots` | `IReadOnlyList<TimeSlot>` | startAt, endAt; empty = no availability |
| `FamilyMembers` | `IReadOnlyList<FamilyMemberOption>` | id, displayName; includes "Myself" |
| `ValidationErrors` | `IReadOnlyList<string>` | Shown if prior submission failed |

---

### `POST /Booking/Book`

Submits a booking request.

**Access**: `RequireClient` or `RequireBarber`

**Form Fields**:

| Field | Type | Required | Validation |
|---|---|---|---|
| `BarberProfileId` | `Guid` | Yes | Must be valid, active barber |
| `ServiceOfferingId` | `Guid` | Yes | Must be active service of barber |
| `SlotStartAt` | `DateTimeOffset` | Yes | Must be a valid available slot |
| `BookForId` | `Guid` | Yes | ClientProfile.id or FamilyMember.id |
| `IsFirstAvailable` | `bool` | No | If true, SlotStartAt is computed by engine |

**Success Response**: Redirect to `GET /Booking/Confirm?appointmentId={id}`

**Error Response**: Re-render `GET /Booking/Book` with `ValidationErrors` populated.

**Validation Errors**:

| Code | Description |
|---|---|
| `SLOT_UNAVAILABLE` | Slot no longer available (concurrent booking) |
| `LIMIT_EXCEEDED` | Client has reached per-barber future booking limit |
| `NOT_ACTIVE_CLIENT` | Client does not have an active invitation with barber |
| `INVALID_SERVICE` | Service is inactive or does not belong to barber |
| `INVALID_FAMILY_MEMBER` | FamilyMember does not belong to booking client |

---

### `GET /Booking/Confirm`

Displays a booking confirmation summary.

**Access**: `RequireClient` or `RequireBarber`

**Query Parameters**:

| Parameter | Type | Required | Description |
|---|---|---|---|
| `appointmentId` | `Guid` | Yes | Newly created appointment |

**Rendered Model** (`ConfirmPageModel`):

| Property | Type | Notes |
|---|---|---|
| `AppointmentId` | `Guid` | |
| `BarberName` | `string` | |
| `ServiceName` | `string` | |
| `StartAt` | `DateTimeOffset` | |
| `EndAt` | `DateTimeOffset` | |
| `BookedFor` | `string` | Client name or family member name |
| `ReminderChannels` | `IReadOnlyList<string>` | Channels registered for reminders |

---

### `POST /Booking/Cancel`

Cancels an existing appointment.

**Access**: `RequireClient` or `RequireBarber` (must own or manage the appointment)

**Form Fields**:

| Field | Type | Required | Validation |
|---|---|---|---|
| `AppointmentId` | `Guid` | Yes | Must be a Confirmed appointment |
| `CancellationReason` | `string` | No | max 300 chars |

**Success Response**: Redirect to `GET /Booking/Cancelled?appointmentId={id}` which renders rebooking options (FR-018).

**Validation Errors**:

| Code | Description |
|---|---|
| `APPOINTMENT_NOT_FOUND` | Appointment does not exist or not accessible |
| `ALREADY_CANCELLED` | Appointment is already cancelled |
| `UNAUTHORIZED` | Actor does not have permission to cancel this appointment |

---

### `GET /Booking/Cancelled`

Displays cancellation confirmation and immediate rebooking options.

**Query Parameters**:

| Parameter | Type | Required | Description |
|---|---|---|---|
| `appointmentId` | `Guid` | Yes | Cancelled appointment |

**Rendered Model** (`CancelledPageModel`):

| Property | Type | Notes |
|---|---|---|
| `CancelledAppointment` | `AppointmentSummary` | |
| `RebookingOptions` | `IReadOnlyList<TimeSlot>` | Next 3 available slots for same service (FR-018) |
| `FirstAvailableUrl` | `string` | Link to Book page pre-set to firstAvailable=true |
