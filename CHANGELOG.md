# Changelog

All notable changes to the `Dorn.Templates.WebApi` package are documented here.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project uses the version pushed as a `v<version>` tag (see [CONTRIBUTING.md](CONTRIBUTING.md#-releases)).

## [Unreleased]

### Added

- `ConnectionString` template parameter (`string`, default empty). Empty keeps the per-provider default; a value replaces it in `appsettings.json` for SQLite, SQL Server, and PostgreSQL, and is wired into the `AppHost` (`AddConnectionString`, no database container) and the Docker Compose service environment (bundled database service and volume omitted). The value is escaped for JSON and Compose, and the test tiers keep their own SQLite file or Testcontainers database. The value is stored in clear text: keep secrets in user-secrets or environment variables.
- `IncludeSample` template parameter (`bool`, default `true`). With `false`, the Todo feature (entity, event, handlers, endpoints, repository, persistence mapping, migrations or schema bootstrap, and its tests) is left out while every layer and all four test tiers remain: the Integration tier opens a connection to the real database, the Functional tier smoke-tests the OpenAPI document, and the Application and Architecture tiers keep their sample-independent tests. `Auth=custom` is not supported without the sample and fails the build of the `Domain` project with an actionable `#error`.
- `IncludeAgentRules` template parameter (`bool`, default `false`). When true, the solution root gets an `AGENTS.md` describing its layers, conventions, commands, and test tiers, plus a `CLAUDE.md` that imports it. The content follows the selected `Orm`, `DatabaseProvider`, `Orchestrator`, `Auth`, and `IncludeTests` options and never mentions what was not generated.
- `Auth=custom` now issues and rotates refresh tokens: `POST /auth/login` returns a `refreshToken` alongside the access token, and `POST /auth/refresh` exchanges it for a new access/refresh pair. The server persists only a SHA-256 hash of the refresh token; presenting a token that was already rotated away (a stolen/replayed token) revokes the user's entire refresh-token chain as a compromise signal.
- Permission-based authorization on the Todo endpoints: `GET` requires `todos:read`, `POST`/`PUT`/`PATCH` require `todos:write`, and `DELETE` requires `todos:delete`. Policies are registered whenever `Auth` is enabled (`custom` or `azure-ad`), backed by a `PermissionAuthorizationHandler` that checks a `permission` claim. `Auth=custom` also gains an `AppUser.Permissions` column and `JwtTokenService` now emits one `permission` claim per granted permission; the seeded demo user is granted all three. `Auth=azure-ad` enforces the same policies but has no seeding story of its own -- Entra ID (via App Roles or a claims-mapping policy) must be configured to emit a matching `permission` claim.

- `IncludeLocalization`, `DefaultLanguage`, and `Languages` template parameters (`bool` default `false`, `string` default `en`, `string` default empty). With `IncludeLocalization=true` the API resolves the culture from `Accept-Language` (request localization, `Content-Language` echoed, fallback to the default culture), reads localizable messages through `IStringLocalizer<SharedResource>` (validation problem title and common error titles), and follows the request culture in FluentValidation messages. `Languages` is a comma-separated list of culture codes, including custom ones such as `gl`; each code except `en` gets a `SharedResource.<code>.resx` starter copy next to the neutral English `SharedResource.resx`, and the `Localization` section of `appsettings.json` holds the default and supported cultures. Works with both ORMs and every database, orchestrator, and `Auth` choice; without the parameter the generated solution is unchanged. Invalid or duplicated codes, or more than 24, fail the build of the `Domain` project with an actionable `#error`.

### Fixed

- A solution name with a dot (for example `Acme.Orders`, the usual way to name a .NET solution) generated an Aspire `AppHost` that did not compile: it referenced `Projects.Acme.Orders_WebApi`, but Aspire names that type `Projects.Acme_Orders_WebApi`. The reference now replaces every non-identifier character of the name with an underscore.
- `IncludeTests=false` removed the `tests` folder but left the four test projects in `<name>.slnx`, so `dotnet restore` and `dotnet build` on the generated solution failed with "project file was not found". The solution files now list the test projects only when `IncludeTests` is true.

## [1.1.0]

### Fixed

- Dapper repository row mapping discarded the persisted `Id` and `IsComplete` values instead of reusing them.
- `Orm=dapper` failed to build with the default `IncludeTests=true`: several test projects referenced EF Core-only types with no `#if` guard.
- `Orm=dapper` had no schema bootstrap, so a freshly generated project's first request failed with "no such table."
- `Auth=custom` combined with `Orm=dapper` generated successfully but failed at runtime with a confusing DI error; it now fails the build immediately with an actionable message.
- The Todo handlers depended on `IApplicationDbContext` (an EF Core-only abstraction) instead of the ORM-agnostic `ITodoItemRepository`, making the Dapper repository unreachable regardless of the ORM selected.

### Added

- `GET /api/todos/{id}`, `PUT /api/todos/{id}`, `PATCH /api/todos/{id}/complete`, and `DELETE /api/todos/{id}` — the sample Todo feature previously only supported create and list.
- A baseline `/health` endpoint for `Orchestrator=docker-compose` and `Orchestrator=none` (previously only `Orchestrator=aspire` had one, via `ServiceDefaults`).
- `Integration.Tests` coverage for the Dapper repository against SQL Server and PostgreSQL via Testcontainers, matching EF Core's existing `PersistenceTestFixture.cs` pattern.

### Testing

- Test coverage locking in the documented `NotSupportedException` contract for Dapper's `FindAsync`/`AnyAsync`/`CountAsync`.

## [1.0.5] and earlier

Not tracked in this file. See the [GitHub releases](https://github.com/mbarretot/dorn-templates-webapi/releases) and tags for that history.
