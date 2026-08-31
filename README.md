<div align="center">
  <img src="docs/assets/dorn-icon.jpg" alt="Hand-drawn Dorn architectural mark" width="112" />

# Dorn Web API Template

**Production-ready .NET 10 Clean Architecture Web APIs, generated in one command.**

[![.NET 10](https://img.shields.io/badge/.NET-10-b0533a?style=flat-square&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![NuGet](https://img.shields.io/nuget/v/Dorn.Templates.WebApi?style=flat-square&color=b0533a&label=NuGet&logo=nuget&logoColor=white)](https://www.nuget.org/packages/Dorn.Templates.WebApi)
[![Build](https://img.shields.io/github/actions/workflow/status/mbarretot/dorn-templates-webapi/ci.yml?branch=main&style=flat-square&label=build&color=b0533a)](https://github.com/mbarretot/dorn-templates-webapi/actions/workflows/ci.yml)

</div>

Generate a .NET 10 Minimal API with clean boundaries, CQRS, optional authentication, and a local runtime that fits your stack.

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

- Clean Architecture: Domain, Application, Infrastructure, and WebApi
- CQRS commands, queries, and handlers via `Dorn.Messaging`
- EF Core or Dapper with SQLite, SQL Server, or PostgreSQL
- Aspire or Docker Compose, including local observability
- Custom JWT or Microsoft Entra ID token validation
- Application, Integration, Architecture, and Functional xUnit tests

## ⚙️ Template options

| Option | Default | Choices | Effect |
| --- | --- | --- | --- |
| `--Auth` | `none` | `none`, `custom`, `azure-ad` | Authentication |
| `--Orm` | `efcore` | `efcore`, `dapper` | Persistence style |
| `--DatabaseProvider` | `sqlite` | `sqlite`, `sqlserver`, `postgres` | Database |
| `--Orchestrator` | `aspire` | `aspire`, `docker-compose`, `none` | Local runtime |
| `--IncludeTests` | `true` | `bool` | Generated test projects |

> [!IMPORTANT]
> `--Auth custom` requires `--Orm efcore`. The template stops an unsupported combination at build time with an actionable `#error`.

```bash
dotnet new dorn-webapi -n Acme.Orders \
  --Auth custom \
  --DatabaseProvider postgres \
  --Orchestrator docker-compose
```

### 💾 `Orm=dapper` support level

- **SQLite**: schema bootstrap, full Todo CRUD, and all generated test tiers work without external services.
- **SQL Server and PostgreSQL**: generation, build, integration, and functional tests are supported through Testcontainers.
- Expression-based repository methods (`FindAsync`, `AnyAsync`, and `CountAsync`) deliberately throw `NotSupportedException` under Dapper. Add a dedicated repository method for a new query instead of relying on LINQ translation.

## 🛠️ Work on the template

1. Read the [contributor guide](CONTRIBUTING.md).
2. Run the focused tests for the affected template or pack/generate harness.
3. Keep `.template.config/template.json`'s symbols and this README's option table in sync.
