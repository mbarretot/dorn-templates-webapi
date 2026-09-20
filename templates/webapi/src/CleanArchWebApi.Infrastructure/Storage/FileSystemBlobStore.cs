#if (UseFileSystemBlobs)
using CleanArchWebApi.Application.Common.Storage;

namespace CleanArchWebApi.Infrastructure.Storage;

/// <summary>
/// Stores each blob as a file under one root directory. The name is validated by <see cref="BlobRules"/> and the resulting
/// path is checked to stay inside the root, so a name can never address anything outside it. Content types live in
/// <c>.meta</c> and in-flight uploads in <c>.tmp</c>; no valid name starts with '.', so they never collide with a blob.
/// </summary>
public sealed class FileSystemBlobStore : IBlobStore
{
    private const string MetaDirectory = ".meta";
    private const string TempDirectory = ".tmp";

    private readonly string _root;

    public FileSystemBlobStore(string rootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);

        _root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(rootPath));
    }

    public async Task SaveAsync(
        string name,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default
    )
    {
        var path = Resolve(_root, name);
        var metaPath = Resolve(Path.Combine(_root, MetaDirectory), name);
        var type = BlobRules.NormalizeContentType(contentType);
        var tempPath = Path.Combine(_root, TempDirectory, Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(Path.GetDirectoryName(tempPath)!);
        try
        {
            await using (
                var target = new FileStream(
                    tempPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 4096,
                    useAsync: true
                )
            )
            {
                await content.CopyToAsync(target, cancellationToken);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.Move(tempPath, path, overwrite: true);
        }
        finally
        {
            File.Delete(tempPath);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(metaPath)!);
        await File.WriteAllTextAsync(metaPath, type, cancellationToken);
    }

    public async Task<BlobContent?> OpenAsync(
        string name,
        CancellationToken cancellationToken = default
    )
    {
        var path = Resolve(_root, name);
        var metaPath = Resolve(Path.Combine(_root, MetaDirectory), name);
        if (!File.Exists(path))
        {
            return null;
        }

        FileStream stream;
        try
        {
            stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read | FileShare.Delete,
                bufferSize: 4096,
                useAsync: true
            );
        }
        catch (Exception exception)
            when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            return null;
        }

        var contentType = File.Exists(metaPath)
            ? BlobRules.NormalizeContentType(
                await File.ReadAllTextAsync(metaPath, cancellationToken)
            )
            : BlobRules.DefaultContentType;
        return new BlobContent(stream, contentType, stream.Length);
    }

    public Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default) =>
        Task.FromResult(File.Exists(Resolve(_root, name)));

    public Task<bool> DeleteAsync(string name, CancellationToken cancellationToken = default)
    {
        var path = Resolve(_root, name);
        var metaPath = Resolve(Path.Combine(_root, MetaDirectory), name);

        if (!File.Exists(path))
        {
            return Task.FromResult(false);
        }

        File.Delete(path);
        File.Delete(metaPath);
        return Task.FromResult(true);
    }

    private static string Resolve(string directory, string name)
    {
        BlobRules.EnsureValidName(name);

        var path = Path.GetFullPath(Path.Combine(directory, name));
        if (!path.StartsWith(directory + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The blob name resolves outside the storage root.",
                nameof(name)
            );
        }

        return path;
    }
}
#endif
