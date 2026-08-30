# Changelog

All notable changes to the `Dorn.Templates.WebApi` package are documented here.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project uses the version pushed as a `v<version>` tag (see [CONTRIBUTING.md](CONTRIBUTING.md#-releases)).

## [Unreleased]

### Fixed

- Dapper repository row mapping discarded the persisted `Id` and `IsComplete` values instead of reusing them.
- `Orm=dapper` failed to build with the default `IncludeTests=true`: several test projects referenced EF Core-only types with no `#if` guard.
- `Orm=dapper` had no schema bootstrap, so a freshly generated project's first request failed with "no such table."
- `Auth=custom` combined with `Orm=dapper` generated successfully but failed at runtime with a confusing DI error; it now fails the build immediately with an actionable message.
- The Todo handlers depended on `IApplicationDbContext` (an EF Core-only abstraction) instead of the ORM-agnostic `ITodoItemRepository`, making the Dapper repository unreachable regardless of the ORM selected.

### Added

- `GET /api/todos/{id}`, `PUT /api/todos/{id}`, `PATCH /api/todos/{id}/complete`, and `DELETE /api/todos/{id}` — the sample Todo feature previously only supported create and list.
- A baseline `/health` endpoint for `Orchestrator=docker-compose` and `Orchestrator=none` (previously only `Orchestrator=aspire` had one, via `ServiceDefaults`).

## [1.0.5] and earlier

Not tracked in this file. See the [GitHub releases](https://github.com/mbarretot/dorn-templates-webapi/releases) and tags for that history.
