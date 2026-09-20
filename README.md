# Claims API

Insurance claims handling. Covers are taken out on vessels and claims are filed against them.
Claims and covers live in MongoDB; an audit trail of every create and delete lives in SQL Server.

## Live demo

**<https://claims-api.runasp.net/swagger>**

A deployed instance you can call directly. Health of its two databases:
**<https://claims-api.runasp.net/health>**

A couple of things worth knowing:

- It runs on free hosting and **sleeps when idle**, so the first request after a quiet spell can
  take up to a minute. Subsequent calls are fast.
- The data is a shared sandbox with no authentication — treat anything in it as disposable.
- Worth trying: `POST /Covers` then `POST /Claims` against the id it returns. A `damageCost` over
  100,000, or a `created` date outside the cover period, will come back as a 400 problem document.

## Running it

Needs the .NET 9 SDK and a running Docker daemon.

```bash
dotnet run --project Claims.Api
```

Then open <https://localhost:7052/swagger>.

In `Development` the API starts throwaway SQL Server and MongoDB containers and wires itself to
them, so there is nothing to install. **Those containers are disposable — data does not survive a
restart.**

To run against real databases, set `UseDevelopmentContainers=false` and supply
`ConnectionStrings__AuditDatabase`, `ConnectionStrings__ClaimsDatabase` and `MongoDb__DatabaseName`.
Nothing else changes, which is what makes it deployable. The audit schema is applied from EF Core
migrations on startup, and `EnableSwagger=true` exposes the API documentation on a hosted instance.

The live demo above is that same build: `dotnet publish` output on IIS, with MongoDB Atlas for
claims and covers and a hosted SQL Server for the audit trail. No source change is needed to move
between the two — only configuration.

## Tests

All tests live in `Claims.Tests`, split into `Unit/` and `Integration/`. The integration tests carry
a `Category=Integration` trait, so the fast suite can still be run on its own without Docker.

```bash
dotnet test                                    # all 179
dotnet test --filter "Category!=Integration"   # 138 unit tests, no Docker
dotnet test --filter "Category=Integration"    # 41 integration tests, needs Docker
```

Coverage is 98.9% of lines (migrations excluded). To reproduce:

```bash
dotnet test --collect:"XPlat Code Coverage" --settings coverlet.runsettings
```

## Endpoints

| Method | Route | Purpose |
| --- | --- | --- |
| `GET` | `/Claims` · `/Claims/{id}` | List or fetch claims |
| `POST` | `/Claims` | File a claim against an existing cover |
| `DELETE` | `/Claims/{id}` | Delete a claim |
| `GET` | `/Covers` · `/Covers/{id}` | List or fetch covers |
| `GET` | `/Covers/compute` | Price a cover without taking it out |
| `POST` | `/Covers` | Take out a cover (premium computed server-side) |
| `DELETE` | `/Covers/{id}` | Delete a cover |
| `GET` | `/health` | Readiness — checks both databases |
| `GET` | `/health/live` | Liveness — process only, no dependencies |

## Structure

Four projects, each referencing only the ones below it, so the dependency direction is enforced by
the compiler rather than by convention:

```
Claims.Api             controllers, startup, HTTP concerns
      ↓
Claims.Infrastructure  EF Core contexts, repositories, audit writer
      ↓
Claims.Application     commands, queries, handlers, DTOs, port interfaces
      ↓
Claims.Domain          entities, premium calculation, Result
```

**Domain** references nothing — no EF Core, no MongoDB driver, no ASP.NET. Entities have private
setters and are built through factories, so an instance cannot exist in an invalid shape.

**Application** has one command or query per operation with a single handler, and declares the
interfaces it needs from the outside world. Queries are thin; the commands carry the orchestration.

**Infrastructure** implements those interfaces. MongoDB collection and element names live in EF
entity configurations here, which is why the domain entities carry no `[BsonElement]` attributes.

**Api** is a thin edge: controllers dispatch through MediatR and translate the result into a status
code.

### Validation

Input rules are FluentValidation validators run by a MediatR pipeline behaviour, so a new command
cannot forget to be validated. The cross-entity rule — a claim's date must fall inside its cover's
period — lives in `CreateClaimCommandHandler` instead, because the handler has already loaded that
cover and a validator would fetch it a second time.

### Errors

Failures the caller is expected to handle return a `Result` carrying `Success`, `NotFound` or
`Invalid`; `ResultExtensions` is the single place those become status codes. Everything else is
thrown and caught by `GlobalExceptionHandler`, which logs the detail and returns an opaque RFC 7807
response with a `traceId`. Both paths produce the same error shape.

### Health

`/health` is the readiness probe: it pings MongoDB and opens a connection to SQL Server, returns
503 when either is unreachable, and names the failing check in the body. `/health/live` is the
liveness probe and deliberately checks nothing, so a database outage takes the instance out of
rotation rather than having the orchestrator restart a healthy process.

### Auditing

`POST` and `DELETE` are audited without the request waiting for the write. `QueuedAuditService` puts
an entry on a bounded channel and returns; `AuditWriterService` drains it in batches on a background
thread and hands each batch to an `IAuditStore`, draining whatever is left on shutdown. The store is a
separate seam so the writer owns queueing while the store owns persistence, and so tests can substitute
it and signal deterministically rather than sleeping.

This is in-memory, so entries queued but not yet written are lost if the process is killed. In
production I would use a transactional outbox — write the audit row in the same transaction as the
business data and have a relay forward it — which gives at-least-once delivery without a distributed
transaction.

## On the size of this solution

This is heavier than two CRUD entities need, deliberately: the task asked for layering and SOLID, and
this is what that looks like applied properly. What it bought in practice:

- Handlers are unit-testable against mocked repositories, with no database and no containers.
- Validation attaches in one place rather than being repeated per endpoint.
- Moving auditing from synchronous to queued changed one registration line.

For a codebase that genuinely stayed this size, vertical slices organised by feature would fit better
than four projects, and I would not reach for CQRS at all.

## Decisions worth flagging

- **Dates are `DateOnly`.** Cover periods and claim dates are calendar dates, not instants. As
  `DateTime` they shifted by the server's UTC offset, which would break the date rules at boundaries.
  A value converter maps them to UTC midnight for storage.
- **The insurance period is inclusive of both dates**, so a single-day cover costs one day's premium.
  The specification does not say; this matches the one-year rule and `Cover.CoversDate`.
- **The third premium band is cumulative** — "discounted by an additional 3%" reads as 5% + 3% = 8%
  for a yacht, 2% + 1% = 3% for other types.
- **A cover's type is persisted under the element name `claimType`.** Odd, but it is what the original
  schema used and changing it would orphan existing documents.
- **Audit timestamps are UTC** and taken when the operation happens, not when the row is written.

## Bugs fixed along the way

Beyond the listed tasks:

- `CreatedAtAction` could not resolve action names ending in `Async`, so every `POST` returned 500
  after persisting the row.
- A new `MongoClient` was created per request, which exhausted EF Core's internal service-provider
  cache after twenty requests and broke the API under load.
- Audit entries already dequeued were abandoned on shutdown because the write honoured the stopping
  token.
- Empty connection-string placeholders in `appsettings.json` satisfied the startup guard, so a
  misconfigured deployment failed at the first query instead of at startup.
