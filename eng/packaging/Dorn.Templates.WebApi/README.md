# Dorn Clean Architecture Web API Template

Create a .NET 10 Clean Architecture Minimal API with CQRS, your choice of ORM and database provider, and optional Docker/Aspire orchestration.

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
| `--Auth <none\|custom\|azure-ad>` | `none` | Authentication scheme for the generated API |
| `--Orm <efcore\|dapper>` | `efcore` | The Object-Relational Mapper used for data access |
| `--DatabaseProvider <sqlite\|sqlserver\|postgres>` | `sqlite` | The database provider used for persistence |
| `--Orchestrator <aspire\|docker-compose\|none>` | `aspire` | Local orchestration approach for the generated service |
| `--IncludeTests <bool>` | `true` | Includes the generated test projects |

> [!IMPORTANT]
> `--Auth custom` requires `--Orm efcore`. The custom user store and migrations do not exist under Dapper.

[View source and full documentation](https://github.com/mbarretot/dorn-templates-webapi)
