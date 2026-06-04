# Contract: Reminder Events

**Feature**: 001-barber-client-booking  
**Phase**: 1 — Design  
**Date**: 2026-06-04

This document describes the internal event/service contracts for the reminder dispatch subsystem. Reminders are delivered on four channels: email, SMS, calendar invite, and mobile push notification.

---

## Reminder Trigger Points

| Trigger | Lead Time | Channels |
|---|---|---|
| Appointment created | Immediately (confirmation) | Email, CalendarInvite |
| 48 hours before appointment | 48 h prior | Email, SMS, Push |
| 24 hours before appointment | 24 h prior | Email, SMS, Push |
| 2 hours before appointment | 2 h prior | SMS, Push |

Lead times are configurable per barber (future scope); defaults are as above.

---

## Internal Service Contract: `IReminderDispatchService`

```csharp
Task ScheduleRemindersAsync(
    Guid appointmentId,
    CancellationToken cancellationToken = default);
```

Called when an `Appointment` transitions to `Confirmed`. Creates one `ReminderDispatch` row per (appointment, channel, scheduledFor) combination.

---

## Internal Service Contract: `IReminderChannel`

Each reminder channel implements:

```csharp
Task<ReminderResult> SendAsync(
    ReminderMessage message,
    CancellationToken cancellationToken = default);
```

### `ReminderMessage`

| Field | Type | Description |
|---|---|---|
| `AppointmentId` | `Guid` | |
| `ClientName` | `string` | |
| `BarberName` | `string` | |
| `ServiceName` | `string` | |
| `AppointmentStartAt` | `DateTimeOffset` | |
| `ClientEmail` | `string` | Used by Email, CalendarInvite channels |
| `ClientPhoneNumber` | `string` | Used by SMS channel |
| `ClientPushToken` | `string?` | Used by Push channel; null if not registered |

### `ReminderResult`

| Field | Type | Description |
|---|---|---|
| `Success` | `bool` | |
| `ExternalMessageId` | `string?` | Provider-assigned ID (e.g., SendGrid Message-ID) |
| `FailureReason` | `string?` | Error detail if `Success == false` |

---

## Channel Implementations

### Email Channel (`SendGridReminderChannel`)

- Provider: SendGrid
- Template: HTML email with barber-pole header graphic, appointment details, and iCalendar attachment link.
- Configuration: `SendGrid:ApiKey` in app secrets.

### SMS Channel (`TwilioReminderChannel`)

- Provider: Twilio
- Message: Plain-text SMS with appointment date/time, barber name, and a cancellation short-link.
- Configuration: `Twilio:AccountSid`, `Twilio:AuthToken`, `Twilio:FromNumber` in app secrets.

### Calendar Invite Channel (`ICalendarReminderChannel`)

- Format: RFC 5545 iCalendar `.ics` file attached to a confirmation email.
- Fields: `DTSTART`, `DTEND`, `SUMMARY` (service + barber name), `DESCRIPTION`, `ORGANIZER` (barber email), `ATTENDEE` (client email).
- Delivered as an email attachment via the email channel adapter (reuses SendGrid send path).

### Push Channel (`PushNotificationReminderChannel`)

- v1 scope: Placeholder implementation that logs dispatch intent; real APNs/FCM integration deferred.
- Contract: Implements `IReminderChannel`; returns `Success = false, FailureReason = "Push not configured"` in v1.
- When `ClientPushToken` is null, the channel is skipped without recording a failure.

---

## Failure Handling

- Each channel dispatches independently.
- A `Failed` `ReminderDispatch` record is written with `failureReason` populated.
- Channel failures are logged at `Warning` level via .NET Aspire OpenTelemetry integration.
- Failed dispatches are eligible for retry by a hosted service up to 3 times with exponential back-off (30 s, 2 min, 10 min).
- After 3 failures, the dispatch is marked `Failed` permanently and an alert is surfaced in the Aspire dashboard.

---

## Background Hosted Service: `ReminderDispatchHostedService`

Runs on a configurable schedule (default: every 60 seconds). Queries for `ReminderDispatch` records where:
- `status == Pending`
- `scheduledFor <= DateTimeOffset.UtcNow`

For each record, invokes the appropriate `IReminderChannel` implementation and updates the record status.

Concurrency: Single-instance per deployment (Aspire Container App replica = 1 for reminder service in v1 to avoid duplicate dispatch).
