# System Architecture

## Overview

The Customer Support System uses a **Clean Architecture (Onion Architecture)** approach for the backend services and **MVVM** for the dual desktop clients.

```
                         WINDOWS DOMAIN
                               │
                 ┌─────────────┴─────────────┐
                 │                           │
          Customer User                CS Agent
                 │                           │
                 ▼                           ▼
       ┌──────────────────┐        ┌──────────────────┐
       │ Customer Desktop │        │ CS Desktop       │
       │ WinUI 3          │        │ WinUI 3          │
       │                  │        │                  │
       │ MVVM             │        │ MVVM             │
       │ REST Client      │        │ REST Client      │
       │ SignalR Client   │        │ SignalR Client   │
       │ Notifications    │        │ Notifications    │
       │ Offline Queue    │        │ Connectivity     │
       └────────┬─────────┘        └────────┬─────────┘
                │                           │
                │ HTTPS + JWT               │ HTTPS + JWT
                │ WSS (SignalR)             │ WSS (SignalR)
                └─────────────┬─────────────┘
                              │
                              ▼
                    ┌────────────────────┐
                    │  ASP.NET Core API  │
                    │                    │
                    │ Controllers        │
                    │ SignalR Hubs       │
                    │ Auth Middleware    │
                    └─────────┬──────────┘
                              │
                              ▼
                    ┌────────────────────┐
                    │    Application     │
                    │                    │
                    │ Use Cases (CQRS)   │
                    │ DTOs, Interfaces   │
                    └─────────┬──────────┘
                              │
                              ▼
                    ┌────────────────────┐
                    │       Domain       │
                    │                    │
                    │ Entities, Enums    │
                    │ Domain Rules       │
                    └────────────────────┘
                              ▲
                              │
                    ┌─────────┴──────────┐
                    │   Infrastructure   │
                    │                    │
                    │ EF Core, Postgres  │
                    │ Npgsql, AD Auth    │
                    └─────────┬──────────┘
                              │
                              ▼
                    ┌────────────────────┐
                    │    PostgreSQL      │
                    │                    │
                    │ Users, Requests    │
                    │ Messages, Audit    │
                    └────────────────────┘
```

## Dependency Inversion Principle
1. **CustomerSupport.Domain**: Depends on nothing. Pure business logic.
2. **CustomerSupport.Application**: Depends only on Domain. Defines interfaces for persistence and external services.
3. **CustomerSupport.Infrastructure**: Implements interfaces from Application using EF Core and PostgreSQL.
4. **CustomerSupport.Api**: Composition root. Configures dependency injection and exposes HTTP/SignalR endpoints.
5. **CustomerDesktop & CsDesktop**: Two completely separate WinUI 3 client binaries. CS agent UI and customer UI do not share application binaries.
