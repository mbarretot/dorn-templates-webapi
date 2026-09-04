using CleanArchWebApi.Application.Common.Caching;

namespace CleanArchWebApi.Application.Tests.Common.Caching;

public sealed class TodoCacheKeysTests
{
    [Fact]
    public void All_ReturnsAStableKey()
    {
        Assert.Equal("todos:all", TodoCacheKeys.All());
    }

    [Fact]
    public void ById_ReturnsAKeyScopedToTheGivenId()
    {
        var id = Guid.NewGuid();

        Assert.Equal($"todos:{id}", TodoCacheKeys.ById(id));
    }

    [Fact]
    public void ById_ReturnsDifferentKeysForDifferentIds()
    {
        Assert.NotEqual(TodoCacheKeys.ById(Guid.NewGuid()), TodoCacheKeys.ById(Guid.NewGuid()));
    }
}
