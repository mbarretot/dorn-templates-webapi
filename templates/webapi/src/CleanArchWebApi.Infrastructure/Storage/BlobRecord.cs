#if (UseDatabaseBlobs && UseEfCore)
namespace CleanArchWebApi.Infrastructure.Storage;

internal sealed class BlobRecord
{
    public string Name { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public byte[] Content { get; set; } = [];
}
#endif
