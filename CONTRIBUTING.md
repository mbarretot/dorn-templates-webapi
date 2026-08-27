# Contributing

Thanks for improving the Dorn Web API template. Keep changes focused and backed by tests.

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

| Area | Source of truth | Keep aligned |
| --- | --- | --- |
| Domain entities & events | `src/CleanArchWebApi.Domain` | Application ports and Infrastructure implementations |
| Commands, queries, handlers | `src/CleanArchWebApi.Application` | Validators and pipeline behaviors |
| Persistence (EF Core / Dapper) | `src/CleanArchWebApi.Infrastructure` | `Orm` and `DatabaseProvider` template.json exclude/rename rules |
| Endpoints & composition root | `src/CleanArchWebApi.WebApi` | `Auth` template.json exclude rules |
| Auth (`custom`, `azure-ad`) | `Extensions/AuthenticationExtensions.cs`, `Endpoints/{Auth,Me}Endpoints.cs` | `custom` requires `Orm=efcore`; enforced by `.template.config/template.json` |
| Orchestration (`aspire`, `docker-compose`, `none`) | `src/CleanArchWebApi.AppHost`, `src/CleanArchWebApi.ServiceDefaults`, `docker-compose*.yml` | Observability wiring stays equivalent across all three |
| Template parameters | `.template.config/template.json` | This repo's README and `eng/packaging/Dorn.Templates.WebApi/README.md` option tables |
| Generated CI workflow | `.github/workflows/ci.yml` (inside the template) | `tests/Dorn.Templates.WebApi.Tests` structural assertions |
| MudBlazor-equivalent shared packages | `Directory.Packages.props` (template-local) | Pinned versions match `docs/templates/webapi.md`-equivalent references |

<details>
<summary><strong>Generation-test harness detail</strong></summary>

`tests/Dorn.Templates.WebApi.Tests` packs the real `Dorn.Templates.WebApi` NuGet package, installs it into the local `dotnet new` template cache, and drives generation through `dotnet new dorn-webapi`, `dotnet build`, and `dotnet test` against the generated output — the same distribution mechanism an end user goes through, decoupled from any internal Dorn API.

</details>

---

## 📦 Releases

- Package: `Dorn.Templates.WebApi`
- Tags: push `v<version>` to trigger NuGet Trusted Publishing
- Local builds: use non-release fallback versions and are never published

## ✅ Conventions

- Conventional commits: `type(scope): message`
- No `Co-Authored-By` or AI attribution
- English in code, comments, commits, and documentation
- xUnit with plain `Assert.*`; no FluentAssertions or Moq
- Comments only for a compact, non-obvious **why**
