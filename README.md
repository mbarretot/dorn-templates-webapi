<div align="center">
  <img src="docs/assets/dorn-icon.jpg" alt="Hand-drawn Dorn architectural mark" width="112" />

# Dorn Web API Template

**Production-ready .NET 10 Clean Architecture Web APIs, generated in one command.**

[![.NET 10](https://img.shields.io/badge/.NET-10-b0533a?style=flat-square&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![NuGet](https://img.shields.io/nuget/v/Dorn.Templates.WebApi?style=flat-square&color=b0533a&label=NuGet&logo=nuget&logoColor=white)](https://www.nuget.org/packages/Dorn.Templates.WebApi)
[![Build](https://img.shields.io/github/actions/workflow/status/mbarretot/dorn-templates-webapi/ci.yml?branch=main&style=flat-square&label=build&color=b0533a)](https://github.com/mbarretot/dorn-templates-webapi/actions/workflows/ci.yml)

</div>

Production-ready Clean Architecture Web API with CQRS, layered structure, and Docker support.

## 🚀 Quick start

```bash
dotnet new install Dorn.Templates.WebApi
dotnet new dorn-webapi -n Acme.Orders
cd Acme.Orders
dotnet tool restore
dotnet build
dotnet dorn run
```

> [!TIP]
> In scripts, defaults are deterministic: `Auth=none`, `DatabaseProvider=sqlite`, `Orm=efcore`, `Orchestrator=aspire`.

## ✨ Included

- 🧱 Clean Architecture layering: Domain, Application, Infrastructure, WebApi
- 🔁 CQRS via commands, queries, and handlers (`Dorn.Messaging.Contracts` / `Dorn.Messaging`)
- 💾 EF Core or Dapper persistence over SQLite, SQL Server, or PostgreSQL
- ☁️ Optional .NET Aspire orchestration or Docker Compose with local observability (Grafana, Loki, Prometheus, Tempo)
- 🔐 Optional JWT authentication: self-issued custom tokens or Azure AD/Entra ID validation
- 🧪 Application, Integration, Architecture, and Functional xUnit test tiers

## ⚙️ Template options

| Option | Default | Choices | Effect |
| --- | --- | --- | --- |
| `--Auth` | `none` | `none`, `custom`, `azure-ad` | Authentication scheme for the generated API |
| `--Orm` | `efcore` | `efcore`, `dapper` | The Object-Relational Mapper used for data access |
| `--DatabaseProvider` | `sqlite` | `sqlite`, `sqlserver`, `postgres` | The database provider used for persistence |
| `--Orchestrator` | `aspire` | `aspire`, `docker-compose`, `none` | Local orchestration approach for the generated service |
| `--IncludeTests` | `true` | `bool` | Includes the generated test projects |

> [!IMPORTANT]
> `--Auth custom` requires `--Orm efcore`. The custom user store and its migrations do not exist under Dapper — that combination generates but fails to build immediately, with a `#error` pointing at this constraint.

```bash
dotnet new dorn-webapi -n Acme.Orders \
  --Auth custom \
  --DatabaseProvider postgres \
  --Orchestrator docker-compose
```

### 💾 `Orm=dapper` support level

- **`--DatabaseProvider sqlite`**: fully supported — schema bootstrap on startup, full CRUD through `ITodoItemRepository`, and the same Application/Integration/Functional test coverage as EF Core.
- **`--DatabaseProvider sqlserver` / `postgres`**: generates and builds. `Integration.Tests` runs the same Testcontainers-backed round trip as EF Core's `PersistenceTestFixture.cs` (see `DapperTodoItemRepositoryTests.cs`). The HTTP-tier (`Functional.Tests`) is the one gap: it only swaps to a local SQLite file the way the EF Core partial does, and Dapper's connection type is fixed per provider at generation time, so that swap only works for `sqlite` — exercising the full HTTP pipeline against a real SQL Server/PostgreSQL instance needs its own Testcontainers-backed `WebApplicationFactory`, which doesn't exist yet for Dapper.

## 🛠️ Work on the template

1. Read the [contributor guide](CONTRIBUTING.md).
2. Run the focused tests for the files you changed.
3. Keep `.template.config/template.json`'s symbols and this README's option table in sync.
