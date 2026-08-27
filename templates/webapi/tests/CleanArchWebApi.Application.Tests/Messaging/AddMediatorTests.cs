using CleanArchWebApi.Application.Common.Behaviors;
using CleanArchWebApi.Application.Todos.CreateTodoItem;
using Dorn.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace CleanArchWebApi.Application.Tests.Messaging;

public sealed class AddMediatorTests
{
    [Fact]
    public void AddMediator_RegistersOpenGenericPipelineBehavior_WithoutThrowingOnBuild()
    {
        var services = new ServiceCollection();
        services.AddMediator(typeof(CreateTodoItemCommand).Assembly);

        var provider = services.BuildServiceProvider();

        var behaviors = provider.GetServices<IPipelineBehavior<CreateTodoItemCommand, Guid>>();

        Assert.Contains(
            behaviors,
            behavior => behavior is ValidationBehavior<CreateTodoItemCommand, Guid>
        );
    }
}
