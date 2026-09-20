# Contributing

Thanks for improving the Dorn Web API template. Keep each change focused, tested, and aligned with the generated experience.

---

## 🔁 Development loop

1. Create a focused branch.
2. Follow **RED → GREEN → REFACTOR** for new logic.
3. Format touched files with CSharpier.
4. Run the relevant suites below.

```bash
dotnet test templates/webapi/CleanArchWebApi.slnx
dotnet test tests/Dorn.Templates.WebApi.Tests/Dorn.Templates.WebApi.Tests.csproj
```

> [!IMPORTANT]
> Run test projects separately. The template's own tiers and the pack/generate suite share the global `dotnet new` store and can race when executed together.

---

## 🧭 Where to change things

| Area | Source of truth | Also verify |
| --- | --- | --- |
| Domain entities & events | `src/CleanArchWebApi.Domain` | Application ports and Infrastructure implementations |
| Commands, queries, handlers | `src/CleanArchWebApi.Application` | Validators and pipeline behaviors |
| Todo sample feature | The `Todos` folders, `TodoItem`, `TodoEndpoints`, migrations | The `IncludeSample` exclusions in template.json; shared files keep `#if (IncludeSample)` blocks, and the replacement tests are wrapped in `#if (!IncludeSample)` |
| Persistence (EF Core / Dapper) | `src/CleanArchWebApi.Infrastructure` | `Orm` and `DatabaseProvider` template.json exclude/rename rules |
| Endpoints & composition root | `src/CleanArchWebApi.WebApi` | `Auth` template.json exclude rules |
| Auth (`custom`, `azure-ad`) | `Extensions/AuthenticationExtensions.cs`, `Endpoints/{Auth,Me}Endpoints.cs` | `custom` requires `Orm=efcore`; enforced by a `#error` guard in `src/CleanArchWebApi.Domain/TemplateConstraints.cs` (the template engine has no declarative cross-parameter constraint) |
| Orchestration (`aspire`, `docker-compose`, `none`) | `src/CleanArchWebApi.AppHost`, `src/CleanArchWebApi.ServiceDefaults`, `docker-compose*.yml` | Observability wiring stays equivalent across all three |
| Localization (`IncludeLocalization`) | `Extensions/LocalizationExtensions.cs`, `Localization/SharedResource.*`, `tests/.../Functional.Tests/LocalizationTests.cs` | The `LanguageFileNN` slot symbols and their exclusions in template.json: the engine cannot loop, so each `Languages` entry maps to one fixed slot file (24 max) |
| Blob storage (`BlobStorage`) | `Application/Common/Storage`, `Infrastructure/Storage`, `Persistence/Migrations/*/*_AddBlobs*`, `DapperContext.InitializeSchemaAsync`, the `Storage` tests in three tiers | The EF Core `AddBlobs` migrations and their snapshot blocks are `dotnet ef` output wrapped in `#if`; regenerate them per provider when `BlobRecord` changes, and keep `manual` byte-identical |
| Template parameters | `.template.config/template.json` | Root and package README option tables, `TemplateParameterMetadataTests` |
| Generated CI workflow | `.github/workflows/ci.yml` (inside the template) | `tests/Dorn.Templates.WebApi.Tests` structural assertions |
| Shared package versions | Template-local `Directory.Packages.props` | Package references remain intentional |

> [!NOTE]
> `Data Source=app.db` in the template's `appsettings.json` and the `__DORN_CONNECTION_STRING_*__` markers are replacement tokens of the `ConnectionString` parameter. Do not reuse those literals elsewhere in the template: every occurrence is rewritten at generation time.

<details>
<summary><strong>Generation-test harness detail</strong></summary>

`tests/Dorn.Templates.WebApi.Tests` packs the real NuGet package, installs it into the local `dotnet new` cache, then generates, builds, and tests a project exactly as a user would.

</details>

---

## 📦 Releases

- Package: `Dorn.Templates.WebApi`
- Tags: push `v<version>` to trigger NuGet Trusted Publishing
- Local builds: use non-release fallback versions and are never published
- Move relevant [CHANGELOG.md](CHANGELOG.md) entries from `[Unreleased]` to `[<version>]` when cutting a release

## ✅ Conventions

- Conventional commits: `type(scope): message`
- No `Co-Authored-By` or AI attribution
- English in code, comments, commits, and documentation
- xUnit with plain `Assert.*`; no FluentAssertions or Moq
- Comments only for a compact, non-obvious **why**
