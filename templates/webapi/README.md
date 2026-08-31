# CleanArchWebApi

[![Scaffolded with Dorn](https://img.shields.io/badge/scaffolded_with-Dorn-1A1A1A?style=flat-square)](https://github.com/mbarretot/dorn)

A Clean Architecture Minimal API with CQRS and the persistence, authentication, and local runtime choices selected at generation time.

## ⚡ Start here

```bash
dotnet tool restore
dotnet build
dotnet dorn run
```

Then run every generated test tier:

```bash
dotnet dorn test
```

## 🏛️ Project map

| Area | Responsibility |
| --- | --- |
| `Domain` | Entities, aggregates, and domain events |
| `Application` | Commands, queries, handlers, validation, and ports |
| `Infrastructure` | EF Core or Dapper persistence |
| `WebApi` | Minimal API endpoints and composition root |
| `AppHost` and `ServiceDefaults` | Aspire runtime support when selected |

Dependencies point inward; architecture tests enforce the boundary rules.

## ⚙️ Generated choices

| Choice | Default | Values |
| --- | --- | --- |
| ORM | `efcore` | `efcore`, `dapper` |
| Database | `sqlite` | `sqlite`, `sqlserver`, `postgres` |
| Orchestration | `aspire` | `aspire`, `docker-compose`, `none` |
| Authentication | `none` | `none`, `custom`, `azure-ad` |

These choices are fixed when the project is generated. Custom authentication requires EF Core.

## 🧪 Test tiers

| Tier | Verifies |
| --- | --- |
| Application | Handlers, validators, and behaviors |
| Integration | Selected persistence provider |
| Architecture | Layer dependency rules |
| Functional | HTTP request pipeline |

SQLite needs no Docker. SQL Server and PostgreSQL tests start their dependencies with Testcontainers on supported hosts.

## ⌨️ Project CLI

| Command | Action |
| --- | --- |
| `dotnet dorn run` | Auto-detect Aspire, Compose, or plain .NET |
| `dotnet dorn test` | Run every tier |
| `dotnet dorn test --tier <name>` | Run one tier |
| `dotnet dorn coverage` | Test with the 80% coverage gate |

## 🔄 CI

`.github/workflows/ci.yml` runs the generated test matrix on Ubuntu and Windows; container-backed provider tests run on Linux.

## 📚 Details

- [Web API template reference](https://github.com/mbarretot/dorn/blob/main/docs/templates/webapi.md)
- [Dorn architecture decisions](https://github.com/mbarretot/dorn/tree/main/docs/adr)
