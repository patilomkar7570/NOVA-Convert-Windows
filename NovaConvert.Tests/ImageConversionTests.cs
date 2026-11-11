using ImageMagick;
using NovaConvert.Core.Models;
using NovaConvert.Core.Services;
using NovaConvert.Engines;
using Xunit;

namespace NovaConvert.Tests;

public class ImageConversionTests : IDisposable
{
    private readonly string _testDir;
    private readonly MagickImageEngine _engine;
    private readonly FormatRegistry _registry;

    public ImageConversionTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "NOVA_Test_Images_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
        _engine = new MagickImageEngine();
        _registry = new FormatRegistry();
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDir)) Directory.Delete(_testDir, true);
        }
        catch { }
    }

    private string CreateSamplePng(int width = 100, int height = 100, bool withAlpha = true)
    {
        var path = Path.Combine(_testDir, $"sample_{Guid.NewGuid():N}.png");
        using var image = new MagickImage(withAlpha ? MagickColors.Transparent : MagickColors.Blue, (uint)width, (uint)height);
        using var circle = new MagickImage(MagickColors.Indigo, (uint)(width / 2), (uint)(height / 2));
        image.Composite(circle, (int)(width / 4), (int)(height / 4), CompositeOperator.Over);
        image.Write(path);
        return path;
    }

    [Fact]
    public async Task ConvertAsync_PngToJpg_SucceedsAndRemovesAlphaCleanly()
    {
        var srcPng = CreateSamplePng(120, 120, true);
        var targetJpg = Path.Combine(_testDir, "output.jpg");
        var srcFmt = _registry.GetFormatById("png")!;
        var tgtFmt = _registry.GetFormatById("jpg")!;

        var result = await _engine.ConvertAsync(srcPng, targetJpg, srcFmt, tgtFmt, ConversionOptions.Default);

        Assert.True(result.Success);
        Assert.True(File.Exists(targetJpg));
        Assert.True(result.OutputSizeBytes > 0);

        // Verify output image using MagickImage
        using var readBack = new MagickImage(targetJpg);
        Assert.Equal(MagickFormat.Jpeg, readBack.Format);
        Assert.Equal(120u, readBack.Width);
        Assert.Equal(120u, readBack.Height);
        Assert.False(readBack.HasAlpha);
    }

    [Fact]
    public async Task ConvertAsync_PngToWebp_PreservesTransparency()
    {
        var srcPng = CreateSamplePng(80, 80, true);
        var targetWebp = Path.Combine(_testDir, "output.webp");
        var srcFmt = _registry.GetFormatById("png")!;
        var tgtFmt = _registry.GetFormatById("webp")!;

        var result = await _engine.ConvertAsync(srcPng, targetWebp, srcFmt, tgtFmt, ConversionOptions.Default);

        Assert.True(result.Success);
        Assert.True(File.Exists(targetWebp));

        using var readBack = new MagickImage(targetWebp);
        Assert.Equal(MagickFormat.WebP, readBack.Format);
        Assert.True(readBack.HasAlpha);
    }

    [Fact]
    public async Task ConvertAsync_AppliesResizeCorrectly()
    {
        var srcPng = CreateSamplePng(200, 200);
        var targetPng = Path.Combine(_testDir, "resized.png");
        var srcFmt = _registry.GetFormatById("png")!;

        var options = new ConversionOptions
        {
            ResizeWidth = 50,
            ResizeHeight = 50,
            PreserveAspectRatio = true
        };

        var result = await _engine.ConvertAsync(srcPng, targetPng, srcFmt, srcFmt, options);

        Assert.True(result.Success);
        using var readBack = new MagickImage(targetPng);
        Assert.Equal(50u, readBack.Width);
        Assert.Equal(50u, readBack.Height);
    }

    [Fact]
    public async Task ConvertAsync_QualityPreset_ReducesFileSizeForSmallerFile()
    {
        var path = Path.Combine(_testDir, $"noisy_{Guid.NewGuid():N}.png");
        // Create an image with varied gradient/pattern
        using (var img = new MagickImage(MagickColors.Red, 256, 256))
        {
            img.AddNoise(NoiseType.Gaussian);
            img.Write(path);
        }

        var highJpg = Path.Combine(_testDir, "high.jpg");
        var smallJpg = Path.Combine(_testDir, "small.jpg");
        var srcFmt = _registry.GetFormatById("png")!;
        var tgtFmt = _registry.GetFormatById("jpg")!;

        var resHigh = await _engine.ConvertAsync(path, highJpg, srcFmt, tgtFmt, new ConversionOptions { Preset = QualityPreset.BestQuality });
        var resSmall = await _engine.ConvertAsync(path, smallJpg, srcFmt, tgtFmt, new ConversionOptions { Preset = QualityPreset.SmallerFile });

        Assert.True(resHigh.Success);
        Assert.True(resSmall.Success);
        Assert.True(resSmall.OutputSizeBytes < resHigh.OutputSizeBytes);
    }
}
