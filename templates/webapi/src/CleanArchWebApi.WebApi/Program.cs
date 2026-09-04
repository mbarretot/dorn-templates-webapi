using CleanArchWebApi.Application.Todos.CreateTodoItem;
using CleanArchWebApi.Infrastructure.DependencyInjection;
using CleanArchWebApi.WebApi;
using CleanArchWebApi.WebApi.Endpoints;
using CleanArchWebApi.WebApi.Extensions;
using Dorn.Messaging;
using FluentValidation;
#if (UseEfCore)
using CleanArchWebApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
#endif
#if (UseDapper)
using CleanArchWebApi.Infrastructure.Repositories.Dapper;
#endif
#if (UseCustomAuth)
using CleanArchWebApi.Domain.Users;
using CleanArchWebApi.Infrastructure.Auth;
using CleanArchWebApi.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
#endif

var builder = WebApplication.CreateBuilder(args);

builder.AddObservability();

#if (UseAspire)
builder.AddServiceDefaults();
#endif
#if (!UseAspire)
// Aspire's ServiceDefaults wires this up already; every other orchestrator needs its own
// baseline liveness/readiness endpoint for container healthchecks and monitoring.
builder.Services.AddHealthChecks();
#endif

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddCaching();
builder.Services.AddMediator(typeof(CreateTodoItemCommand).Assembly);
builder.Services.AddValidatorsFromAssembly(typeof(CreateTodoItemCommand).Assembly);
builder.Services.AddOpenApi();

builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddRateLimiting();

#if (UseAuth)
#if (UseCustomAuth)
builder.Services.AddCustomJwtAuth(builder.Configuration, builder.Environment);
#elif (UseAzureAdAuth)
builder.Services.AddAzureAdAuth(builder.Configuration);
#endif
builder.Services.AddAuthorization();
#endif

var app = builder.Build();

#if (UseEfCore)
// Applies pending migrations on startup so `dotnet run` works against a fresh SQLite
// file with zero manual setup. Fine for this scaffold's default (SQLite, single instance);
// swap for a startup migration job or manual `dotnet ef database update` in production setups
// with concurrent instances.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();
#if (UseCustomAuth)
    var seededPassword = await AuthSeeder.SeedAsync(
        dbContext,
        scope.ServiceProvider.GetRequiredService<IPasswordHasher<AppUser>>(),
        scope.ServiceProvider.GetRequiredService<IOptions<AuthSeedOptions>>(),
        CancellationToken.None
    );
    if (!string.IsNullOrEmpty(seededPassword))
    {
        var seedEmail = scope
            .ServiceProvider.GetRequiredService<IOptions<AuthSeedOptions>>()
            .Value.DemoEmail;
        app.Logger.LogWarning(
            "Seeded demo user '{Email}' with password '{Password}' (development convenience only; configure or seed your own user in production).",
            seedEmail,
            seededPassword
        );
    }
#endif
}
#endif
#if (UseDapper)
// Dapper has no migration story of its own, so bootstrap the schema on startup the same
// way the EF Core branch above does via MigrateAsync. Fine for this scaffold's default
// (SQLite, single instance); swap for a real migration tool in production setups with
// concurrent instances.
using (var scope = app.Services.CreateScope())
{
    var dapperContext = scope.ServiceProvider.GetRequiredService<DapperContext>();
    await dapperContext.InitializeSchemaAsync();
}
#endif

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();

app.UseHttpsRedirection();

app.UseRateLimiter();

#if (UseAuth)
app.UseAuthentication();
app.UseAuthorization();
#endif

app.MapTodoEndpoints();
#if (UseAuth)
app.MapMeEndpoints();
#endif
#if (UseCustomAuth)
app.MapAuthEndpoints();
#endif
#if (UseAspire)
app.MapDefaultEndpoints();
#endif
#if (!UseAspire)
app.MapHealthChecks("/health");
#endif

app.Run();

// Top-level statement Program is internal by default; WebApplicationFactory<Program> needs
// a public type it can reference from CleanArchWebApi.Functional.Tests.
public partial class Program;
