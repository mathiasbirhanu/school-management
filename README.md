# Scholaris 🎓

**Student management & online learning platform** — K-12 and College/University, built for
Ethiopia: Tele Birr & friends for payments, AI-powered study aids and practice tests, branded
video delivery, automatic certificates, and a full admin back office.

> Implementation of the delivered SRS (IEEE 830) and SAD (IEEE 1471) documents.

## Stack

| Layer | Technology |
|---|---|
| Frontend | Next.js 14 (App Router) + TypeScript + Tailwind CSS — brand `#3F3D8F` / `#F0A731` |
| Backend | .NET 8 Web API — Clean Architecture (Domain / Application / Infrastructure / Api) |
| Database | PostgreSQL 16 (EF Core + Npgsql) |
| Auth | JWT access (15 min) + rotating refresh tokens, RBAC (Admin / Teacher / Student / Parent) |
| Payments | Strategy/adapter pattern — **Mock providers by default**, real Chapa/Tele Birr integration points ready (ADR-013) |
| AI | Free-tier abstraction — **Gemini / Groq / Ollama**, offline **Mock** engine by default (ADR-012) |
| Deployment | Docker Compose (db + api + web) |

## Quick start

```bash
docker compose up --build
```

| Service | URL |
|---|---|
| Web (Next.js) | http://localhost:3000 |
| API (Swagger) | http://localhost:8080/swagger |
| PostgreSQL | localhost:5432 (scholaris/scholaris) |

Seeded demo accounts:

| Role | Email | Password |
|---|---|---|
| Admin | `admin@scholaris.et` | `Admin@123` |
| Teacher | `sara.tesfaye@scholaris.et` | `Teacher@123` |
| Teacher | `daniel.bekele@scholaris.et` | `Teacher@123` |
| Student | `abi@student.scholaris.et` | `Student@123` |

The seed ships 6 courses (Grade 8 → University Year 1, two of them discounted), historical paid
orders across recent months, and the default 22% commission rate — so every dashboard is alive
on first run.

## Feature tour (where to look)

| Requirement | Implementation |
|---|---|
| Courses added / priced / deleted by **admin** | Admin office → Courses (`/admin`) |
| **Income (commission) rate adjusted by admin** | Admin office → Income rate (default 22%, snapshotted per order) |
| **Course discount adjusted by admin** | Admin office → Courses → Edit → `Discount %` |
| **Balloon announcement** of discounted courses | Floating balloon bottom-right on every page; click → discounted list |
| **Discount rate tag on course cards** | Gold `-X%` pill on card covers, detail page, balloon list |
| **YouTube de-branded player** | `BrandedVideoPlayer.tsx` — custom controls, Scholaris logo top-left, watermark bottom-right, `controls=0 modestbranding rel=0 iv_load_policy=3`, right-click disabled, pause-on-blur |
| **AI exams per topic** (fresh per student & attempt, **never saved**) | `POST /api/ai-exam/generate` → answer → `POST /api/ai-exam/evaluate`; papers live only in `IMemoryCache` (90 min) and are destroyed on grading |
| **AI summaries / elaboration of teacher notes** | `POST /api/ai/notes/summarize` (ephemeral) |
| Ask the teacher | Q&A threads + teacher inbox on the Teacher desk |
| **Certificates awarded at course end, design/content edited by admin** | Auto-issued on last lesson completion; Admin office → Certificate (live preview, placeholders `{{StudentName}} {{CourseTitle}} {{Date}} {{Serial}}`); public verification `/certificate/{serial}` |
| **22% commission ledger + teacher income by month + CSV** | Admin office → Revenue (+ CSV), Teacher desk → income + CSV |
| Payment method chosen by the payer | Checkout page selector (Tele Birr / CBE Birr / M-Pesa / card via Chapa — all mock until keys are set) |
| Paid-access gating | Every learn/AI endpoint returns **HTTP 402 + pricing** for non-enrolled students |

## Configuration

### AI (backend only — keys never reach the browser)
`backend/src/Scholaris.Api/appsettings.json` → `"Ai"`:

```jsonc
"Ai": {
  "Provider": "Mock",                  // Mock | Gemini | Groq | Ollama
  "Gemini": { "ApiKey": "", "Model": "gemini-1.5-flash" },
  "Groq":   { "ApiKey": "", "Model": "llama-3.1-8b-instant" },
  "Ollama": { "BaseUrl": "http://localhost:11434", "Model": "llama3.1" }
}
```
With `Mock` (default) the whole product works offline with deterministic engines — swap in a
free Gemini key (Google AI Studio) or Groq key for real generations. The abstraction is
`IAiExamService` / `IAiNotesService`; add providers in `Scholaris.Infrastructure/Ai`.

### Payments (mock first, real API later)
`"Payments": { "Mode": "Mock" | "Live", "Chapa": { "ApiKey": "" } }`

To go live with **Chapa** (aggregates Tele Birr, CBE Birr, cards, banks):
1. Create a merchant account at chapa.co, get the secret key.
2. Set `Payments:Mode=Live`, `Payments:Chapa:ApiKey=<key>`, `Payments:PublicBaseUrl=<your web url>`.
3. Point the Chapa dashboard webhook to `POST https://<api>/api/payments/webhook/chapa`.
4. Complete signature verification in the webhook handler (marked integration point) and use
   `ChapaProvider.VerifyAsync(txRef)` before granting access.

Direct **Tele Birr** (Ethio Telecom) integration follows the same adapter seam:
implement `IPaymentProvider` and register it in `PaymentProviderResolver`.

### Security notes
- Change `Jwt:Secret` in production (min 32 chars) and restrict `Cors:Origins` to your domain.
- Video protection is the agreed three-layer code approach (deterrents + detection hooks +
  branding) — no web page can make screen-recording impossible; private unlisted videos plus
  these measures raise the bar (documented in the SAD).

## Project structure

```
scholaris/
├── backend/
│   ├── Scholaris.sln
│   └── src/
│       ├── Scholaris.Domain/         entities, enums, exceptions
│       ├── Scholaris.Application/    DTOs, interfaces (IPaymentProvider, IAiExamService…), services
│       ├── Scholaris.Infrastructure/ EF Core/Npgsql, JWT, mock/live payments, AI clients, seeding
│       └── Scholaris.Api/            controllers, middleware, Program.cs
├── frontend/                         Next.js 14 app (pages under app/, components/)
├── docs/API_CONTRACT.md              full REST contract
└── docker-compose.yml
```

## v1 engineering notes
- **Schema creation**: `EnsureCreated()` on startup (no migration files yet). For production,
  switch to `dotnet ef migrations add Init` + `Database.Migrate()`.
- **Course content editing**: admin UI covers course metadata, price, discount, publish and
  delete; full curriculum editing (modules/topics/lessons) is available via
  `POST /api/admin/courses` with the nested payload (see `docs/API_CONTRACT.md`) — wire it into
  the UI next sprint.
- **AI exams are transient by design** — verify `ai_exams` never appears in your database.
- The branded player hides all YouTube chrome; Google's embed ToS still applies to the stream
  itself (see SAD ADR-011 for the trade-off discussion).
# school-management
