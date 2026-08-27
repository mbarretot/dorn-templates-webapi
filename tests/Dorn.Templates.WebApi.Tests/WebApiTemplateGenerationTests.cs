using Xunit;

namespace Dorn.Templates.WebApi.Tests;

[Trait("Category", "Integration")]
[Collection(TemplatePackCollection.Name)]
public class WebApiTemplateGenerationTests
{
    [Fact]
    public async Task GenerateAndBuild_DornWebApiTemplate_ProducesBuildableSolution()
    {
        await GenerateBuildAndCleanupAsync(
            "DornIntegrationTestApp",
            async (outputDirectory, slnPath) =>
            {
                Assert.Equal("DornIntegrationTestApp.slnx", Path.GetFileName(slnPath));

                var buildResult = await BuildSupport.RunDotnetBuildAsync(slnPath);
                AssertBuildSucceeded(buildResult);
            }
        );
    }

    [Fact]
    public async Task Generate_DornWebApiTemplate_ShipsLocalToolManifestWithDornCli()
    {
        await GenerateAndCleanupAsync(
            "DornIntegrationTestManifestApp",
            async outputDirectory =>
            {
                var manifestPath = Path.Combine(outputDirectory, ".config", "dotnet-tools.json");
                Assert.True(
                    File.Exists(manifestPath),
                    $"Expected local tool manifest at '{manifestPath}' but it was not generated."
                );

                var manifestJson = await File.ReadAllTextAsync(manifestPath);
                Assert.Contains("\"dorn.cli\"", manifestJson, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("\"dorn\"", manifestJson, StringComparison.OrdinalIgnoreCase);
                Assert.Contains(
                    "\"rollForward\"",
                    manifestJson,
                    StringComparison.OrdinalIgnoreCase
                );
            }
        );
    }

    [Fact]
    public async Task GenerateAndBuild_DornWebApiTemplateWithNoAuth_EmitsNoAuthArtifacts()
    {
        await GenerateBuildAndCleanupAsync(
            "DornNoAuthApp",
            async (outputDirectory, slnPath) =>
            {
                var webApiDir = Path.Combine(outputDirectory, "src", "DornNoAuthApp.WebApi");
                var domainDir = Path.Combine(outputDirectory, "src", "DornNoAuthApp.Domain");
                var applicationDir = Path.Combine(
                    outputDirectory,
                    "src",
                    "DornNoAuthApp.Application"
                );
                var infrastructureDir = Path.Combine(
                    outputDirectory,
                    "src",
                    "DornNoAuthApp.Infrastructure"
                );
                Assert.False(
                    File.Exists(
                        Path.Combine(webApiDir, "Extensions", "AuthenticationExtensions.cs")
                    ),
                    "Auth=none must not emit AuthenticationExtensions.cs"
                );
                Assert.False(
                    File.Exists(Path.Combine(webApiDir, "Endpoints", "MeEndpoints.cs")),
                    "Auth=none must not emit MeEndpoints.cs"
                );
                Assert.False(
                    File.Exists(Path.Combine(webApiDir, "Endpoints", "AuthEndpoints.cs")),
                    "Auth=none must not emit AuthEndpoints.cs"
                );
                Assert.False(
                    File.Exists(Path.Combine(domainDir, "Users", "AppUser.cs")),
                    "Auth=none must not emit AppUser.cs"
                );
                Assert.False(
                    File.Exists(Path.Combine(applicationDir, "Auth", "Login", "LoginCommand.cs")),
                    "Auth=none must not emit LoginCommand.cs"
                );
                Assert.False(
                    File.Exists(
                        Path.Combine(applicationDir, "Auth", "Login", "LoginCommandHandler.cs")
                    ),
                    "Auth=none must not emit LoginCommandHandler.cs"
                );
                Assert.False(
                    File.Exists(
                        Path.Combine(applicationDir, "Auth", "Login", "LoginCommandValidator.cs")
                    ),
                    "Auth=none must not emit LoginCommandValidator.cs"
                );
                Assert.False(
                    File.Exists(Path.Combine(applicationDir, "Auth", "Login", "LoginResponse.cs")),
                    "Auth=none must not emit LoginResponse.cs"
                );
                Assert.False(
                    File.Exists(
                        Path.Combine(applicationDir, "Common", "Security", "ITokenService.cs")
                    ),
                    "Auth=none must not emit ITokenService.cs"
                );
                Assert.False(
                    File.Exists(Path.Combine(infrastructureDir, "Auth", "JwtOptions.cs")),
                    "Auth=none must not emit JwtOptions.cs"
                );
                Assert.False(
                    File.Exists(Path.Combine(infrastructureDir, "Auth", "JwtTokenService.cs")),
                    "Auth=none must not emit JwtTokenService.cs"
                );
                Assert.False(
                    File.Exists(Path.Combine(infrastructureDir, "Auth", "AuthSeedOptions.cs")),
                    "Auth=none must not emit AuthSeedOptions.cs"
                );
                Assert.False(
                    File.Exists(
                        Path.Combine(infrastructureDir, "Persistence", "Seed", "AuthSeeder.cs")
                    ),
                    "Auth=none must not emit AuthSeeder.cs"
                );
                var migrationsDir = Path.Combine(infrastructureDir, "Persistence", "Migrations");
                Assert.False(
                    Directory.Exists(migrationsDir)
                        && Directory
                            .GetFiles(migrationsDir, "*_AddAuthUser*", SearchOption.AllDirectories)
                            .Length > 0,
                    "Auth=none must not emit any *_AddAuthUser* migration file"
                );

                var applicationTestsAuthDir = Path.Combine(
                    outputDirectory,
                    "tests",
                    "DornNoAuthApp.Application.Tests",
                    "Auth"
                );
                var functionalTestsAuthDir = Path.Combine(
                    outputDirectory,
                    "tests",
                    "DornNoAuthApp.Functional.Tests",
                    "Auth"
                );
                Assert.False(
                    Directory.Exists(applicationTestsAuthDir),
                    "Auth=none must not emit tests/*.Application.Tests/Auth/"
                );
                Assert.False(
                    Directory.Exists(functionalTestsAuthDir),
                    "Auth=none must not emit tests/*.Functional.Tests/Auth/"
                );

                var appsettingsPath = Path.Combine(webApiDir, "appsettings.json");
                Assert.True(File.Exists(appsettingsPath));
                var appsettings = await File.ReadAllTextAsync(appsettingsPath);
                Assert.DoesNotContain("\"Jwt\"", appsettings, StringComparison.Ordinal);
                Assert.DoesNotContain("SigningKey", appsettings, StringComparison.Ordinal);

                var programCsPath = Path.Combine(webApiDir, "Program.cs");
                Assert.True(File.Exists(programCsPath));
                var programCs = await File.ReadAllTextAsync(programCsPath);
                Assert.DoesNotContain("UseAuthentication", programCs, StringComparison.Ordinal);
                Assert.DoesNotContain("UseAuthorization(", programCs, StringComparison.Ordinal);
                Assert.DoesNotContain("MapMeEndpoints", programCs, StringComparison.Ordinal);

                var csprojPath = Path.Combine(webApiDir, "DornNoAuthApp.WebApi.csproj");
                Assert.True(File.Exists(csprojPath));
                var csproj = await File.ReadAllTextAsync(csprojPath);
                Assert.DoesNotContain(
                    "Microsoft.AspNetCore.Authentication.JwtBearer",
                    csproj,
                    StringComparison.Ordinal
                );
                Assert.DoesNotContain(
                    "Microsoft.Extensions.Identity.Core",
                    csproj,
                    StringComparison.Ordinal
                );

                var buildResult = await BuildSupport.RunDotnetBuildAsync(slnPath);
                AssertBuildSucceeded(buildResult);
            },
            "--Auth",
            "none"
        );
    }

    [Fact]
    public async Task GenerateAndBuild_DornWebApiTemplateWithAzureAd_EmitsAuthArtifactsAndBuilds()
    {
        await GenerateBuildAndCleanupAsync(
            "DornAzureAdApp",
            async (outputDirectory, slnPath) =>
            {
                var webApiDir = Path.Combine(outputDirectory, "src", "DornAzureAdApp.WebApi");
                Assert.True(
                    File.Exists(
                        Path.Combine(webApiDir, "Extensions", "AuthenticationExtensions.cs")
                    ),
                    "Auth=azure-ad must emit AuthenticationExtensions.cs"
                );
                Assert.True(
                    File.Exists(Path.Combine(webApiDir, "Endpoints", "MeEndpoints.cs")),
                    "Auth=azure-ad must emit MeEndpoints.cs"
                );

                var appsettingsPath = Path.Combine(webApiDir, "appsettings.json");
                var appsettings = await File.ReadAllTextAsync(appsettingsPath);
                Assert.Contains("\"AzureAd\"", appsettings, StringComparison.Ordinal);
                Assert.Contains("\"ClientId\"", appsettings, StringComparison.Ordinal);
                Assert.DoesNotContain("\"Jwt\"", appsettings, StringComparison.Ordinal);

                var programCsPath = Path.Combine(webApiDir, "Program.cs");
                var programCs = await File.ReadAllTextAsync(programCsPath);
                Assert.Contains("UseAuthentication", programCs, StringComparison.Ordinal);
                Assert.Contains("MapMeEndpoints", programCs, StringComparison.Ordinal);

                var csprojPath = Path.Combine(webApiDir, "DornAzureAdApp.WebApi.csproj");
                var csproj = await File.ReadAllTextAsync(csprojPath);
                Assert.Contains(
                    "Microsoft.AspNetCore.Authentication.JwtBearer",
                    csproj,
                    StringComparison.Ordinal
                );
                Assert.DoesNotContain(
                    "Microsoft.Extensions.Identity.Core",
                    csproj,
                    StringComparison.Ordinal
                );

                var buildResult = await BuildSupport.RunDotnetBuildAsync(slnPath);
                AssertBuildSucceeded(buildResult);

                var functionalProject = Path.Combine(
                    outputDirectory,
                    "tests",
                    "DornAzureAdApp.Functional.Tests",
                    "DornAzureAdApp.Functional.Tests.csproj"
                );
                var functionalTestResult = await TemplatePackHarness.RunProcessAsync(
                    Path.GetDirectoryName(slnPath)!,
                    null,
                    "test",
                    functionalProject,
                    "-c",
                    "Release",
                    "--no-build",
                    "--filter",
                    "Auth"
                );
                Assert.True(
                    functionalTestResult.ExitCode == 0,
                    $"Generated azure-ad functional tests exited with {functionalTestResult.ExitCode}."
                        + $"{Environment.NewLine}STDOUT:{Environment.NewLine}{functionalTestResult.StdOut}"
                        + $"{Environment.NewLine}STDERR:{Environment.NewLine}{functionalTestResult.StdErr}"
                );
            },
            "--Auth",
            "azure-ad",
            "--Orm",
            "efcore"
        );
    }

    [Fact]
    public async Task GenerateAndBuild_DornWebApiTemplateWithCustomAuth_EmitsAuthArtifactsAndBuilds()
    {
        await GenerateBuildAndCleanupAsync(
            "DornCustomAuthApp",
            async (outputDirectory, slnPath) =>
            {
                var webApiDir = Path.Combine(outputDirectory, "src", "DornCustomAuthApp.WebApi");
                var infraDir = Path.Combine(
                    outputDirectory,
                    "src",
                    "DornCustomAuthApp.Infrastructure"
                );
                var applicationDir = Path.Combine(
                    outputDirectory,
                    "src",
                    "DornCustomAuthApp.Application"
                );
                var domainDir = Path.Combine(outputDirectory, "src", "DornCustomAuthApp.Domain");

                Assert.True(
                    File.Exists(
                        Path.Combine(webApiDir, "Extensions", "AuthenticationExtensions.cs")
                    ),
                    "Auth=custom must emit AuthenticationExtensions.cs"
                );
                Assert.True(
                    File.Exists(Path.Combine(webApiDir, "Endpoints", "MeEndpoints.cs")),
                    "Auth=custom must emit MeEndpoints.cs"
                );
                Assert.True(
                    File.Exists(Path.Combine(domainDir, "Users", "AppUser.cs")),
                    "Auth=custom must emit Domain/Users/AppUser.cs"
                );
                Assert.True(
                    File.Exists(Path.Combine(applicationDir, "Auth", "Login", "LoginCommand.cs")),
                    "Auth=custom must emit Application/Auth/Login/LoginCommand.cs"
                );
                Assert.True(
                    File.Exists(
                        Path.Combine(applicationDir, "Auth", "Login", "LoginCommandHandler.cs")
                    ),
                    "Auth=custom must emit Application/Auth/Login/LoginCommandHandler.cs"
                );
                Assert.True(
                    File.Exists(
                        Path.Combine(applicationDir, "Auth", "Login", "LoginCommandValidator.cs")
                    ),
                    "Auth=custom must emit Application/Auth/Login/LoginCommandValidator.cs"
                );
                Assert.True(
                    File.Exists(
                        Path.Combine(applicationDir, "Common", "Security", "ITokenService.cs")
                    ),
                    "Auth=custom must emit Application/Common/Security/ITokenService.cs"
                );
                Assert.True(
                    File.Exists(Path.Combine(infraDir, "Auth", "JwtTokenService.cs")),
                    "Auth=custom must emit Infrastructure/Auth/JwtTokenService.cs"
                );
                Assert.True(
                    File.Exists(Path.Combine(infraDir, "Auth", "JwtOptions.cs")),
                    "Auth=custom must emit Infrastructure/Auth/JwtOptions.cs"
                );

                var authExtensionsPath = Path.Combine(
                    webApiDir,
                    "Extensions",
                    "AuthenticationExtensions.cs"
                );
                var authExtensions = await File.ReadAllTextAsync(authExtensionsPath);
                Assert.Contains("AddJwtBearer", authExtensions, StringComparison.Ordinal);
                Assert.Contains(
                    "TokenValidationParameters",
                    authExtensions,
                    StringComparison.Ordinal
                );
                Assert.Contains("IssuerSigningKey", authExtensions, StringComparison.Ordinal);
                Assert.Contains("SymmetricSecurityKey", authExtensions, StringComparison.Ordinal);

                var infraCsproj = await File.ReadAllTextAsync(
                    Path.Combine(infraDir, "DornCustomAuthApp.Infrastructure.csproj")
                );
                Assert.Contains(
                    "Microsoft.IdentityModel.JsonWebTokens",
                    infraCsproj,
                    StringComparison.Ordinal
                );
                Assert.Contains(
                    "Microsoft.Extensions.Identity.Core",
                    infraCsproj,
                    StringComparison.Ordinal
                );

                var domainCsproj = await File.ReadAllTextAsync(
                    Path.Combine(domainDir, "DornCustomAuthApp.Domain.csproj")
                );
                Assert.Contains(
                    "Microsoft.Extensions.Identity.Stores",
                    domainCsproj,
                    StringComparison.Ordinal
                );

                var infraDiPath = Path.Combine(
                    infraDir,
                    "DependencyInjection",
                    "ServiceCollectionExtensions.cs"
                );
                var infraDi = await File.ReadAllTextAsync(infraDiPath);
                Assert.Contains("PasswordHasher<AppUser>", infraDi, StringComparison.Ordinal);
                Assert.Contains(
                    "ITokenService, JwtTokenService",
                    infraDi,
                    StringComparison.Ordinal
                );

                var buildResult = await BuildSupport.RunDotnetBuildAsync(slnPath);
                AssertBuildSucceeded(buildResult);

                var functionalProject = Path.Combine(
                    outputDirectory,
                    "tests",
                    "DornCustomAuthApp.Functional.Tests",
                    "DornCustomAuthApp.Functional.Tests.csproj"
                );
                var functionalTestResult = await TemplatePackHarness.RunProcessAsync(
                    Path.GetDirectoryName(slnPath)!,
                    null,
                    "test",
                    functionalProject,
                    "-c",
                    "Release",
                    "--no-build",
                    "--filter",
                    "Auth"
                );
                Assert.True(
                    functionalTestResult.ExitCode == 0,
                    $"Generated custom-auth functional tests exited with {functionalTestResult.ExitCode}."
                        + $"{Environment.NewLine}STDOUT:{Environment.NewLine}{functionalTestResult.StdOut}"
                        + $"{Environment.NewLine}STDERR:{Environment.NewLine}{functionalTestResult.StdErr}"
                );
            },
            "--Auth",
            "custom",
            "--Orm",
            "efcore"
        );
    }

    /// <summary>Catches migration namespace collisions, bad #if/Condition/rename modifiers, and stray //#if markers in appsettings.json.</summary>
    [Fact]
    public async Task GenerateAndBuild_DornWebApiTemplateWithSqlServer_ProducesBuildableSolution()
    {
        await GenerateBuildAndCleanupAsync(
            "DornIntegrationTestSqlServerApp",
            async (outputDirectory, slnPath) =>
            {
                var migrationsDirectory = Path.Combine(
                    outputDirectory,
                    "src",
                    "DornIntegrationTestSqlServerApp.Infrastructure",
                    "Persistence",
                    "Migrations"
                );
                Assert.True(Directory.Exists(migrationsDirectory));
                Assert.False(Directory.Exists(Path.Combine(migrationsDirectory, "Sqlite")));
                Assert.False(Directory.Exists(Path.Combine(migrationsDirectory, "SqlServer")));
                Assert.Single(
                    Directory.GetFiles(migrationsDirectory, "*ModelSnapshot.cs"),
                    path => Path.GetFileName(path) == "ApplicationDbContextModelSnapshot.cs"
                );

                Assert.False(
                    File.Exists(Path.Combine(outputDirectory, "otel-collector-config.yaml")),
                    "DatabaseProvider=sqlserver with Orchestrator=aspire must not emit otel-collector-config.yaml"
                );
                Assert.False(
                    File.Exists(Path.Combine(outputDirectory, "tempo.yaml")),
                    "DatabaseProvider=sqlserver with Orchestrator=aspire must not emit tempo.yaml"
                );
                Assert.False(
                    File.Exists(
                        Path.Combine(
                            outputDirectory,
                            "grafana",
                            "provisioning",
                            "datasources",
                            "datasources.yaml"
                        )
                    ),
                    "DatabaseProvider=sqlserver with Orchestrator=aspire must not emit grafana/provisioning/datasources/datasources.yaml"
                );

                Assert.Contains("AppHost", await File.ReadAllTextAsync(slnPath));

                var buildResult = await BuildSupport.RunDotnetBuildAsync(slnPath);
                AssertBuildSucceeded(buildResult);
            },
            "--DatabaseProvider",
            "sqlserver"
        );
    }

    /// <summary>Mirrors the sqlserver cell above (namespace collisions, #if/Condition/rename modifiers, stray markers).</summary>
    [Fact]
    public async Task GenerateAndBuild_DornWebApiTemplateWithPostgres_ProducesBuildableSolution()
    {
        await GenerateBuildAndCleanupAsync(
            "DornIntegrationTestPostgresApp",
            async (outputDirectory, slnPath) =>
            {
                var migrationsDirectory = Path.Combine(
                    outputDirectory,
                    "src",
                    "DornIntegrationTestPostgresApp.Infrastructure",
                    "Persistence",
                    "Migrations"
                );
                Assert.True(Directory.Exists(migrationsDirectory));
                Assert.False(Directory.Exists(Path.Combine(migrationsDirectory, "Sqlite")));
                Assert.False(Directory.Exists(Path.Combine(migrationsDirectory, "SqlServer")));
                Assert.False(Directory.Exists(Path.Combine(migrationsDirectory, "Postgres")));
                Assert.Single(
                    Directory.GetFiles(migrationsDirectory, "*ModelSnapshot.cs"),
                    path => Path.GetFileName(path) == "ApplicationDbContextModelSnapshot.cs"
                );

                Assert.False(
                    File.Exists(Path.Combine(outputDirectory, "otel-collector-config.yaml")),
                    "DatabaseProvider=postgres with Orchestrator=aspire must not emit otel-collector-config.yaml"
                );
                Assert.False(
                    File.Exists(Path.Combine(outputDirectory, "tempo.yaml")),
                    "DatabaseProvider=postgres with Orchestrator=aspire must not emit tempo.yaml"
                );
                Assert.False(
                    File.Exists(
                        Path.Combine(
                            outputDirectory,
                            "grafana",
                            "provisioning",
                            "datasources",
                            "datasources.yaml"
                        )
                    ),
                    "DatabaseProvider=postgres with Orchestrator=aspire must not emit grafana/provisioning/datasources/datasources.yaml"
                );

                Assert.Contains("AppHost", await File.ReadAllTextAsync(slnPath));

                var buildResult = await BuildSupport.RunDotnetBuildAsync(slnPath);
                AssertBuildSucceeded(buildResult);
            },
            "--DatabaseProvider",
            "postgres"
        );
    }

    /// <summary>
    /// Source-level only, not a nested build: `dotnet build` from inside this xunit host is
    /// unreliable for CPM resolution through ProjectReference (fails here, succeeds from a plain
    /// shell — a nested dotnet-in-dotnet-test artifact, not a real defect).
    /// </summary>
    [Fact]
    public async Task Generate_DornWebApiTemplateWithPostgresAndDapper_ExcludesEfOnlyMigrationsAndWiresNpgsql()
    {
        await GenerateAndCleanupAsync(
            "DornIntegrationTestPostgresDapperApp",
            async outputDirectory =>
            {
                var infrastructureDirectory = Path.Combine(
                    outputDirectory,
                    "src",
                    "DornIntegrationTestPostgresDapperApp.Infrastructure"
                );
                Assert.False(
                    Directory.Exists(
                        Path.Combine(infrastructureDirectory, "Persistence", "Migrations")
                    )
                );
                Assert.False(
                    Directory.Exists(
                        Path.Combine(infrastructureDirectory, "Repositories", "EfCore")
                    )
                );
                Assert.True(
                    Directory.Exists(
                        Path.Combine(infrastructureDirectory, "Repositories", "Dapper")
                    )
                );

                var dapperContextPath = Path.Combine(
                    infrastructureDirectory,
                    "Repositories",
                    "Dapper",
                    "DapperContext.cs"
                );
                Assert.True(File.Exists(dapperContextPath));
                var dapperContextSource = await File.ReadAllTextAsync(dapperContextPath);
                Assert.Contains("using Npgsql;", dapperContextSource, StringComparison.Ordinal);
                Assert.Contains(
                    "new NpgsqlConnection(",
                    dapperContextSource,
                    StringComparison.Ordinal
                );
                Assert.DoesNotContain(
                    "Postgres provider wiring lands in Slice B",
                    dapperContextSource,
                    StringComparison.Ordinal
                );

                var infrastructureCsprojSource = await File.ReadAllTextAsync(
                    Path.Combine(
                        infrastructureDirectory,
                        "DornIntegrationTestPostgresDapperApp.Infrastructure.csproj"
                    )
                );
                Assert.Contains("Npgsql", infrastructureCsprojSource, StringComparison.Ordinal);
            },
            "--DatabaseProvider",
            "postgres",
            "--Orm",
            "dapper"
        );
    }

    /// <summary>Omits Aspire projects while retaining Docker assets.</summary>
    [Fact]
    public async Task GenerateAndBuild_DornWebApiTemplateWithDockerComposeAndSqlite_ProducesBuildableSolution()
    {
        await GenerateBuildAndCleanupAsync(
            "DornIntegrationTestComposeApp",
            async (outputDirectory, slnPath) =>
            {
                Assert.False(
                    Directory.Exists(
                        Path.Combine(
                            outputDirectory,
                            "src",
                            "DornIntegrationTestComposeApp.AppHost"
                        )
                    )
                );
                Assert.False(
                    Directory.Exists(
                        Path.Combine(
                            outputDirectory,
                            "src",
                            "DornIntegrationTestComposeApp.ServiceDefaults"
                        )
                    )
                );
                Assert.True(
                    File.Exists(
                        Path.Combine(
                            outputDirectory,
                            "src",
                            "DornIntegrationTestComposeApp.WebApi",
                            "Dockerfile"
                        )
                    )
                );
                Assert.True(File.Exists(Path.Combine(outputDirectory, "docker-compose.yml")));

                Assert.DoesNotContain("AppHost", await File.ReadAllTextAsync(slnPath));

                var buildResult = await BuildSupport.RunDotnetBuildAsync(slnPath);
                AssertBuildSucceeded(buildResult);
            },
            "--Orchestrator",
            "docker-compose"
        );
    }

    /// <summary>Omits orchestration files but retains the Docker assets and solution.</summary>
    [Fact]
    public async Task GenerateAndBuild_DornWebApiTemplateWithNoneOrchestrator_ProducesBuildableSolution()
    {
        await GenerateBuildAndCleanupAsync(
            "DornIntegrationTestNoneApp",
            async (outputDirectory, slnPath) =>
            {
                Assert.False(
                    Directory.Exists(
                        Path.Combine(outputDirectory, "src", "DornIntegrationTestNoneApp.AppHost")
                    )
                );
                Assert.False(
                    Directory.Exists(
                        Path.Combine(
                            outputDirectory,
                            "src",
                            "DornIntegrationTestNoneApp.ServiceDefaults"
                        )
                    )
                );
                Assert.True(
                    File.Exists(
                        Path.Combine(
                            outputDirectory,
                            "src",
                            "DornIntegrationTestNoneApp.WebApi",
                            "Dockerfile"
                        )
                    )
                );
                Assert.True(File.Exists(Path.Combine(outputDirectory, ".dockerignore")));
                Assert.False(File.Exists(Path.Combine(outputDirectory, "docker-compose.yml")));
                Assert.False(
                    File.Exists(Path.Combine(outputDirectory, "docker-compose.SqlServer.yml"))
                );
                Assert.False(
                    File.Exists(Path.Combine(outputDirectory, "otel-collector-config.yaml")),
                    "Orchestrator=none must not emit otel-collector-config.yaml"
                );
                Assert.False(
                    File.Exists(Path.Combine(outputDirectory, "tempo.yaml")),
                    "Orchestrator=none must not emit tempo.yaml"
                );
                Assert.False(
                    File.Exists(
                        Path.Combine(
                            outputDirectory,
                            "grafana",
                            "provisioning",
                            "datasources",
                            "datasources.yaml"
                        )
                    ),
                    "Orchestrator=none must not emit grafana/provisioning/datasources/datasources.yaml"
                );

                Assert.DoesNotContain("AppHost", await File.ReadAllTextAsync(slnPath));

                var buildResult = await BuildSupport.RunDotnetBuildAsync(slnPath);
                AssertBuildSucceeded(buildResult);
            },
            "--Orchestrator",
            "none"
        );
    }

    [Fact]
    public async Task GivenNoneOrchestrator_ProducesObservabilityExtension()
    {
        await GenerateAndCleanupAsync(
            "DornOtelNoneApp",
            async outputDirectory =>
            {
                var webApiDir = Path.Combine(outputDirectory, "src", "DornOtelNoneApp.WebApi");

                Assert.True(
                    File.Exists(
                        Path.Combine(webApiDir, "Extensions", "ObservabilityExtensions.cs")
                    ),
                    "Orchestrator=none must emit ObservabilityExtensions.cs"
                );
                Assert.False(
                    Directory.Exists(
                        Path.Combine(outputDirectory, "src", "DornOtelNoneApp.ServiceDefaults")
                    ),
                    "Orchestrator=none must not emit the ServiceDefaults project"
                );

                var programCs = await File.ReadAllTextAsync(Path.Combine(webApiDir, "Program.cs"));
                Assert.Contains("AddObservability()", programCs, StringComparison.Ordinal);
                Assert.DoesNotContain("AddServiceDefaults()", programCs, StringComparison.Ordinal);

                var csproj = await File.ReadAllTextAsync(
                    Path.Combine(webApiDir, "DornOtelNoneApp.WebApi.csproj")
                );
                foreach (
                    var package in new[]
                    {
                        "OpenTelemetry.Exporter.OpenTelemetryProtocol",
                        "OpenTelemetry.Extensions.Hosting",
                        "OpenTelemetry.Instrumentation.AspNetCore",
                        "OpenTelemetry.Instrumentation.Http",
                        "OpenTelemetry.Instrumentation.Runtime",
                    }
                )
                {
                    Assert.Contains(
                        $"PackageReference Include=\"{package}\"",
                        csproj,
                        StringComparison.Ordinal
                    );
                }
            },
            "--Orchestrator",
            "none"
        );
    }

    [Fact]
    public async Task GivenAspireOrchestrator_NoDoubleOtelRegistration()
    {
        await GenerateAndCleanupAsync(
            "DornOtelAspireApp",
            async outputDirectory =>
            {
                var serviceDefaultsPath = Path.Combine(
                    outputDirectory,
                    "src",
                    "DornOtelAspireApp.ServiceDefaults",
                    "Extensions.cs"
                );
                Assert.True(File.Exists(serviceDefaultsPath));
                var serviceDefaultsSource = await File.ReadAllTextAsync(serviceDefaultsPath);
                Assert.DoesNotContain(
                    "ConfigureOpenTelemetry",
                    serviceDefaultsSource,
                    StringComparison.Ordinal
                );
                Assert.DoesNotContain(
                    "AddOpenTelemetryExporters",
                    serviceDefaultsSource,
                    StringComparison.Ordinal
                );
                Assert.Contains(
                    "AddDefaultHealthChecks",
                    serviceDefaultsSource,
                    StringComparison.Ordinal
                );
                Assert.Contains(
                    "MapDefaultEndpoints",
                    serviceDefaultsSource,
                    StringComparison.Ordinal
                );
                Assert.Contains(
                    "HealthEndpointPath",
                    serviceDefaultsSource,
                    StringComparison.Ordinal
                );
                Assert.Contains(
                    "AlivenessEndpointPath",
                    serviceDefaultsSource,
                    StringComparison.Ordinal
                );

                var serviceDefaultsCsproj = await File.ReadAllTextAsync(
                    Path.Combine(
                        outputDirectory,
                        "src",
                        "DornOtelAspireApp.ServiceDefaults",
                        "DornOtelAspireApp.ServiceDefaults.csproj"
                    )
                );
                Assert.DoesNotContain(
                    "OpenTelemetry.",
                    serviceDefaultsCsproj,
                    StringComparison.Ordinal
                );

                var observabilityExtensionsPath = Path.Combine(
                    outputDirectory,
                    "src",
                    "DornOtelAspireApp.WebApi",
                    "Extensions",
                    "ObservabilityExtensions.cs"
                );
                Assert.True(
                    File.Exists(observabilityExtensionsPath),
                    "ObservabilityExtensions.cs must be emitted for every orchestrator, including aspire"
                );

                var programCs = await File.ReadAllTextAsync(
                    Path.Combine(outputDirectory, "src", "DornOtelAspireApp.WebApi", "Program.cs")
                );
                Assert.Contains("AddObservability()", programCs, StringComparison.Ordinal);
                Assert.Contains("AddServiceDefaults()", programCs, StringComparison.Ordinal);
            }
        );
    }

    /// <summary>Verifies its SQL Server connection override and clean generated settings.</summary>
    [Fact]
    public async Task GenerateAndBuild_DornWebApiTemplateWithDockerComposeAndSqlServer_ProducesBuildableSolution()
    {
        await GenerateBuildAndCleanupAsync(
            "DornIntegrationTestComposeSqlServerApp",
            async (outputDirectory, slnPath) =>
            {
                var composeFile = Path.Combine(outputDirectory, "docker-compose.yml");
                Assert.True(File.Exists(composeFile));
                var composeContent = await File.ReadAllTextAsync(composeFile);
                Assert.Contains("sqlserver:", composeContent);
                Assert.Contains("ConnectionStrings__", composeContent);

                var migrationsDirectory = Path.Combine(
                    outputDirectory,
                    "src",
                    "DornIntegrationTestComposeSqlServerApp.Infrastructure",
                    "Persistence",
                    "Migrations"
                );
                Assert.True(Directory.Exists(migrationsDirectory));
                Assert.False(Directory.Exists(Path.Combine(migrationsDirectory, "Sqlite")));
                Assert.False(Directory.Exists(Path.Combine(migrationsDirectory, "SqlServer")));

                var appSettingsContent = await File.ReadAllTextAsync(
                    Path.Combine(
                        outputDirectory,
                        "src",
                        "DornIntegrationTestComposeSqlServerApp.WebApi",
                        "appsettings.json"
                    )
                );
                Assert.DoesNotContain("//#if", appSettingsContent);

                var buildResult = await BuildSupport.RunDotnetBuildAsync(slnPath);
                AssertBuildSucceeded(buildResult);
            },
            "--Orchestrator",
            "docker-compose",
            "--DatabaseProvider",
            "sqlserver"
        );
    }

    /// <summary>Verifies its PostgreSQL connection override and clean generated settings.</summary>
    [Fact]
    public async Task GenerateAndBuild_DornWebApiTemplateWithDockerComposeAndPostgres_ProducesBuildableSolution()
    {
        await GenerateBuildAndCleanupAsync(
            "DornIntegrationTestComposePostgresApp",
            async (outputDirectory, slnPath) =>
            {
                var composeFile = Path.Combine(outputDirectory, "docker-compose.yml");
                Assert.True(File.Exists(composeFile));
                var composeContent = await File.ReadAllTextAsync(composeFile);
                Assert.Contains("postgres:", composeContent);
                Assert.Contains("ConnectionStrings__", composeContent);
                Assert.False(
                    File.Exists(Path.Combine(outputDirectory, "docker-compose.SqlServer.yml"))
                );

                var migrationsDirectory = Path.Combine(
                    outputDirectory,
                    "src",
                    "DornIntegrationTestComposePostgresApp.Infrastructure",
                    "Persistence",
                    "Migrations"
                );
                Assert.True(Directory.Exists(migrationsDirectory));
                Assert.False(Directory.Exists(Path.Combine(migrationsDirectory, "Sqlite")));
                Assert.False(Directory.Exists(Path.Combine(migrationsDirectory, "Postgres")));

                var appSettingsContent = await File.ReadAllTextAsync(
                    Path.Combine(
                        outputDirectory,
                        "src",
                        "DornIntegrationTestComposePostgresApp.WebApi",
                        "appsettings.json"
                    )
                );
                Assert.DoesNotContain("//#if", appSettingsContent);

                var buildResult = await BuildSupport.RunDotnetBuildAsync(slnPath);
                AssertBuildSucceeded(buildResult);
            },
            "--Orchestrator",
            "docker-compose",
            "--DatabaseProvider",
            "postgres"
        );
    }

    internal static void AssertBuildSucceeded(
        (int ExitCode, string StdOut, string StdErr) buildResult
    )
    {
        Assert.True(
            buildResult.ExitCode == 0,
            $"dotnet build exited with {buildResult.ExitCode}."
                + $"{Environment.NewLine}STDOUT:{Environment.NewLine}{buildResult.StdOut}"
                + $"{Environment.NewLine}STDERR:{Environment.NewLine}{buildResult.StdErr}"
        );
    }

    internal static async Task<string> GenerateAsync(
        string name,
        string outputDirectory,
        params string[] extraArgs
    )
    {
        var result = await TemplatePackHarness.GenerateAsync(
            "dorn-webapi",
            name,
            outputDirectory,
            extraArgs
        );
        Assert.True(
            result.ExitCode == 0,
            $"Template generation failed (exit {result.ExitCode})."
                + $"{Environment.NewLine}STDOUT:{Environment.NewLine}{result.StdOut}"
                + $"{Environment.NewLine}STDERR:{Environment.NewLine}{result.StdErr}"
        );

        var slnFiles = Directory.GetFiles(outputDirectory, "*.slnx", SearchOption.TopDirectoryOnly);
        Assert.Single(slnFiles);
        return slnFiles[0];
    }

    private static async Task GenerateAndCleanupAsync(
        string name,
        Func<string, Task> body,
        params string[] extraArgs
    )
    {
        var outputDirectory = Path.Combine(
            BuildSupport.RealTempRoot,
            $"dorn-tests-webapi-{Guid.NewGuid():N}"
        );
        try
        {
            await GenerateAsync(name, outputDirectory, extraArgs);
            await body(outputDirectory);
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                await BuildSupport.DeleteDirectoryWithRetryAsync(outputDirectory);
            }
        }
    }

    private static async Task GenerateBuildAndCleanupAsync(
        string name,
        Func<string, string, Task> body,
        params string[] extraArgs
    )
    {
        var outputDirectory = Path.Combine(
            BuildSupport.RealTempRoot,
            $"dorn-tests-webapi-{Guid.NewGuid():N}"
        );
        try
        {
            var slnPath = await GenerateAsync(name, outputDirectory, extraArgs);
            await body(outputDirectory, slnPath);
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                await BuildSupport.DeleteDirectoryWithRetryAsync(outputDirectory);
            }
        }
    }
}
