using Dorn.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace CleanArchWebApi.Application.Tests.Messaging;

public sealed class MediatorPublishTests
{
    private sealed record TestNotification(string Message) : INotification;

    private sealed class RecordingHandler : INotificationHandler<TestNotification>
    {
        public bool WasCalled { get; private set; }

        public Task Handle(TestNotification notification, CancellationToken ct)
        {
            WasCalled = true;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Publish_InvokesEveryRegisteredHandler()
    {
        var first = new RecordingHandler();
        var second = new RecordingHandler();

        var services = new ServiceCollection();
        services.AddSingleton<INotificationHandler<TestNotification>>(first);
        services.AddSingleton<INotificationHandler<TestNotification>>(second);
        services.AddSingleton<IPublisher, Mediator>();

        var provider = services.BuildServiceProvider();
        var publisher = provider.GetRequiredService<IPublisher>();

        await publisher.Publish(new TestNotification("hello"));

        Assert.True(first.WasCalled);
        Assert.True(second.WasCalled);
    }
}
