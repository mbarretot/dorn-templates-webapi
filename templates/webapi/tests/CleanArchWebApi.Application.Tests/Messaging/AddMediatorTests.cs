using CleanArchWebApi.Application.Common.Behaviors;
using CleanArchWebApi.Application.Todos.CreateTodoItem;
using Dorn.Messaging;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;

namespace CleanArchWebApi.Application.Tests.Messaging;

public sealed class AddMediatorTests
{
    [Fact]
    public void AddMediator_RegistersOpenGenericPipelineBehaviors_WithoutThrowingOnBuild()
    {
        var services = new ServiceCollection();
        // CachingBehavior/CacheInvalidationBehavior are open-generic IPipelineBehavior<,> implementations
        // discovered by AddMediator's assembly scan alongside ValidationBehavior, so resolving them needs
        // HybridCache registered too -- exactly like CachingExtensions.AddCaching does in Program.cs.
        services.AddHybridCache();
        services.AddMediator(typeof(CreateTodoItemCommand).Assembly);

        var provider = services.BuildServiceProvider();

        var behaviors = provider
            .GetServices<IPipelineBehavior<CreateTodoItemCommand, Guid>>()
            .ToList();

        Assert.Contains(
            behaviors,
            behavior => behavior is ValidationBehavior<CreateTodoItemCommand, Guid>
        );
        Assert.Contains(
            behaviors,
            behavior => behavior is CachingBehavior<CreateTodoItemCommand, Guid>
        );
        Assert.Contains(
            behaviors,
            behavior => behavior is CacheInvalidationBehavior<CreateTodoItemCommand, Guid>
        );
    }
}
