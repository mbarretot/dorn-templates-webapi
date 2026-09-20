using CleanArchWebApi.Application.Common.Behaviors;
#if (IncludeSample)
using CleanArchWebApi.Application.Todos.CreateTodoItem;
#endif
using Dorn.Messaging;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;

namespace CleanArchWebApi.Application.Tests.Messaging;

public sealed class AddMediatorTests
{
#if (!IncludeSample)
    private sealed record PingQuery : IRequest<string>;

#endif
    [Fact]
    public void AddMediator_RegistersOpenGenericPipelineBehaviors_WithoutThrowingOnBuild()
    {
        var services = new ServiceCollection();
        // CachingBehavior/CacheInvalidationBehavior are open-generic IPipelineBehavior<,> implementations
        // discovered by AddMediator's assembly scan alongside ValidationBehavior, so resolving them needs
        // HybridCache registered too -- exactly like CachingExtensions.AddCaching does in Program.cs.
        services.AddHybridCache();
        services.AddMediator(AssemblyReference.Assembly);

        var provider = services.BuildServiceProvider();

#if (IncludeSample)
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
#else
        var behaviors = provider.GetServices<IPipelineBehavior<PingQuery, string>>().ToList();

        Assert.Contains(behaviors, behavior => behavior is ValidationBehavior<PingQuery, string>);
        Assert.Contains(behaviors, behavior => behavior is CachingBehavior<PingQuery, string>);
        Assert.Contains(
            behaviors,
            behavior => behavior is CacheInvalidationBehavior<PingQuery, string>
        );
#endif
    }
}
