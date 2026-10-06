# Interview Preparation Notes

## Phase 0 & Phase 1 Core Questions

### 1. Architecture & Design Principles
* **Q: Why should desktop applications never connect directly to a production database?**
  * **A:** Desktop binaries run on untrusted client machines. Connection strings can easily be extracted by decompilation or memory inspection. Direct connections bypass business rules, cause database connection exhaustion, make audit logging unreliable, and eliminate server-side authorization checks.
* **Q: What is Clean Architecture and why does `Domain` have zero dependencies?**
  * **A:** Clean Architecture organizes code in concentric layers where dependencies only point inward. `Domain` represents enterprise business rules, entities, and enums. It must never depend on database frameworks (EF Core), presentation frameworks (WinUI), or serialization libraries. This guarantees business rules remain pure, testable, and insulated from technological shifts.
* **Q: What is the purpose of the `Application` layer?**
  * **A:** It orchestrates use cases (e.g., `ClaimComplaintHandler`, `CreateTicketCommand`). It coordinates domain entities and defines persistence interfaces (`ISupportRequestRepository`) without knowing how those interfaces are implemented.

### 2. Desktop & Real-Time
* **Q: Why WinUI 3 over WPF or Electron for this system?**
  * **A:** WinUI 3 is Microsoft's modern native UI framework providing native Windows 11/10 Fluent design, low resource overhead compared to Chromium-based Electron, and first-party support for Windows App SDK features (system tray, native toast notifications, taskbar integrations).
* **Q: Why do we use SignalR instead of polling?**
  * **A:** Polling creates continuous wasteful HTTP traffic and latency. SignalR maintains a persistent full-duplex WebSocket connection, allowing the server to push ticket claim updates and messages instantly to connected agents within milliseconds.

---

## Phase 4 & Phase 5 Core Questions: Authentication, Active Directory & JWT

### 1. Active Directory Integration
* **Q: How does Windows Integrated Authentication (Negotiate) work in ASP.NET Core?**
  * **A:** Negotiate uses SPNEGO to negotiate between Kerberos and NTLM. When a client requests a protected endpoint without credentials, the server responds with `401 Unauthorized` and a `WWW-Authenticate: Negotiate` header. The Windows client obtains a Kerberos ticket from the Active Directory Key Distribution Center (KDC) and sends it in the `Authorization: Negotiate <ticket>` header. The server validates the ticket and establishes a `WindowsIdentity`.

* **Q: Why should client desktop apps NEVER self-assign or claim their own role?**
  * **A:** Client-side role claims can be easily manipulated via memory editing, proxy interception (Fiddler/Burp), or modified binaries. The server MUST determine the role authoritatively from the database based on the verified Windows domain identity.

### 2. Dual Authentication Scheme (Negotiate + JwtBearer)
* **Q: Why don't we use Windows Authentication for every single REST API and SignalR call?**
  * **A:** Windows authentication is connection-oriented and has significant handshake overhead. In microservice or scalable architectures, maintaining Kerberos state is heavy. By issuing a signed JWT upon an initial Windows login, all subsequent REST requests and SignalR connections are completely stateless, fast, and cross-platform.

* **Q: How do we handle JWT authentication in SignalR WebSockets?**
  * **A:** WebSockets in browser/desktop clients cannot pass custom headers (`Authorization: Bearer <token>`) during the initial HTTP upgrade handshake. Therefore, SignalR passes the token as a query parameter `?access_token=...`. On the ASP.NET Core server, we hook into `JwtBearerEvents.OnMessageReceived`, check if the path starts with `/hubs/`, and extract the token from the query string into `context.Token`.

* **Q: What is ClockSkew in TokenValidationParameters and why tighten it?**
  * **A:** `ClockSkew` accounts for time differences between the token issuer and consumer servers. ASP.NET Core defaults to 5 minutes of clock skew, meaning a token set to expire in 5 minutes might actually remain valid for 10 minutes. In high-security systems, we tighten `ClockSkew` to 30 seconds or `TimeSpan.Zero` to ensure prompt expiration.

---

## Phase 6 Core Questions: WinUI 3, MVVM & Desktop Client Architecture

### 1. MVVM & Source Generators
* **Q: Why use `CommunityToolkit.Mvvm` instead of writing custom `INotifyPropertyChanged` boilerplate?**
  * **A:** `CommunityToolkit.Mvvm` uses Roslyn C# source generators to generate property changed notifications and command bindings at compile time. This eliminates reflection, minimizes memory allocations, reduces boilerplate code by 80%, and guarantees compile-time type safety.

* **Q: Why `{x:Bind}` instead of classic WPF `{Binding}` in WinUI 3?**
  * **A:** Classic `{Binding}` relies on runtime reflection, making it slower and susceptible to silent runtime errors if property names change. `{x:Bind}` generates strongly-typed C# code behind the scenes during compilation (`MarkupCompilePass1`), providing high rendering performance and immediate compile-time errors for misspelled properties or type mismatches.

### 2. Client Security & Network Architecture
* **Q: How does `AuthHeaderHandler` (`DelegatingHandler`) work in desktop applications?**
  * **A:** `DelegatingHandler` acts as client-side HTTP middleware. By registering `AuthHeaderHandler` into the `HttpClient` pipeline via `Microsoft.Extensions.Http`, every outgoing request automatically checks the in-memory `IAuthenticationState` and injects `Authorization: Bearer <token>` without any repetitive boilerplate in ViewModels or API clients.

* **Q: How does the desktop client perform silent Windows login?**
  * **A:** The client configures `HttpClientHandler.UseDefaultCredentials = true`. When calling `POST /api/auth/windows-login`, Windows automatically passes the active OS Kerberos/NTLM ticket via SPNEGO. The server authenticates the Windows identity, maps the enterprise role from PostgreSQL, and returns an application JWT.


