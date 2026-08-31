# Dorn Clean Architecture Web API Template

Create a .NET 10 Clean Architecture Minimal API with CQRS, your persistence stack, and an optional local runtime.

## 🚀 Create and run

```bash
dotnet new install Dorn.Templates.WebApi
dotnet new dorn-webapi -n Acme.Orders
cd Acme.Orders
dotnet tool restore
dotnet build
dotnet dorn run
```

## ✨ What you get

- Clean Architecture layering: Domain, Application, Infrastructure, WebApi
- CQRS via commands, queries, and handlers
- EF Core or Dapper persistence over SQLite, SQL Server, or PostgreSQL
- Optional Aspire orchestration or Docker Compose with local observability
- Optional JWT authentication: self-issued custom tokens or Azure AD/Entra ID validation
- Application, Integration, Architecture, and Functional xUnit test tiers

## ⚙️ Options

| Option | Default | Effect |
| --- | --- | --- |
| `--Auth <none\|custom\|azure-ad>` | `none` | Authentication |
| `--Orm <efcore\|dapper>` | `efcore` | Persistence style |
| `--DatabaseProvider <sqlite\|sqlserver\|postgres>` | `sqlite` | Database |
| `--Orchestrator <aspire\|docker-compose\|none>` | `aspire` | Local runtime |
| `--IncludeTests <bool>` | `true` | Generated test projects |

> [!IMPORTANT]
> `--Auth custom` requires `--Orm efcore`; unsupported combinations stop at build time with an actionable error.

[View source and full documentation](https://github.com/mbarretot/dorn-templates-webapi)
