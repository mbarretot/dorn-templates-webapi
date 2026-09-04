# Changelog

All notable changes to the `Dorn.Templates.WebApi` package are documented here.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project uses the version pushed as a `v<version>` tag (see [CONTRIBUTING.md](CONTRIBUTING.md#-releases)).

## [Unreleased]

### Added

- `Auth=custom` now issues and rotates refresh tokens: `POST /auth/login` returns a `refreshToken` alongside the access token, and `POST /auth/refresh` exchanges it for a new access/refresh pair. The server persists only a SHA-256 hash of the refresh token; presenting a token that was already rotated away (a stolen/replayed token) revokes the user's entire refresh-token chain as a compromise signal.
- Permission-based authorization on the Todo endpoints: `GET` requires `todos:read`, `POST`/`PUT`/`PATCH` require `todos:write`, and `DELETE` requires `todos:delete`. Policies are registered whenever `Auth` is enabled (`custom` or `azure-ad`), backed by a `PermissionAuthorizationHandler` that checks a `permission` claim. `Auth=custom` also gains an `AppUser.Permissions` column and `JwtTokenService` now emits one `permission` claim per granted permission; the seeded demo user is granted all three. `Auth=azure-ad` enforces the same policies but has no seeding story of its own -- Entra ID (via App Roles or a claims-mapping policy) must be configured to emit a matching `permission` claim.

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
