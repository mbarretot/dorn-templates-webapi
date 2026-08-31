#if (UseDapper)
#if (UseSqlServer)
using Testcontainers.MsSql;
#elif (UsePostgres)
using Testcontainers.PostgreSql;
#endif
using CleanArchWebApi.Domain.Common.Interfaces;
using CleanArchWebApi.Domain.Events;
using CleanArchWebApi.Infrastructure.Repositories.Dapper;
using Microsoft.Extensions.Configuration;

namespace CleanArchWebApi.Integration.Tests.Todos;

/// <summary>
/// Exercises the Dapper repository's schema bootstrap, unit-of-work, and domain-event
/// publishing against the selected real provider (Testcontainers SQL Server/PostgreSQL, or a
/// temp-file SQLite database).
/// </summary>
public sealed class DapperTodoItemRepositoryTests : IAsyncLifetime
{
#if (UseSqlite)
    private readonly string _databasePath = Path.Combine(
        Path.GetTempPath(),
        $"{Guid.NewGuid()}.db"
    );
#elif (UseSqlServer)
    // Same image tag as docker-compose.SqlServer.yml, kept in sync deliberately.
    private readonly MsSqlContainer _container = new MsSqlBuilder(
        "mcr.microsoft.com/mssql/server:2022-latest"
    ).Build();
#elif (UsePostgres)
    // Same image tag as docker-compose.Postgres.yml, kept in sync deliberately.
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17").Build();
#endif

    private readonly IPublisher _publisher = Substitute.For<IPublisher>();
    private ITodoItemRepository _repository = null!;

    public async Task InitializeAsync()
    {
#if (UseSqlite)
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Default"] = $"Data Source={_databasePath}",
                }
            )
            .Build();
#elif (UseSqlServer)
        await _container.StartAsync();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:CleanArchWebApi"] = _container.GetConnectionString(),
                }
            )
            .Build();
#elif (UsePostgres)
        await _container.StartAsync();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:CleanArchWebApi"] = _container.GetConnectionString(),
                }
            )
            .Build();
#endif

        var context = new DapperContext(configuration);
        await context.InitializeSchemaAsync();

        _repository = new TodoItemRepository(context, _publisher);
    }

    [Fact]
    public async Task Add_ThenSaveChangesAsync_PersistsAndPublishesTheCreatedEvent()
    {
        var todoItem = TodoItem.Create("Prove Dapper persists and publishes events");

        _repository.Add(todoItem);
        await _repository.SaveChangesAsync(CancellationToken.None);

        var reloaded = await _repository.GetByIdAsync(todoItem.Id);
        Assert.NotNull(reloaded);
        Assert.Equal(todoItem.Title, reloaded!.Title);
        Assert.False(reloaded.IsComplete);

        await _publisher
            .Received(1)
            .Publish(
                Arg.Is<TodoItemCreatedEvent>(e => e.TodoItemId == todoItem.Id),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task Add_WithoutSaveChangesAsync_DoesNotPersist()
    {
        var todoItem = TodoItem.Create("Never actually saved");

        _repository.Add(todoItem);

        var reloaded = await _repository.GetByIdAsync(todoItem.Id);
        Assert.Null(reloaded);
    }

    public async Task DisposeAsync()
    {
#if (UseSqlite)
        // Microsoft.Data.Sqlite pools native connections by file path, so disposing can leave
        // the database locked on Windows until SqliteConnection.ClearAllPools() is called.
        SqliteConnection.ClearAllPools();
        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
#elif (UseSqlServer)
        await _container.DisposeAsync();
#elif (UsePostgres)
        await _container.DisposeAsync();
#endif
    }
}
#endif
