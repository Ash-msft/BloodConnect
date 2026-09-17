# BloodConnect

BloodConnect is an opt-in, privacy-first employee blood donor network designed to run as a
Microsoft Teams personal tab. Employees can privately register as donors, colleagues can raise
blood requests for patients in need, and the system matches compatible, available, eligible
donors — revealing donor identity to the requester **only** after the donor affirms availability.

> **Medical disclaimer**: BloodConnect's eligibility guidance (e.g. the 56-day minimum interval
> between whole-blood donations) is **informational only**. It does not replace clinical
> screening. The blood donation center's on-site medical assessment and local regulations always
> take precedence over anything shown in this application.

---

## 1. Architecture

```
BloodConnect/
├── src/
│   ├── BloodConnect.Domain/         # Entities, enums, compatibility + eligibility business rules (no dependencies)
│   ├── BloodConnect.Infrastructure/ # EF Core DbContext, migrations, matching engine, notification outbox/channels, demo seeder
│   └── BloodConnect.Api/            # ASP.NET Core Web API: controllers, demo auth, DTOs, Swagger, Program.cs composition root
├── tests/
│   └── BloodConnect.Tests/          # xUnit: domain unit tests + WebApplicationFactory integration tests
├── client/                          # React + TypeScript + Vite SPA (Teams personal tab UI)
├── teams-app/                       # Teams app manifest, generated icons, packaging script
├── Dockerfile.api, Dockerfile.client, docker-compose.yml
└── BloodConnect.slnx                # Visual Studio solution (opens all 4 .NET projects)
```

**Backend**: ASP.NET Core on .NET 10, EF Core with SQLite for local development. The
`ConnectionStrings:Default` value and `DbContext` configuration are provider-agnostic enough that
swapping to Azure SQL is a configuration change (see [§8](#8-azure--production-configuration)), not
a code change — `UseSqlite` is the only provider-specific line, isolated in `Program.cs`.

**Frontend**: React 19 + TypeScript, built with Vite. Uses `@microsoft/teams-js` to detect and
initialize the Teams host context (theme, user context) when running inside Teams, with a full
browser-local fallback when it isn't (e.g. `npm run dev`).

**Notifications**: a durable outbox (`NotificationOutboxItem` table) records every notification
BloodConnect *intends* to deliver, independent of delivery channel. Two channel implementations:
- `Local` (default): notifications are observable via `/api/notifications` and the in-app inbox.
- `TeamsWebhook`: posts a real Adaptive Card to a configured Microsoft Teams Incoming Webhook URL.
  This is an **honest adapter** — if `BloodConnect:Teams:Enabled` is `false` or no webhook URL is
  configured, it does not pretend to succeed; it marks the outbox record `Failed` with a specific
  `DeliveryDetail` explaining why.

## 2. Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/) (developed/tested with Node 24, npm 11)
- On Windows, use `npm.cmd` explicitly if `npm.ps1` is blocked by your execution policy.
- Optional: Docker Desktop (for the container path), Visual Studio 2022+ (open `BloodConnect.slnx`).

## 3. Running locally (without Docker)

### Backend API

```powershell
cd src\BloodConnect.Api
dotnet run --urls http://localhost:5030
```

On startup in the `Development` environment, the API automatically:
1. Applies EF Core migrations (creates `bloodconnect.db` SQLite file).
2. Seeds 8 realistic demo users/donor profiles and one sample blood request (`DemoDataSeeder`) —
   **seeding only ever runs in `Development`**, never in `Production`.

Swagger UI is available at `http://localhost:5030/swagger` in Development.

### Frontend SPA

```powershell
cd client
copy .env.example .env
npm.cmd install
npm.cmd run dev
```

Open `http://localhost:5173`. Use the demo user switcher in the top nav to act as different
employees (see [§4](#4-demo-accounts--local-auth) below).

## 4. Demo accounts & local auth

There is **no password-based login**. Instead, a `DemoAuthenticationHandler` reads an
`X-Demo-User` HTTP header sent by the frontend and validates it against a **fixed, server-side
allow-list** of demo external ids — the browser cannot invent an identity; only the 8 seeded ids
below are ever accepted, and the server always resolves the authoritative `AppUser` from that
validated claim, never from any other client-supplied field:

| External id   | Name          | Seeded blood group | City       |
|----------------|---------------|---------------------|------------|
| `demo-priya`   | Priya Sharma  | O+                  | Bengaluru  |
| `demo-arjun`   | Arjun Mehta   | O+                  | Bengaluru  |
| `demo-fatima`  | Fatima Khan   | A+                  | Hyderabad  |
| `demo-liam`    | Liam O'Connor | B+                  | Dublin     |
| `demo-wei`     | Wei Zhang     | AB+                 | Singapore  |
| `demo-sofia`   | Sofia Rossi   | O-                  | Milan      |
| `demo-noah`    | Noah Williams | A-                  | Bengaluru  |
| `demo-ana`     | Ana Costa     | B-                  | Lisbon     |

Fetch the full, current list (with emails/opt-in status) any time via `GET /api/demo-users`.

### ⚠️ Local demo auth limitations — read before any real deployment

This mechanism exists **only** to make the app runnable and demoable without an identity
provider. It is **not** secure or production-appropriate:
- Any caller who can set an HTTP header can "become" any of the 8 fixed demo users — there is no
  password, token signature, or session. It is safe *only* because the set of identities is a
  small fixed allow-list with no real personal data, meant for local/dev demonstration.
- It must be replaced with real authentication (Entra ID / Azure AD, see below) before this API is
  exposed to any network beyond a trusted local machine.

## 5. Replacing demo auth with Microsoft Entra ID

The demo handler is intentionally isolated behind the standard ASP.NET Core authentication
abstraction so it's a drop-in replacement:

1. Register two Entra ID app registrations (API + SPA), or use Teams SSO with a single
   registration exposing an API scope.
2. In `src/BloodConnect.Api/Program.cs`, replace the
   `builder.Services.AddAuthentication(...).AddScheme<DemoAuthenticationOptions, DemoAuthenticationHandler>(...)`
   registration with `AddMicrosoftIdentityWebApi(...)` (via
   `Microsoft.Identity.Web`), configured from an `AzureAd` section (`Instance`, `TenantId`,
   `ClientId`, `Audience`).
3. `CurrentUserService` already resolves identity purely from `ClaimTypes.NameIdentifier` /
   `ClaimTypes.Email` — with Entra ID, populate `AppUser.ExternalId` from the token's `oid` claim
   during first-login provisioning instead of from the demo allow-list, and no controller code
   needs to change.
4. In the frontend, replace `X-Demo-User` header injection (`src/context/AuthContext.tsx`) with
   `@microsoft/teams-js` `authentication.getAuthToken()` (Teams SSO) or MSAL.js, and attach the
   resulting bearer token as an `Authorization` header in `src/api/client.ts`.
5. Update `teams-app/manifest.json`'s `webApplicationInfo.id` / `resource` with the real Entra ID
   application (client) id and API scope URI.

## 6. API overview

All endpoints (except `/api/demo-users`) require the `X-Demo-User` header and return `401` if
missing/unrecognized. Full interactive docs at `/swagger` in Development.

| Method & route                              | Purpose |
|----------------------------------------------|---------|
| `GET /api/demo-users`                        | List selectable local demo identities (no auth required) |
| `GET /api/me`                                 | Resolve the current authenticated user + whether they have a donor profile |
| `GET /api/profile`                            | Get the caller's **own** donor profile (private; 404 if none) |
| `PUT /api/profile`                            | Create/update the caller's own donor profile |
| `GET /api/donations`                          | List the caller's own donation history |
| `POST /api/donations`                         | Record a completed donation (updates last-donation date) |
| `POST /api/requests`                          | Create a blood request; triggers donor matching + notification fan-out |
| `GET /api/requests`                           | List blood requests **created by the caller** |
| `GET /api/requests/{id}`                      | Get one request the caller owns |
| `GET /api/requests/{id}/responses`            | List matched-donor responses — donor identity is `null` unless that donor responded `Available` |
| `POST /api/requests/{id}/cancel`              | Cancel the caller's own open request |
| `POST /api/requests/{id}/fulfill`             | Mark the caller's own request fulfilled |
| `GET /api/matches`                            | List requests **matched to the caller as a donor**, with the caller's own response status |
| `POST /api/matches/{responseId}/respond`      | Respond `Available`/`NotAvailable` to a match — only the matched donor may respond for themself |
| `GET /api/notifications`                      | List the caller's own notification outbox items |
| `POST /api/notifications/{id}/read`           | Mark a notification read |

**Never exposed**: there is no endpoint that lists all donor profiles or all users' requests —
every list/detail endpoint is scoped to "my own" or "matched to me" data.

## 7. Privacy & security decisions

- **Ownership is always server-resolved.** `ICurrentUserService`/`CurrentUserService` derives the
  caller's `AppUser` strictly from the authenticated claim set by `DemoAuthenticationHandler` (or,
  post-Entra-ID-migration, the validated JWT). No controller trusts any user id present in a
  request body/query string as authoritative.
- **Matched-donor identity reveal is conditional.** In `RequestsController.GetResponses`, a
  donor's name/email/phone/contact preference are populated in the response DTO **only when**
  `DonorResponse.Status == ResponseStatus.Available`. Donors who haven't responded, or who
  responded `NotAvailable`, are represented with those fields `null` — verified in
  `RequestWorkflowTests.MatchedDonor_Identity_OnlyRevealed_AfterAvailableResponse`.
- **Donors can only act for themselves.** `MatchesController.Respond` checks that the response's
  `DonorProfile.UserId` equals the current user's id, returning `403 Forbidden` otherwise —
  verified in `RequestWorkflowTests.Donor_Cannot_Respond_On_Behalf_Of_Another_Donor`. The full
  matching engine and disclosure logic were also exercised manually via curl during development.
- **Outbox pattern for notifications.** Every notification is durably persisted before dispatch is
  attempted, so a delivery failure never means "the system silently forgot to notify someone" —
  it's recorded as a `Failed` outbox row with a `DeliveryDetail` explanation, visible via
  `/api/notifications`.
- **UTC everywhere.** All persisted timestamps (`CreatedUtc`, `ExpiresUtc`, `LastDonationUtc`,
  etc.) are UTC `DateTime`s; the frontend formats them for display, never treating them as local
  time before formatting.
- **Explicit validation & errors, no silent failures.** Input validation throws a typed
  `ValidationException` (→ `400`), ownership violations throw `ForbiddenException` (→ `403`),
  missing resources throw `NotFoundApiException` (→ `404`) — all mapped centrally and explicitly
  by `ApiExceptionHandler`. There are no empty/broad `catch` blocks that swallow errors silently.
- **Cancellation tokens** are threaded through all async controller actions and EF Core calls so
  requests can be cooperatively cancelled (e.g. on client navigation/unmount).

## 8. Azure / production configuration

No secrets are committed. Configuration is environment/`appsettings`-driven:

| Setting | Local default | Production guidance |
|---|---|---|
| `ConnectionStrings:Default` | `Data Source=bloodconnect.db` (SQLite file) | Azure SQL connection string, injected via App Service configuration / Key Vault reference — swap `UseSqlite` → `UseSqlServer` in `Program.cs` |
| `BloodConnect:MinimumDonationIntervalDays` | `56` | Tune per organizational/medical policy |
| `BloodConnect:RequestExpiryHours` | `72` | Tune per operational policy |
| `BloodConnect:Teams:Enabled` / `IncomingWebhookUrl` | `false` / empty | Set `true` + a real Teams Incoming Webhook (or replace `TeamsWebhookNotificationChannel` with a bot/Graph-based sender) via App Service app settings, not source control |
| `Cors:AllowedOrigins` | `http://localhost:5173` | Your deployed SPA origin(s) |
| Auth | Demo header scheme | Entra ID (see §5) |

## 9. Teams app packaging & sideloading

```powershell
cd teams-app
node generate-icons.js          # regenerate color.png (192x192) / outline.png (32x32) if needed
powershell -ExecutionPolicy Bypass -File .\package.ps1   # produces BloodConnect.teams.zip
```

Before packaging for a real tenant, edit `teams-app/manifest.json`:
- Replace `id` and `webApplicationInfo.id`/`resource` with a real Entra ID app registration id.
- Replace every `REPLACE_WITH_YOUR_APP_HOST` with your deployed frontend's hostname.
- Update `developer`, `privacyUrl`, `termsOfUseUrl` with your organization's real values.

**Sideload**: in Microsoft Teams, go to **Apps → Manage your apps → Upload an app → Upload a
custom app**, and select `BloodConnect.teams.zip`. (Sideloading/custom app upload must be enabled
for your tenant by an admin.)

## 10. Running with Docker

```powershell
docker compose up --build
```

This builds and runs the API (port `8080`, SQLite persisted in a named volume, seeded with demo
data because `ASPNETCORE_ENVIRONMENT=Development` in `docker-compose.yml`) and the frontend
(served by nginx on port `5173`, calling the API at `http://localhost:8080`).

> **Limitation**: Docker Desktop's daemon was not running in the environment this project was
> built in, so `docker compose up` could not be executed end-to-end here. The Dockerfiles were
> reviewed carefully against the actual project/reference layout (multi-stage restore using each
> `.csproj`, matching relative paths, correct `ENTRYPOINT`/`EXPOSE`), but you should run
> `docker compose up --build` yourself before relying on it.

## 11. Tests

```powershell
# Backend: 50 tests (domain unit tests + WebApplicationFactory integration tests)
dotnet test

# Frontend: 13 tests (vitest)
cd client
npm.cmd run test -- --run

# Frontend lint
npm.cmd run lint

# Frontend production build
npm.cmd run build
```

Backend test coverage highlights (`tests/BloodConnect.Tests`):
- `Domain/BloodCompatibilityTests.cs` — full ABO/Rh donor→recipient compatibility matrix.
- `Domain/BloodEligibilityCalculatorTests.cs` — boundary conditions around the configurable
  minimum-donation-interval (default 56 days).
- `Integration/RequestWorkflowTests.cs` — unauthenticated access, unknown demo identity rejection,
  server-resolved profile ownership, cross-user request access denial, donor identity
  reveal-only-after-`Available`, donor-cannot-respond-for-another-donor, request validation, and
  cancel-twice rejection.

## 12. Known limitations

- **Local demo authentication is not secure** — see §4. It must be replaced before any real
  deployment (see §5).
- **SQLite is for local development only**; production should use Azure SQL (config change only,
  see §8).
- **The Teams webhook notification channel requires a real Incoming Webhook URL** to actually
  deliver to Teams; without one configured, notifications remain visible only via the in-app
  inbox/API (this is by design — an honest adapter, not a fake success).
- **Docker Compose was not executed end-to-end** in this environment (daemon unavailable) — see
  §10.
- **No LICENSE file is included.** Adding one would require asserting an organizational or
  personal copyright holder, which wasn't specified — add one appropriate to your organization
  before distributing this code externally.
- Proximity ranking is a simple same-city-first heuristic (`ProximityRank` in
  `DonorMatchingService`), not real geo-distance — sufficient for an employee network scoped to a
  handful of office cities, but not a general geo-matching engine.

## 13. Production hardening checklist

- [ ] Replace demo authentication with Entra ID (§5) and remove `DemoAuthenticationHandler` from
      the DI pipeline entirely (or gate it strictly behind `Development`).
- [ ] Move all secrets (Teams webhook URLs, Azure SQL connection strings, Entra ID client
      secrets/certificates) into Azure Key Vault / App Service application settings — never
      `appsettings.json`.
- [ ] Switch EF Core provider to `UseSqlServer` against Azure SQL; add a migrations deployment step
      (e.g. `dotnet ef database update` in CI/CD, or migration-on-startup gated by environment).
- [ ] Enforce HTTPS-only, HSTS, and restrict CORS `AllowedOrigins` to the exact production SPA
      origin(s).
- [ ] Add rate limiting / throttling to request-creation and response endpoints to reduce abuse
      and notification spam potential.
- [ ] Add structured logging + telemetry (Application Insights) around matching, notification
      dispatch failures, and authorization denials.
- [ ] Replace the Incoming Webhook Teams channel with a proper Bot Framework / Graph API sender if
      you need per-user @mentions, message updates, or Adaptive Card action callbacks handled
      server-side.
- [ ] Add automated data-retention/erasure handling for donor profile opt-out (GDPR-style "right
      to be forgotten") beyond the current opt-in/opt-out toggle.
- [ ] Review and tighten Content-Security-Policy / frame-ancestors headers for Teams iframe
      hosting.
- [ ] Add integration tests against a real SQL Server/Azure SQL instance in CI, not just SQLite.
