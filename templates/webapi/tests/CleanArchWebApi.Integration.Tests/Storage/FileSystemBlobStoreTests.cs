#if (UseFileSystemBlobs)
using System.Text;
using CleanArchWebApi.Application.Common.Storage;
using CleanArchWebApi.Infrastructure.Storage;

namespace CleanArchWebApi.Integration.Tests.Storage;

/// <summary>The real file system store, in a throwaway directory. Needs no external service.</summary>
public sealed class FileSystemBlobStoreTests : IDisposable
{
    // The store root is a child of a directory the test owns, so a name that escapes the root
    // lands somewhere this test can see and assert on.
    private readonly string _sandbox = Path.Combine(
        Path.GetTempPath(),
        $"blobs-{Guid.NewGuid():N}"
    );
    private readonly FileSystemBlobStore _store;

    public FileSystemBlobStoreTests()
    {
        Directory.CreateDirectory(_sandbox);
        _store = new FileSystemBlobStore(Root);
    }

    private string Root => Path.Combine(_sandbox, "root");

    public void Dispose()
    {
        if (Directory.Exists(_sandbox))
        {
            Directory.Delete(_sandbox, recursive: true);
        }
    }

    private static MemoryStream Bytes(string text) => new(Encoding.UTF8.GetBytes(text));

    private static async Task<string> ReadAsync(BlobContent blob)
    {
        await using var content = blob.Content;
        using var reader = new StreamReader(content, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }

    [Fact]
    public async Task Save_ThenOpen_ReturnsTheContentTypeAndSize()
    {
        await _store.SaveAsync("notes/today.txt", Bytes("hello"), "text/plain");

        var blob = await _store.OpenAsync("notes/today.txt");

        Assert.NotNull(blob);
        Assert.Equal("text/plain", blob.ContentType);
        Assert.Equal(5, blob.Size);
        Assert.Equal("hello", await ReadAsync(blob));
        Assert.True(File.Exists(Path.Combine(Root, "notes", "today.txt")));
    }

    [Fact]
    public async Task Save_WithAnExistingName_ReplacesTheContentAndTheContentType()
    {
        await _store.SaveAsync("file.bin", Bytes("first"), "text/plain");
        await _store.SaveAsync("file.bin", Bytes("second version"), "application/json");

        var blob = await _store.OpenAsync("file.bin");

        Assert.NotNull(blob);
        Assert.Equal("application/json", blob.ContentType);
        Assert.Equal("second version", await ReadAsync(blob));
    }

    [Fact]
    public async Task Save_WithoutAContentType_UsesTheDefault()
    {
        await _store.SaveAsync("file.bin", Bytes("x"), " ");

        var blob = await _store.OpenAsync("file.bin");

        Assert.NotNull(blob);
        await using var content = blob.Content;
        Assert.Equal("application/octet-stream", blob.ContentType);
    }

    [Fact]
    public async Task Open_WithAnUnknownName_ReturnsNull()
    {
        Assert.Null(await _store.OpenAsync("missing.txt"));
    }

    [Fact]
    public async Task ExistsAndDelete_FollowTheBlobLifecycle()
    {
        Assert.False(await _store.ExistsAsync("file.txt"));
        Assert.False(await _store.DeleteAsync("file.txt"));

        await _store.SaveAsync("file.txt", Bytes("x"), "text/plain");

        Assert.True(await _store.ExistsAsync("file.txt"));
        Assert.True(await _store.DeleteAsync("file.txt"));
        Assert.False(await _store.ExistsAsync("file.txt"));
        Assert.Null(await _store.OpenAsync("file.txt"));
        Assert.False(await _store.DeleteAsync("file.txt"));
    }

    [Fact]
    public async Task Save_LeavesNoUploadInFlightBehind()
    {
        await _store.SaveAsync("file.txt", Bytes("x"), "text/plain");

        Assert.Empty(Directory.EnumerateFileSystemEntries(Path.Combine(Root, ".tmp")));
    }

    public static TheoryData<string> NamesThatMustNotEscapeTheRoot()
    {
        var sandbox = Path.Combine(Path.GetTempPath(), "outside-the-root");
        return
        [
            "../escape.txt",
            "../../escape.txt",
            "sub/../../escape.txt",
            "sub/../..",
            "..\\escape.txt",
            "sub\\..\\..\\escape.txt",
            "%2e%2e/escape.txt",
            "/escape.txt",
            "\\escape.txt",
            "//escape.txt",
            "C:\\escape.txt",
            "C:/escape.txt",
            "~/escape.txt",
            Path.Combine(sandbox, "escape.txt"),
            "..",
            ".",
            ".meta/escape.txt",
            ".tmp/escape.txt",
        ];
    }

    [Theory]
    [MemberData(nameof(NamesThatMustNotEscapeTheRoot))]
    public async Task EveryOperation_WithANameThatCouldEscapeTheRoot_ThrowsAndTouchesNothing(
        string name
    )
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _store.SaveAsync(name, Bytes("pwned"), "text/plain")
        );
        await Assert.ThrowsAsync<ArgumentException>(() => _store.OpenAsync(name));
        await Assert.ThrowsAsync<ArgumentException>(() => _store.ExistsAsync(name));
        await Assert.ThrowsAsync<ArgumentException>(() => _store.DeleteAsync(name));

        Assert.Empty(Directory.EnumerateFiles(_sandbox, "*", SearchOption.AllDirectories));
        Assert.False(File.Exists(Path.Combine(Path.GetTempPath(), "escape.txt")));
    }

    [Fact]
    public async Task Delete_WithANameThatCouldEscapeTheRoot_LeavesAFileOutsideTheRootAlone()
    {
        var outside = Path.Combine(_sandbox, "outside.txt");
        await File.WriteAllTextAsync(outside, "keep me");

        await Assert.ThrowsAsync<ArgumentException>(() => _store.DeleteAsync("../outside.txt"));
        await Assert.ThrowsAsync<ArgumentException>(() => _store.OpenAsync("../outside.txt"));
        await Assert.ThrowsAsync<ArgumentException>(() => _store.DeleteAsync(outside));
        await Assert.ThrowsAsync<ArgumentException>(() => _store.OpenAsync(outside));

        Assert.Equal("keep me", await File.ReadAllTextAsync(outside));
    }

    [Fact]
    public void Constructor_WithoutARootPath_Throws()
    {
        Assert.Throws<ArgumentException>(() => new FileSystemBlobStore(" "));
    }
}
#endif
