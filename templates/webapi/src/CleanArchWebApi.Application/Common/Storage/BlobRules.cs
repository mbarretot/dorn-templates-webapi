#if (UseBlobStorage)
namespace CleanArchWebApi.Application.Common.Storage;

/// <summary>The naming and content-type rules every <see cref="IBlobStore"/> implementation applies.</summary>
public static class BlobRules
{
    public const int MaxNameLength = 255;

    public const int MaxContentTypeLength = 255;

    public const string DefaultContentType = "application/octet-stream";

    /// <summary>
    /// A name is one or more segments separated by '/'. A segment is ASCII letters, digits, '.', '_' or '-', is never empty
    /// and never starts or ends with '.', which rules out '..', rooted paths, drive letters, backslashes and hidden files.
    /// </summary>
    public static void EnsureValidName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        if (name.Length is 0 or > MaxNameLength)
        {
            throw new ArgumentException(
                $"A blob name must be 1 to {MaxNameLength} characters long.",
                nameof(name)
            );
        }

        foreach (var segment in name.Split('/'))
        {
            if (
                segment.Length == 0
                || segment[0] == '.'
                || segment[^1] == '.'
                || !segment.All(IsNameCharacter)
            )
            {
                throw new ArgumentException(
                    "A blob name is made of segments separated by '/'. Each segment uses only letters, digits, '.', '_' and '-', "
                        + "and never starts or ends with '.'.",
                    nameof(name)
                );
            }
        }
    }

    public static string NormalizeContentType(string? contentType)
    {
        var trimmed = contentType?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return DefaultContentType;
        }

        if (trimmed.Length > MaxContentTypeLength)
        {
            throw new ArgumentException(
                $"A content type can be at most {MaxContentTypeLength} characters long.",
                nameof(contentType)
            );
        }

        return trimmed;
    }

    private static bool IsNameCharacter(char character) =>
        char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or '-';
}
#endif
