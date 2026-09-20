#if (UseDatabaseBlobs && UseDapper)
using CleanArchWebApi.Application.Common.Storage;
using CleanArchWebApi.Infrastructure.Repositories.Dapper;
using Dapper;

namespace CleanArchWebApi.Infrastructure.Storage;

/// <summary>Keeps blobs in the Blobs table. A blob is read into memory whole, so this suits small files.</summary>
public sealed class DapperBlobStore : IBlobStore
{
    private readonly DapperContext _context;

    public DapperBlobStore(DapperContext context)
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
        var parameters = new
        {
            Name = name,
            ContentType = type,
            Content = buffer.ToArray(),
        };

        using var connection = _context.CreateConnection();
        var updated = await connection.ExecuteAsync(
            new CommandDefinition(
                "UPDATE Blobs SET ContentType = @ContentType, Content = @Content WHERE Name = @Name",
                parameters,
                cancellationToken: cancellationToken
            )
        );
        if (updated == 0)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    "INSERT INTO Blobs (Name, ContentType, Content) VALUES (@Name, @ContentType, @Content)",
                    parameters,
                    cancellationToken: cancellationToken
                )
            );
        }
    }

    public async Task<BlobContent?> OpenAsync(
        string name,
        CancellationToken cancellationToken = default
    )
    {
        BlobRules.EnsureValidName(name);

        using var connection = _context.CreateConnection();
        var blob = await connection.QueryFirstOrDefaultAsync<BlobRow>(
            new CommandDefinition(
                "SELECT ContentType, Content FROM Blobs WHERE Name = @Name",
                new { Name = name },
                cancellationToken: cancellationToken
            )
        );

        return blob is null
            ? null
            : new BlobContent(
                new MemoryStream(blob.Content, writable: false),
                blob.ContentType,
                blob.Content.Length
            );
    }

    public async Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        BlobRules.EnsureValidName(name);

        using var connection = _context.CreateConnection();
        var found = await connection.QueryFirstOrDefaultAsync<int?>(
            new CommandDefinition(
                "SELECT 1 FROM Blobs WHERE Name = @Name",
                new { Name = name },
                cancellationToken: cancellationToken
            )
        );
        return found is not null;
    }

    public async Task<bool> DeleteAsync(string name, CancellationToken cancellationToken = default)
    {
        BlobRules.EnsureValidName(name);

        using var connection = _context.CreateConnection();
        var deleted = await connection.ExecuteAsync(
            new CommandDefinition(
                "DELETE FROM Blobs WHERE Name = @Name",
                new { Name = name },
                cancellationToken: cancellationToken
            )
        );
        return deleted > 0;
    }

    private sealed class BlobRow
    {
        public string ContentType { get; set; } = string.Empty;

        public byte[] Content { get; set; } = [];
    }
}
#endif
