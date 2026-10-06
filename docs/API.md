# API Specification (REST & SignalR)

## Authentication
All requests (except initial Windows/domain identity negotiation) require:
```http
Authorization: Bearer <JWT>
```

## REST Endpoints (Planned)

### Users
- `GET /api/users/me` — Returns current authenticated user profile and roles.

### Support Requests / Complaints
- `GET /api/support-requests` — List complaints (filtered by role: Customer sees own, CS sees claimable/assigned).
- `GET /api/support-requests/{id}` — Get complaint details and conversation history.
- `POST /api/support-requests` — Submit a new complaint.
- `POST /api/support-requests/{id}/claim` — Atomically claim an unassigned complaint (CS only). Returns `200 OK` on success, `409 Conflict` if already claimed.
- `POST /api/support-requests/{id}/resolve` — Mark complaint as resolved.
- `POST /api/support-requests/{id}/close` — Mark complaint as closed.

### Messages
- `POST /api/support-requests/{id}/messages` — Send a message in a complaint conversation.

## SignalR Hub (`/hubs/support`)
- **Connection Handshake:** Query parameter `?access_token=<JWT>`.
- **Events (Server to Client):**
  - `ComplaintCreated`
  - `ComplaintClaimed`
  - `ComplaintStatusChanged`
  - `MessageReceived`
