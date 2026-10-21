using ServiceDesk.Application.Tickets;

namespace ServiceDesk.UnitTests;

public class AttachmentRulesTests
{
    [Theory]
    [InlineData(".pdf", "application/pdf")]
    [InlineData(".PNG", "image/png")]
    [InlineData(".jpg", "image/jpeg")]
    [InlineData(".txt", "text/plain")]
    public void Whitelisted_extensions_get_a_server_side_content_type(string extension, string expected)
    {
        Assert.True(AttachmentRules.TryGetContentType(extension, out var contentType));
        Assert.Equal(expected, contentType);
    }

    [Theory]
    [InlineData(".exe")]
    [InlineData(".bat")]
    [InlineData(".ps1")]
    [InlineData(".html")]
    [InlineData(".svg")]
    [InlineData("")]
    public void Dangerous_or_unknown_extensions_are_rejected(string extension)
        => Assert.False(AttachmentRules.TryGetContentType(extension, out _));

    [Fact]
    public void Real_file_signatures_are_accepted()
    {
        Assert.True(AttachmentRules.SignatureMatches(".pdf", new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D }));
        Assert.True(AttachmentRules.SignatureMatches(".png", new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D }));
        Assert.True(AttachmentRules.SignatureMatches(".jpg", new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }));
        Assert.True(AttachmentRules.SignatureMatches(".gif", new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39 }));
        Assert.True(AttachmentRules.SignatureMatches(".docx", new byte[] { 0x50, 0x4B, 0x03, 0x04 }));
        Assert.True(AttachmentRules.SignatureMatches(".txt", "hello world"u8.ToArray()));
    }

    [Fact]
    public void A_file_pretending_to_be_another_type_is_rejected()
    {
        // an executable (MZ header) renamed to .png / .pdf
        var exe = new byte[] { 0x4D, 0x5A, 0x90, 0x00 };

        Assert.False(AttachmentRules.SignatureMatches(".png", exe));
        Assert.False(AttachmentRules.SignatureMatches(".pdf", exe));
        Assert.False(AttachmentRules.SignatureMatches(".txt", new byte[] { 0x4D, 0x00, 0x5A }));
    }

    [Fact]
    public void Too_short_content_does_not_match_a_binary_signature()
        => Assert.False(AttachmentRules.SignatureMatches(".png", new byte[] { 0x89 }));

    [Theory]
    [InlineData("..\\..\\evil.png", "evil.png")]
    [InlineData("../../etc/passwd.txt", "passwd.txt")]
    [InlineData("C:\\temp\\report.pdf", "report.pdf")]
    [InlineData("  notes.txt  ", "notes.txt")]
    public void File_names_lose_any_path_part(string input, string expected)
        => Assert.Equal(expected, AttachmentRules.SanitizeFileName(input));

    [Fact]
    public void Long_file_names_are_shortened_but_keep_their_extension()
    {
        var name = new string('a', 400) + ".pdf";

        var cleaned = AttachmentRules.SanitizeFileName(name);

        Assert.True(cleaned.Length <= AttachmentRules.MaxFileNameLength);
        Assert.EndsWith(".pdf", cleaned);
    }

    [Fact]
    public void An_empty_name_stays_empty()
        => Assert.Equal(string.Empty, AttachmentRules.SanitizeFileName(null));
}
