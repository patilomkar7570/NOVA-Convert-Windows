using NovaConvert.Core.Models;
using NovaConvert.Core.Services;
using Xunit;

namespace NovaConvert.Tests;

public class FileSignatureAndInspectionTests
{
    private readonly FormatRegistry _registry = new();

    [Fact]
    public void IdentifyBySignature_DetectsPngHeaderAccurately()
    {
        byte[] pngHeader = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00 };
        var format = _registry.IdentifyBySignature(pngHeader, ".png");
        Assert.NotNull(format);
        Assert.Equal("png", format.Id);
        Assert.Equal(FileCategory.Image, format.Category);
    }

    [Fact]
    public void IdentifyBySignature_DetectsJpegHeaderAccurately()
    {
        byte[] jpegHeader = { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46 };
        var format = _registry.IdentifyBySignature(jpegHeader, ".jpg");
        Assert.NotNull(format);
        Assert.Equal("jpg", format.Id);
    }

    [Fact]
    public void IdentifyBySignature_DetectsPdfHeaderAccurately()
    {
        byte[] pdfHeader = System.Text.Encoding.ASCII.GetBytes("%PDF-1.7 standard header");
        var format = _registry.IdentifyBySignature(pdfHeader, ".pdf");
        Assert.NotNull(format);
        Assert.Equal("pdf", format.Id);
        Assert.Equal(FileCategory.Pdf, format.Category);
    }

    [Fact]
    public void IdentifyBySignature_DetectsFlacHeaderAccurately()
    {
        byte[] flacHeader = { 0x66, 0x4C, 0x61, 0x43, 0x00, 0x00 };
        var format = _registry.IdentifyBySignature(flacHeader, ".flac");
        Assert.NotNull(format);
        Assert.Equal("flac", format.Id);
        Assert.Equal(FileCategory.Audio, format.Category);
    }

    [Fact]
    public void IdentifyBySignature_Detects7zHeaderAccurately()
    {
        byte[] szHeader = { 0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C };
        var format = _registry.IdentifyBySignature(szHeader, ".7z");
        Assert.NotNull(format);
        Assert.Equal("7z", format.Id);
        Assert.Equal(FileCategory.Archive, format.Category);
    }

    [Fact]
    public async Task InspectFileAsync_DetectsExtensionMismatch_WhenPngIsRenamedToJpg()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"mismatch_test_{Guid.NewGuid():N}.jpg");
        try
        {
            // Write real PNG signature to a file named .jpg
            byte[] fakeJpgRealPng = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52 };
            await File.WriteAllBytesAsync(tempFile, fakeJpgRealPng);

            var inspector = new FileInspectionService(_registry);
            var result = await inspector.InspectFileAsync(tempFile);

            Assert.NotNull(result);
            Assert.True(result.HasExtensionMismatch);
            Assert.NotNull(result.DetectedFormat);
            Assert.Equal("png", result.DetectedFormat.Id);
            Assert.NotNull(result.MismatchWarning);
            Assert.Contains("PNG", result.MismatchWarning);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task InspectFileAsync_HandlesNonExistentFileSafely()
    {
        var nonExistent = Path.Combine(Path.GetTempPath(), "does_not_exist_12345.xyz");
        var inspector = new FileInspectionService(_registry);
        var result = await inspector.InspectFileAsync(nonExistent);

        Assert.NotNull(result);
        Assert.True(result.IsCorruptedOrInvalid);
        Assert.NotNull(result.ErrorMessage);
    }
}
