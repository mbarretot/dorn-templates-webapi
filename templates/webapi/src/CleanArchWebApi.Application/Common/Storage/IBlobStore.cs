#if (UseBlobStorage)
namespace CleanArchWebApi.Application.Common.Storage;

/// <summary>
/// Keeps binary content under a name. Every member throws <see cref="ArgumentException"/> for a name
/// that <see cref="BlobRules.EnsureValidName"/> rejects, so a caller-supplied name never reaches a path or a query unchecked.
/// </summary>
public interface IBlobStore
{
    /// <summary>Creates the blob, or replaces it when the name is already taken.</summary>
    Task SaveAsync(
        string name,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default
    );

    /// <summary>Returns null when there is no such blob. The caller disposes the returned stream.</summary>
    Task<BlobContent?> OpenAsync(string name, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>Returns false when there was nothing to delete.</summary>
    Task<bool> DeleteAsync(string name, CancellationToken cancellationToken = default);
}

public sealed record BlobContent(Stream Content, string ContentType, long Size);
#endif
