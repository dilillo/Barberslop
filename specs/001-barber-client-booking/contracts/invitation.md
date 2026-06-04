# Contract: Invitation Lifecycle

**Feature**: 001-barber-client-booking  
**Phase**: 1 — Design  
**Date**: 2026-06-04

This document describes the UI page contracts for the client-barber invitation lifecycle: discovery, request, acceptance, and disinvitation.

---

## Pages

### `GET /Invitation/Discover`

Lets a client search for barbers to request joining their clientele.

**Access**: `RequireClient`

**Query Parameters**:

| Parameter | Type | Required | Description |
|---|---|---|---|
| `salonName` | `string` | No | Partial match on `BarberProfile.salonName` |
| `barberName` | `string` | No | Partial match on `BarberProfile.displayName` |
| `nearLatitude` | `double` | No | Geographic center latitude |
| `nearLongitude` | `double` | No | Geographic center longitude |
| `radiusKm` | `double` | No | Default 10 km |

**Rendered Model** (`DiscoverPageModel`):

| Property | Type | Notes |
|---|---|---|
| `Results` | `IReadOnlyList<BarberSearchResult>` | id, displayName, salonName, distanceKm |
| `SearchFilters` | `SearchFilters` | Echoed back to re-render the form |

---

### `POST /Invitation/Request`

Submits a request to join a barber's clientele (FR-004).

**Access**: `RequireClient`

**Form Fields**:

| Field | Type | Required | Validation |
|---|---|---|---|
| `BarberProfileId` | `Guid` | Yes | Must be a valid barber |
| `ClientName` | `string` | Yes | max 100 |
| `Email` | `string` | Yes | Valid RFC 5322 |
| `PhoneNumber` | `string` | Yes | E.164 format |

**Success Response**: Redirect to `GET /Invitation/Requested?barberId={id}`

**Validation Errors**:

| Code | Description |
|---|---|
| `ALREADY_PENDING` | An active or pending invitation already exists |
| `DISINVITED` | Client was previously disinvited; only barber can re-invite (FR-006) |
| `INVALID_BARBER` | Barber not found |
| `INVALID_EMAIL` | Email format invalid |
| `INVALID_PHONE` | Phone number format invalid |

---

### `GET /Invitation/Requested`

Confirmation page shown after a client submits an invitation request.

**Access**: `RequireClient`

**Query Parameters**:

| Parameter | Type | Required | Description |
|---|---|---|---|
| `barberId` | `Guid` | Yes | Barber receiving the request |

**Rendered Model** (`RequestedPageModel`):

| Property | Type | Notes |
|---|---|---|
| `BarberName` | `string` | |
| `StatusMessage` | `string` | "Your request has been sent. You'll be notified when accepted." |

---

### `GET /Invitation/Pending`

Lists pending client requests for a barber to review.

**Access**: `RequireBarber`

**Rendered Model** (`PendingPageModel`):

| Property | Type | Notes |
|---|---|---|
| `PendingRequests` | `IReadOnlyList<InvitationRequestSummary>` | id, clientName, email, phone, requestedAt |

---

### `POST /Invitation/Accept`

Barber accepts a pending invitation request (FR-005: triggers notification to client).

**Access**: `RequireBarber`

**Form Fields**:

| Field | Type | Required | Validation |
|---|---|---|---|
| `InvitationRequestId` | `Guid` | Yes | Must be Pending, owned by this barber |

**Success Response**: Redirect to `GET /Invitation/Pending` with success banner.

**Side Effect**: Sends acceptance notification to client (queued `ReminderDispatch`-equivalent notification).

**Validation Errors**:

| Code | Description |
|---|---|
| `NOT_PENDING` | Request is not in Pending status |
| `UNAUTHORIZED` | Request does not belong to this barber |

---

### `POST /Invitation/Reject`

Barber rejects a pending invitation request.

**Access**: `RequireBarber`

**Form Fields**:

| Field | Type | Required | Validation |
|---|---|---|---|
| `InvitationRequestId` | `Guid` | Yes | Must be Pending |

**Success Response**: Redirect to `GET /Invitation/Pending`.

---

### `POST /Invitation/Disinvite`

Barber removes a client from their clientele (FR-019).

**Access**: `RequireBarber`

**Form Fields**:

| Field | Type | Required | Validation |
|---|---|---|---|
| `InvitationRequestId` | `Guid` | Yes | Must be Active |
| `DisinviteReason` | `DisinviteReason` (enum) | Yes | RepeatedNoShow, LostContact, Other |

**Success Response**: Redirect to `GET /Invitation/Clients`.

**Side Effect**: Any future `Confirmed` appointments for the disinvited client with this barber must be resolved (displayed as a warning on the confirmation page, with links to cancel individually).

**Validation Errors**:

| Code | Description |
|---|---|
| `NOT_ACTIVE` | Invitation is not in Active status |
| `UNAUTHORIZED` | Invitation does not belong to this barber |
| `REASON_REQUIRED` | DisinviteReason is required |

---

### `GET /Invitation/Clients`

Lists all active clients for a barber.

**Access**: `RequireBarber`

**Rendered Model** (`ClientsPageModel`):

| Property | Type | Notes |
|---|---|---|
| `ActiveClients` | `IReadOnlyList<ClientSummary>` | id, displayName, email, phone, acceptedAt |

---

### `POST /Invitation/Reinvite`

Barber re-invites a previously disinvited client (FR-007).

**Access**: `RequireBarber`

**Form Fields**:

| Field | Type | Required | Validation |
|---|---|---|---|
| `ClientProfileId` | `Guid` | Yes | Must be a Disinvited client of this barber |

**Success Response**: Redirect to `GET /Invitation/Clients`.

**Validation Errors**:

| Code | Description |
|---|---|
| `NOT_DISINVITED` | Client is not in Disinvited status with this barber |
| `UNAUTHORIZED` | Client-barber relationship does not belong to this barber |
