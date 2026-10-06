# Architecture Decision Records (ADR)

## ADR-001: Centralized 3-Tier Backend over 2-Tier Direct Database Access
* **Date:** 2026-09-22
* **Status:** Accepted
* **Decision:** Desktop applications will communicate exclusively with an ASP.NET Core backend via HTTPS and SignalR, with zero direct database connections.
* **Why:** Desktop apps execute in an untrusted client environment. Direct PostgreSQL access exposes database credentials, exhausts connection pools, prevents atomic business rule enforcement, and renders reliable real-time updates and auditing impossible.
* **Alternatives Considered:** 
  - 2-Tier Client-to-Database via Npgsql: Rejected due to credential theft risks and lack of centralized logic.
  - Peer-to-Peer direct connection: Rejected due to NAT/firewall traversal issues and lack of central audit authority.
* **Trade-offs:** Introduces an additional tier (ASP.NET Core) to host, deploy, and maintain.

---

## ADR-002: Dual Independent WinUI 3 Desktop Applications
* **Date:** 2026-09-22
* **Status:** Accepted
* **Decision:** Build two completely separate desktop projects (`CustomerSupport.CustomerDesktop` and `CustomerSupport.CsDesktop`) rather than a single application with role flags.
* **Why:** Customer Service agents and Customers have completely disjoint workflows, permission models, notification needs, and operational lifecycles. Separate binaries prevent leaking agent-only UI code, administrative tools, or sensitive logic to customer endpoints.
* **Alternatives Considered:** Single desktop binary with role-based feature flags: Rejected due to binary bloat and security risk of client-side feature gating.
* **Trade-offs:** Requires packaging and maintaining two separate desktop executables.

---

## ADR-003: Dual Authentication Model (Active Directory + JWT)
* **Date:** 2026-09-22
* **Status:** Accepted
* **Decision:** Validate Windows/Active Directory identity at backend logon, map to server-determined application roles, and issue a signed short-lived JWT for REST and SignalR communication.
* **Why:** Decouples the application from corporate domain controllers, simplifies SignalR WebSocket authorization, supports future non-domain clients, and prevents domain group changes from blocking application role adjustments.
* **Alternatives Considered:** Pure Windows Integrated Auth (Kerberos/NTLM) across all endpoints: Rejected due to Kerberos WebSocket instability over proxies and tight coupling to enterprise domain configuration.
* **Trade-offs:** Requires secure token storage (DPAPI) and token refresh/re-authentication logic on the desktop clients.
