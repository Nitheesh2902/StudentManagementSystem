namespace ServiceDesk.Application.Tickets;

/// <summary>Whitelist, size limits and basic content checks for uploaded files.</summary>
public static class AttachmentRules
{
    public const long MaxBytes = 5 * 1024 * 1024;
    public const int MaxPerTicket = 10;
    public const int MaxFileNameLength = 150;

    private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = "application/pdf",
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".gif"] = "image/gif",
        [".txt"] = "text/plain",
        [".log"] = "text/plain",
        [".csv"] = "text/csv",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
    };

    public static IReadOnlyCollection<string> AllowedExtensions => ContentTypes.Keys;

    /// <summary>The content type is decided by the extension, never taken from the client.</summary>
    public static bool TryGetContentType(string extension, out string contentType)
    {
        if (ContentTypes.TryGetValue(extension ?? string.Empty, out var found))
        {
            contentType = found;
            return true;
        }

        contentType = string.Empty;
        return false;
    }

    /// <summary>Removes any path, control and invalid characters and limits the length.</summary>
    public static string SanitizeFileName(string? fileName)
    {
        var name = Path.GetFileName((fileName ?? string.Empty).Replace('\\', '/'));
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(name.Where(c => !char.IsControl(c) && !invalid.Contains(c)).ToArray()).Trim();

        if (cleaned.Length > MaxFileNameLength)
        {
            var ext = Path.GetExtension(cleaned);
            var stem = Path.GetFileNameWithoutExtension(cleaned);
            var keep = Math.Max(1, MaxFileNameLength - ext.Length);
            cleaned = (stem.Length > keep ? stem[..keep] : stem) + ext;
        }

        return cleaned;
    }

    /// <summary>Checks that the first bytes of the file look like the type its extension claims.</summary>
    public static bool SignatureMatches(string extension, ReadOnlySpan<byte> head)
    {
        switch ((extension ?? string.Empty).ToLowerInvariant())
        {
            case ".pdf":
                return StartsWith(head, 0x25, 0x50, 0x44, 0x46);              // %PDF
            case ".png":
                return StartsWith(head, 0x89, 0x50, 0x4E, 0x47);
            case ".jpg":
            case ".jpeg":
                return StartsWith(head, 0xFF, 0xD8, 0xFF);
            case ".gif":
                return StartsWith(head, 0x47, 0x49, 0x46, 0x38);              // GIF8
            case ".docx":
            case ".xlsx":
                return StartsWith(head, 0x50, 0x4B);                          // zip container
            case ".txt":
            case ".log":
            case ".csv":
                return !head.Contains((byte)0);                               // text has no NUL bytes
            default:
                return false;
        }
    }

    private static bool StartsWith(ReadOnlySpan<byte> head, params byte[] signature)
    {
        if (head.Length < signature.Length) return false;
        for (var i = 0; i < signature.Length; i++)
            if (head[i] != signature[i]) return false;
        return true;
    }
}
