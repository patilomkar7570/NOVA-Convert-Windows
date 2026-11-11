using NovaConvert.Core.Models;
using NovaConvert.Core.Services;
using Xunit;

namespace NovaConvert.Tests;

public class FormatRegistryTests
{
    private readonly FormatRegistry _registry = new();

    [Fact]
    public void CanConvert_RejectsIncompatibleCrossCategories()
    {
        // Video to Spreadsheet
        Assert.False(_registry.CanConvert("mp4", "xlsx"));
        // PDF to MP3
        Assert.False(_registry.CanConvert("pdf", "mp3"));
        // Archive to Audio
        Assert.False(_registry.CanConvert("zip", "flac"));
    }

    [Fact]
    public void CanConvert_AllowsValidTargetFormats()
    {
        Assert.True(_registry.CanConvert("png", "jpg"));
        Assert.True(_registry.CanConvert("png", "webp"));
        Assert.True(_registry.CanConvert("wav", "mp3"));
        Assert.True(_registry.CanConvert("mp4", "mkv"));
        Assert.True(_registry.CanConvert("mp4", "mp3")); // Audio extraction
        Assert.True(_registry.CanConvert("docx", "txt"));
        Assert.True(_registry.CanConvert("xlsx", "csv"));
        Assert.True(_registry.CanConvert("zip", "tar"));
    }

    [Fact]
    public void GetCompatibleTargets_ReturnsOnlyGenuineMatches()
    {
        var png = _registry.GetFormatById("png");
        Assert.NotNull(png);

        var targets = _registry.GetCompatibleTargets(png);
        Assert.NotEmpty(targets);
        Assert.Contains(targets, t => t.Id == "jpg");
        Assert.Contains(targets, t => t.Id == "webp");
        Assert.DoesNotContain(targets, t => t.Category == FileCategory.Spreadsheet);
        Assert.DoesNotContain(targets, t => t.Category == FileCategory.Video);
    }
}
