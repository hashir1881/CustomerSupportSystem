# Project Notes

## Objective
Build an enterprise Windows desktop Customer Support / Complaint Communication System consisting of two separate WinUI 3 desktop applications communicating through a central ASP.NET Core backend with PostgreSQL and SignalR.

## Development Philosophy
- Zero unexplained AI code.
- Incremental phased development.
- The developer must master every architectural decision, package, and class before implementation.
- Every phase includes verification, failure scenario analysis, security reviews, and interview prep questions.

## Phase Tracking
- [x] **Phase 0 — Environment & Foundational Learning Checkpoint** (Completed: Architectural Mindset, 3-Tier Boundary, 9 JWT/AD Design Decisions, Concurrency Strategy).
- [x] **Phase 1 — Solution Creation & Project Scaffolding** (Completed: Clean Architecture project layout, references, solution build, initial tests).
- [x] **Phase 2 — Database Design & EF Core** (Completed: Domain Entities, Enums, Fluent API, xmin concurrency token, PostgreSQL migrations, CustomerSupportDb created).
- [x] **Phase 3 — Basic REST API** (Completed: Controllers, Application Services, DTOs, Concurrency Claiming, DatabaseSeeder, GlobalExceptionHandler, E2E HTTP tested).
- [x] **Phase 4 — Active Directory Authentication** (Completed: Negotiate/Kerberos middleware, IIdentityService, auto-provisioning, server-enforced role mapping, E2E tested).
- [x] **Phase 5 — JWT Issuance & Validation** (Completed: Dual authentication scheme JwtBearer + Negotiate, HMAC-SHA256 signed JWT generation, ClaimsCurrentUserService, role-based authorization [Authorize(Roles = ...)], SignalR query-string token extraction, 15 unit tests passing, live E2E verified).
- [x] **Phase 6 — Customer Desktop Application** (Completed: WinUI 3 + Windows App SDK 2.5, CommunityToolkit.Mvvm reactive architecture, background Windows auth login, IAuthenticationState, AuthHeaderHandler Bearer injection, typed HttpClients, Master-Detail complaint dashboard, ContentDialog ticket submission, 16 unit tests passing).
- [ ] **Phase 7 — CS Desktop Application**
- [ ] **Phase 8 — Atomic Multi-Agent Claiming**
- [ ] **Phase 9 — SignalR Hub Infrastructure**
- [ ] **Phase 10 — Real-Time Complaint Events**
- [ ] **Phase 11 — Real-Time Chat**
- [ ] **Phase 12 — Custom Floating Notification Bubble**
- [ ] **Phase 13 — System Tray**
- [ ] **Phase 14 — Windows Startup**
- [ ] **Phase 15 — Network Connectivity Detection**
- [ ] **Phase 16 — Offline Request Queue**
- [ ] **Phase 17 — Offline Message Queue**
- [ ] **Phase 18 — Resynchronization Protocol**
- [ ] **Phase 19 — Security Hardening**
- [ ] **Phase 20 — Centralized Error Handling**
- [ ] **Phase 21 — Logging Pipeline**
- [ ] **Phase 22 — Auditing**
- [ ] **Phase 23 — Comprehensive Testing**
- [ ] **Phase 24 — Concurrency Stress Testing**
- [ ] **Phase 25 — Failure Scenario Testing**
- [ ] **Phase 26 — Idempotency & Deduplication**
- [ ] **Phase 27 — Performance Optimization**
- [ ] **Phase 28 — Deployment & Packaging**
- [ ] **Phase 29 — AI Integration (Post-Stability)**
- [ ] **Phase 30 — Final Architecture Review & Interview Prep**
