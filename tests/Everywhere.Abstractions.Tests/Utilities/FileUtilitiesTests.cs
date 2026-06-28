using Everywhere.Utilities;

namespace Everywhere.Abstractions.Tests.Utilities;

[TestFixture]
public class FileUtilitiesTests
{
    [TestCase(".jpg", "image/jpeg")]
    [TestCase(".JPG", "image/jpeg")]
    [TestCase(".png", "image/png")]
    [TestCase(".gif", "image/gif")]
    [TestCase(".mp3", "audio/mpeg")]
    [TestCase(".mp4", "video/mp4")]
    [TestCase(".pdf", "application/pdf")]
    [TestCase(".zip", "application/zip")]
    [TestCase(".py", "text/x-python")]
    [TestCase(".cs", "text/x-csharp")]
    [TestCase(".ts", "text/typescript")]
    [TestCase(".json", "application/json")]
    public void KnownMimeTypes_ShouldContainExtension(string extension, string expectedMimeType)
    {
        Assert.That(FileUtilities.KnownMimeTypes.TryGetValue(extension, out var mimeType), Is.True);
        Assert.That(mimeType, Is.EqualTo(expectedMimeType));
    }

    [TestCase("image/jpeg", FileTypeCategory.Image)]
    [TestCase("audio/mpeg", FileTypeCategory.Audio)]
    [TestCase("video/mp4", FileTypeCategory.Video)]
    [TestCase("application/pdf", FileTypeCategory.Document)]
    [TestCase("application/zip", FileTypeCategory.Archive)]
    [TestCase("text/x-python", FileTypeCategory.Script)]
    [TestCase("application/octet-stream", FileTypeCategory.Binary)]
    public void IsOfCategory_ShouldMatchCorrectCategory(string mimeType, FileTypeCategory expectedCategory)
    {
        Assert.That(FileUtilities.IsOfCategory(mimeType, expectedCategory), Is.True);
    }

    [Test]
    public void IsOfCategory_WhenWrongCategory_ShouldReturnFalse()
    {
        Assert.That(FileUtilities.IsOfCategory("image/jpeg", FileTypeCategory.Audio), Is.False);
    }

    [Test]
    public void IsOfCategory_WhenUnknownMimeType_ShouldReturnFalse()
    {
        Assert.That(FileUtilities.IsOfCategory("unknown/type", FileTypeCategory.Image), Is.False);
    }

    [TestCase("image/jpeg", FileTypeCategory.Image)]
    [TestCase("application/octet-stream", FileTypeCategory.Binary)]
    public void GetCategory_ShouldReturnCorrectCategory(string mimeType, FileTypeCategory expected)
    {
        Assert.That(FileUtilities.GetCategory(mimeType), Is.EqualTo(expected));
    }

    [Test]
    public void GetCategory_WhenUnknown_ShouldDefaultToBinary()
    {
        Assert.That(FileUtilities.GetCategory("unknown/type"), Is.EqualTo(FileTypeCategory.Binary));
    }

    [Test]
    public void VerifyMimeType_WhenKnown_ShouldReturnIt()
    {
        var result = FileUtilities.VerifyMimeType("image/jpeg");
        Assert.That(result, Is.EqualTo("image/jpeg"));
    }

    [Test]
    public void VerifyMimeType_WhenUnknown_ShouldThrow()
    {
        Assert.Throws<NotSupportedException>(() => FileUtilities.VerifyMimeType("unknown/type"));
    }

    [Test]
    public void GetMimeTypesByCategory_ShouldReturnCorrectTypes()
    {
        var imageMimeTypes = FileUtilities.GetMimeTypesByCategory(FileTypeCategory.Image).ToList();

        using var _ = Assert.EnterMultipleScope();
        Assert.That(imageMimeTypes, Does.Contain("image/jpeg"));
        Assert.That(imageMimeTypes, Does.Contain("image/png"));
        Assert.That(imageMimeTypes, Does.Not.Contain("audio/mpeg"));
    }

    [Test]
    public void GetFileExtensionsByCategory_ShouldReturnCorrectExtensions()
    {
        var imageExtensions = FileUtilities.GetFileExtensionsByCategory(FileTypeCategory.Image).ToList();

        using var _ = Assert.EnterMultipleScope();
        Assert.That(imageExtensions, Does.Contain(".jpg"));
        Assert.That(imageExtensions, Does.Contain(".png"));
        Assert.That(imageExtensions, Does.Not.Contain(".mp3"));
    }

    [TestCase("image/jpeg", ".jpg")]
    [TestCase("image/png", ".png")]
    [TestCase("application/pdf", ".pdf")]
    public void GetExtensionByMimeType_ShouldReturnFirstMatchingExtension(string mimeType, string expectedExtension)
    {
        var result = FileUtilities.GetExtensionByMimeType(mimeType);
        Assert.That(result, Is.EqualTo(expectedExtension));
    }

    [Test]
    public void GetExtensionByMimeType_WhenUnknown_ShouldReturnNull()
    {
        var result = FileUtilities.GetExtensionByMimeType("unknown/type");
        Assert.That(result, Is.Null);
    }

    [TestCase(0L, "0 B")]
    [TestCase(512L, "512 B")]
    [TestCase(1024L, "1 KB")]
    [TestCase(1536L, "1.5 KB")]
    [TestCase(1048576L, "1 MB")]
    [TestCase(1073741824L, "1 GB")]
    [TestCase(1099511627776L, "1 TB")]
    public void HumanizeBytes_ShouldReturnCorrectString(long bytes, string expected)
    {
        Assert.That(FileUtilities.HumanizeBytes(bytes), Is.EqualTo(expected));
    }

    [Test]
    public void HumanizeBytes_ShouldCapAtTB()
    {
        // Very large value should still use TB
        var result = FileUtilities.HumanizeBytes(5L * 1099511627776L);
        Assert.That(result, Is.EqualTo("5 TB"));
    }

    [Test]
    public async Task DetectMimeTypeAsync_WithKnownExtension_ShouldReturnMimeType()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), "test_file.json");
        try
        {
            await File.WriteAllTextAsync(tempFile, "{}");
            var result = await FileUtilities.DetectMimeTypeAsync(tempFile);
            Assert.That(result, Is.EqualTo("application/json"));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Test]
    public async Task DetectMimeTypeAsync_WithUnknownExtension_TextContent_ShouldReturnTextPlain()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), "test_file.unknownext");
        try
        {
            await File.WriteAllTextAsync(tempFile, "Hello, this is plain text content.");
            var result = await FileUtilities.DetectMimeTypeAsync(tempFile);
            Assert.That(result, Is.EqualTo("text/plain"));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Test]
    public async Task DetectMimeTypeAsync_WithUnknownExtension_BinaryContent_ShouldReturnOctetStream()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), "test_file.unknownext2");
        try
        {
            await File.WriteAllBytesAsync(tempFile, [0x00, 0x01, 0x02, 0xFF, 0xFE, 0x80, 0x90, 0xA0]);
            var result = await FileUtilities.DetectMimeTypeAsync(tempFile);
            Assert.That(result, Is.EqualTo("application/octet-stream"));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
