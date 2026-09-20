#if (UseBlobStorage)
using CleanArchWebApi.Application.Common.Storage;

namespace CleanArchWebApi.Application.Tests.Common.Storage;

public sealed class BlobRulesTests
{
    [Theory]
    [InlineData("report.pdf")]
    [InlineData("avatars/user-1.png")]
    [InlineData("a/b/c")]
    [InlineData("A_b-c.d")]
    [InlineData("v1.2/file.tar.gz")]
    public void EnsureValidName_WithAWellFormedName_Passes(string name)
    {
        BlobRules.EnsureValidName(name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("..")]
    [InlineData(".")]
    [InlineData("../secret")]
    [InlineData("a/../b")]
    [InlineData("a/..")]
    [InlineData("..\\secret")]
    [InlineData("/etc/passwd")]
    [InlineData("\\\\server\\share")]
    [InlineData("C:\\Windows\\win.ini")]
    [InlineData("C:/Windows/win.ini")]
    [InlineData("a\\b")]
    [InlineData("a//b")]
    [InlineData("a/")]
    [InlineData(".hidden")]
    [InlineData("a/.hidden")]
    [InlineData("trailing.")]
    [InlineData("a b")]
    [InlineData("name\n")]
    [InlineData("a\0b")]
    [InlineData("caf\u00e9")]
    [InlineData("%2e%2e/secret")]
    public void EnsureValidName_WithANameThatCouldBeAPath_Throws(string name)
    {
        Assert.Throws<ArgumentException>(() => BlobRules.EnsureValidName(name));
    }

    [Fact]
    public void EnsureValidName_WithANullName_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => BlobRules.EnsureValidName(null!));
    }

    [Fact]
    public void EnsureValidName_LimitsTheLength()
    {
        BlobRules.EnsureValidName(new string('a', BlobRules.MaxNameLength));

        Assert.Throws<ArgumentException>(
            () => BlobRules.EnsureValidName(new string('a', BlobRules.MaxNameLength + 1))
        );
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizeContentType_WithoutAValue_UsesTheDefault(string? contentType)
    {
        Assert.Equal(BlobRules.DefaultContentType, BlobRules.NormalizeContentType(contentType));
    }

    [Fact]
    public void NormalizeContentType_TrimsTheValue()
    {
        Assert.Equal("image/png", BlobRules.NormalizeContentType("  image/png "));
    }

    [Fact]
    public void NormalizeContentType_LimitsTheLength()
    {
        Assert.Throws<ArgumentException>(
            () => BlobRules.NormalizeContentType(new string('a', BlobRules.MaxContentTypeLength + 1))
        );
    }
}
#endif
