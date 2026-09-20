#if (UseBlobStorage)
using System.Text;
using CleanArchWebApi.Application.Common.Storage;
#if (UseFileSystemBlobs)
using CleanArchWebApi.Infrastructure.Storage;
#endif
using Microsoft.Extensions.DependencyInjection;

namespace CleanArchWebApi.Functional.Tests;

/// <summary>The blob store as the running host wires it: dependency injection, configuration, and the schema created at startup.</summary>
public sealed class BlobStorageTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public BlobStorageTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task BlobStore_ResolvesFromTheHost_AndRoundTripsABlob()
    {
        using var scope = _factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IBlobStore>();

        await store.SaveAsync(
            "functional/hello.txt",
            new MemoryStream(Encoding.UTF8.GetBytes("hello")),
            "text/plain"
        );

        var blob = await store.OpenAsync("functional/hello.txt");
        Assert.NotNull(blob);
        await using (var content = blob.Content)
        {
            using var reader = new StreamReader(content, Encoding.UTF8);
            Assert.Equal("hello", await reader.ReadToEndAsync());
        }

        Assert.Equal("text/plain", blob.ContentType);
        Assert.Equal(5, blob.Size);
        Assert.True(await store.ExistsAsync("functional/hello.txt"));
        Assert.True(await store.DeleteAsync("functional/hello.txt"));
        Assert.False(await store.ExistsAsync("functional/hello.txt"));
    }

    [Fact]
    public async Task BlobStore_RejectsANameThatCouldBeAPath()
    {
        using var scope = _factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IBlobStore>();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            store.SaveAsync("../escape.txt", new MemoryStream(new byte[] { 1 }), "text/plain")
        );
    }
#if (UseFileSystemBlobs)

    [Fact]
    public async Task BlobStore_WritesUnderTheConfiguredRoot()
    {
        using var scope = _factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IBlobStore>();

        Assert.IsType<FileSystemBlobStore>(store);
        await store.SaveAsync("configured/root.txt", new MemoryStream(new byte[] { 1 }), "text/plain");

        Assert.True(File.Exists(Path.Combine(_factory.BlobRoot, "configured", "root.txt")));
    }
#endif
}
#endif
