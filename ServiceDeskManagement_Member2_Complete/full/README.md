# Service Desk Management - Member 2 backend (complete)

Everything for **Member 2 - Ticket Operations** (Modules 4, 5 and 6) plus the shared foundation, as ONE clean solution.
Clean Architecture: `Domain` <- `Application` <- `Infrastructure` / `API`, plus `tests/ServiceDesk.UnitTests`.

## Start from a clean folder
1. Delete your old attempt (all its folders) or unzip this into a NEW folder.
2. Install the .NET SDK 8 or newer and SQL Server LocalDB (comes with Visual Studio) - or change the connection
   string in `src/ServiceDesk.API/appsettings.json`.
3. In PowerShell, inside this folder:
   ```
   ./setup.ps1
   dotnet run --project src/ServiceDesk.API --launch-profile ServiceDesk.API
   ```
   `setup.ps1` detects your SDK version, builds, runs the unit tests, creates the migration and the database.
   Swagger: https://localhost:7100/swagger  (the sample requests are in `ServiceDesk.http`).

If you prefer manual steps: set `TargetFramework` and `MicrosoftPackagesVersion` in `Directory.Build.props`
to your SDK major version, then `dotnet build`, `dotnet test`, and the two `dotnet ef` commands at the bottom of `setup.ps1`.

## Who is "signed in"? (until Member 1's JWT is merged)
`appsettings.json` -> `DevUser` (UserId, EmployeeId, Role). Restart after changing it.
Roles: `Employee`, `ServiceDeskAgent`, `ServiceDeskManager`, `Administrator`. Default: Administrator.

| Role | Can do |
|---|---|
| Employee | create tickets; see/edit/comment on **own** tickets; cancel (New/Open), close a resolved ticket, reopen within the window |
| Agent | see tickets of **their queues** (or assigned to them); status changes, resolve, internal notes; assign **to themselves**; escalate |
| Manager | everything on tickets; manage queues and members; assign/reassign anyone; workload |
| Administrator | everything, plus categories and services |

An agent can only be assigned in a queue they belong to: add members with `POST /api/queues/{id}/members`.

## Endpoints
| Area | Endpoint |
|---|---|
| Categories | GET/POST `/api/categories`, GET/PUT `/{id}`, PATCH `/{id}/deactivate`, `/activate` |
| Services | GET/POST `/api/services` (with `defaultQueueId` routing rule), GET/PUT `/{id}`, PATCH deactivate/activate |
| Queues | GET/POST `/api/queues`, GET/PUT `/{id}`, PATCH deactivate/activate, POST `/{id}/members`, DELETE `/{id}/members/{agentEmployeeId}` |
| Tickets | GET `/api/tickets` (search, status, priority, category, service, queue, assignedToMe, dates, paging), GET `/unassigned`, GET `/{id}`, POST, PUT `/{id}`, POST `/{id}/status`, POST `/{id}/reopen`, GET `/{id}/history` |
| Assignment | POST `/api/tickets/{id}/assign`, `/reassign`, `/escalate` |
| Comments | GET/POST `/api/tickets/{id}/comments` (`isInternal` = staff only) |
| Attachments | GET/POST `/api/tickets/{id}/attachments` (multipart `file`), GET `/{attachmentId}/download`, DELETE `/{attachmentId}` |
| Workload | GET `/api/agents/workload?queueId=` |

Errors use RFC 7807: 400 validation, 401 not signed in, 403 forbidden, 404 not found (also for tickets you may not see),
409 conflict, 422 business rule, 500 generic (no internal details).

## Ticket lifecycle
```
New -> Open -> Assigned -> InProgress <-> WaitingForUser -> Resolved -> Closed
                 Open -> InProgress           Resolved -> Reopened -> InProgress   (inside the reopen window)
New / Open -> Cancelled
```
* Resolving needs a note. Assigning a New ticket accepts it (Open) and then marks it Assigned.
* The requester answering a ticket that is WaitingForUser puts it back to InProgress.
* Escalating to another queue clears the assignee and returns the ticket to Open; escalating to a manager assigns the manager.
* Every change is written to `TicketHistory`; assignments are also kept in `TicketAssignments`.

## Rules worth knowing
* Ticket numbers (`TKT-2026-000123`) come from a SQL Server sequence. Priority defaults to the service's default priority.
* Routing: the service's `defaultQueueId`, else the seeded **General Service Desk** queue (id 1, cannot be deactivated).
* Attachments: pdf, png, jpg, jpeg, gif, txt, log, csv, docx, xlsx; max 5 MB; 10 per ticket; content-type decided by the server,
  first bytes checked against the extension, files stored under `App_Data/attachments` with generated names.
  Removing an attachment is a soft delete.
* `ReopenWindowDays` (default 7) is configurable under `TicketPolicy` in appsettings.

## Hand-over points to the other members
| Contract (`Application/Abstractions`) | Owner | Stub to replace in `Infrastructure/DependencyInjection.cs` |
|---|---|---|
| `ICurrentUserService` | Member 1 | `StubCurrentUserService` (reads `DevUser`) |
| `ISlaService` | Member 3 | `StubSlaService` (returns no deadlines) |
| `INotificationService` (recipient = employee id; types incl. `TicketEscalated`, `CommentAdded`) | Member 3 | `StubNotificationService` (logs) |
| `IAuditService` | Member 4 | `StubAuditService` (logs) |
| `IAssetLinkService` | Member 4 | `StubAssetLinkService` |

After Member 1 adds `Employees` / `Departments`: add foreign keys for `Ticket.RequesterEmployeeId`,
`Ticket.AssignedAgentEmployeeId`, `TicketQueueMember.AgentEmployeeId`, `TicketQueue.DepartmentId` / `ManagerEmployeeId`,
add `[Authorize]` to the controllers, add the ticket **department filter**, and remove `DevUser`.

## Tests
`dotnet test` - workflow table, ticket domain rules (status, reopen, assignment, escalation, comments, attachments, queues),
status permissions per role, validators, attachment rules, ticket visibility per role.
Integration tests against a real database are not included yet (they need a test database).
