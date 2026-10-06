# Desktop Customer Support System

A multi-application enterprise complaint management system built with **WinUI 3**, **.NET 10**, **PostgreSQL**, **SignalR**, **Active Directory**, and **JWT**.

## Solution Structure

```
CustomerSupportSystem/
│
├── src/
│   ├── CustomerSupport.Domain/            # Enterprise Business Rules (Entities, Enums, Zero Deps)
│   ├── CustomerSupport.Application/       # Use Cases, Interfaces, DTOs, Business Orchestration
│   ├── CustomerSupport.Infrastructure/    # EF Core, PostgreSQL Persistence, Identity Adapters
│   ├── CustomerSupport.Api/               # ASP.NET Core Web API, Controllers, SignalR Hubs, JWT Auth
│   ├── CustomerSupport.CustomerDesktop/   # WinUI 3 Desktop App for Customers
│   └── CustomerSupport.CsDesktop/         # WinUI 3 Desktop App for Customer Service Agents
│
├── tests/
│   ├── CustomerSupport.Domain.Tests/      # Domain unit tests (Business rules, state transitions)
│   ├── CustomerSupport.Application.Tests/ # Application unit tests (Use cases, validators)
│   └── CustomerSupport.Api.Tests/         # API integration and concurrency tests
│
├── docs/                                  # Architectural documentation and interview prep
│   ├── PROJECT_NOTES.md
│   ├── ARCHITECTURE.md
│   ├── DECISIONS.md
│   ├── INTERVIEW_NOTES.md
│   └── API.md
│
└── CustomerSupportSystem.slnx             # Solution configuration
```

## Quick Start

### Prerequisites
- .NET 10 SDK
- Windows App SDK (WinUI 3 workload)
- PostgreSQL 16+

### Build the Solution
```bash
dotnet build
```

### Run Tests
```bash
dotnet test
```
