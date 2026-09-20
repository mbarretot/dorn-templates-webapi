#if (UseDatabaseBlobs && UseEfCore)
using CleanArchWebApi.Application.Common.Storage;

namespace CleanArchWebApi.Infrastructure.Storage;

/// <summary>Keeps blobs in the Blobs table. A blob is read into memory whole, so this suits small files.</summary>
public sealed class EfCoreBlobStore : IBlobStore
{
    private readonly ApplicationDbContext _context;

    public EfCoreBlobStore(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task SaveAsync(
        string name,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default
    )
    {
        BlobRules.EnsureValidName(name);
        var type = BlobRules.NormalizeContentType(contentType);

        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();

        var blobs = _context.Set<BlobRecord>();
        var updated = await blobs
            .Where(blob => blob.Name == name)
            .ExecuteUpdateAsync(
                setters =>
                    setters
                        .SetProperty(blob => blob.ContentType, type)
                        .SetProperty(blob => blob.Content, bytes),
                cancellationToken
            );
        if (updated == 0)
        {
            blobs.Add(
                new BlobRecord
                {
                    Name = name,
                    ContentType = type,
                    Content = bytes,
                }
            );
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<BlobContent?> OpenAsync(
        string name,
        CancellationToken cancellationToken = default
    )
    {
        BlobRules.EnsureValidName(name);

        var blob = await _context
            .Set<BlobRecord>()
            .AsNoTracking()
            .Where(blob => blob.Name == name)
            .Select(blob => new { blob.ContentType, blob.Content })
            .FirstOrDefaultAsync(cancellationToken);

        return blob is null
            ? null
            : new BlobContent(
                new MemoryStream(blob.Content, writable: false),
                blob.ContentType,
                blob.Content.Length
            );
    }

    public Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        BlobRules.EnsureValidName(name);

        return _context.Set<BlobRecord>().AnyAsync(blob => blob.Name == name, cancellationToken);
    }

    public async Task<bool> DeleteAsync(string name, CancellationToken cancellationToken = default)
    {
        BlobRules.EnsureValidName(name);

        var deleted = await _context
            .Set<BlobRecord>()
            .Where(blob => blob.Name == name)
            .ExecuteDeleteAsync(cancellationToken);
        return deleted > 0;
    }
}
#endif
