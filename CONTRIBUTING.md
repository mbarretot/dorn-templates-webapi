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
| Persistence (EF Core / Dapper) | `src/CleanArchWebApi.Infrastructure` | `Orm` and `DatabaseProvider` template.json exclude/rename rules |
| Endpoints & composition root | `src/CleanArchWebApi.WebApi` | `Auth` template.json exclude rules |
| Auth (`custom`, `azure-ad`) | `Extensions/AuthenticationExtensions.cs`, `Endpoints/{Auth,Me}Endpoints.cs` | `custom` requires `Orm=efcore`; enforced by a `#error` guard in `src/CleanArchWebApi.Domain/TemplateConstraints.cs` (the template engine has no declarative cross-parameter constraint) |
| Orchestration (`aspire`, `docker-compose`, `none`) | `src/CleanArchWebApi.AppHost`, `src/CleanArchWebApi.ServiceDefaults`, `docker-compose*.yml` | Observability wiring stays equivalent across all three |
| Template parameters | `.template.config/template.json` | Root and package README option tables |
| Generated CI workflow | `.github/workflows/ci.yml` (inside the template) | `tests/Dorn.Templates.WebApi.Tests` structural assertions |
| Shared package versions | Template-local `Directory.Packages.props` | Package references remain intentional |

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
