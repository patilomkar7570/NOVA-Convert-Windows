using ImageMagick;
using NovaConvert.Core.Interfaces;
using NovaConvert.Core.Models;
using System.Diagnostics;

namespace NovaConvert.Engines;

public class MagickImageEngine : IConversionEngine
{
    public string EngineId => "imagemagick";
    public string DisplayName => "ImageMagick Engine";
    public string Version => MagickNET.Version;
    public string License => "ImageMagick License (Apache 2.0 compatible)";
    public bool IsAvailable => true;
    public string StatusSummary => "Ready (Embedded Magick.NET Q8)";
    public IReadOnlyList<FileCategory> SupportedCategories => new[] { FileCategory.Image, FileCategory.Pdf };

    private static readonly HashSet<string> SupportedImageFormats = new(StringComparer.OrdinalIgnoreCase)
    {
        "png", "jpg", "jpeg", "webp", "tiff", "tif", "bmp", "gif", "ico", "avif", "svg", "tga", "psd"
    };

    public bool CanConvert(FormatDefinition source, FormatDefinition target)
    {
        if (source.Category == FileCategory.Image && target.Category == FileCategory.Image)
        {
            return SupportedImageFormats.Contains(source.Id) && SupportedImageFormats.Contains(target.Id);
        }

        // Image to PDF
        if (source.Category == FileCategory.Image && target.Id.Equals("pdf", StringComparison.OrdinalIgnoreCase))
        {
            return SupportedImageFormats.Contains(source.Id);
        }

        // PDF to Image
        if (source.Id.Equals("pdf", StringComparison.OrdinalIgnoreCase) && target.Category == FileCategory.Image)
        {
            return SupportedImageFormats.Contains(target.Id);
        }

        return false;
    }

    public async Task<ConversionResult> ConvertAsync(
        string sourcePath,
        string targetPath,
        FormatDefinition source,
        FormatDefinition target,
        ConversionOptions options,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        progress?.Report(0.1);

        try
        {
            await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var readSettings = new MagickReadSettings();
                if (source.Id.Equals("pdf", StringComparison.OrdinalIgnoreCase))
                {
                    readSettings.Density = new Density(options.PdfDpi ?? 300);
                }

                using var image = new MagickImage(sourcePath, readSettings);
                cancellationToken.ThrowIfCancellationRequested();
                progress?.Report(0.4);

                // Handle Resizing
                if (options.ResizeWidth.HasValue && options.ResizeWidth.Value > 0 ||
                    options.ResizeHeight.HasValue && options.ResizeHeight.Value > 0)
                {
                    uint w = (uint)(options.ResizeWidth ?? 0);
                    uint h = (uint)(options.ResizeHeight ?? 0);
                    var geo = new MagickGeometry(w, h)
                    {
                        IgnoreAspectRatio = !options.PreserveAspectRatio
                    };
                    image.Resize(geo);
                }

                // Handle Quality
                if (options.Preset == QualityPreset.Lossless || options.Lossless)
                {
                    if (target.SupportsLossless)
                    {
                        image.Quality = 100;
                    }
                }
                else if (options.Preset == QualityPreset.BestQuality)
                {
                    image.Quality = 98;
                }
                else if (options.Preset == QualityPreset.Balanced)
                {
                    image.Quality = 80;
                }
                else if (options.Preset == QualityPreset.SmallerFile)
                {
                    image.Quality = 35;
                }
                else if (options.QualityValue.HasValue)
                {
                    image.Quality = (uint)Math.Clamp(options.QualityValue.Value, 1, 100);
                }

                // Handle Metadata
                if (!options.PreserveMetadata)
                {
                    image.Strip();
                }

                // Handle Transparency for formats without alpha channel (like JPEG)
                if (!target.SupportsTransparency && image.HasAlpha)
                {
                    image.ColorAlpha(MagickColors.White);
                }

                // Format mapping
                var magickFormat = ResolveMagickFormat(target.Id);
                image.Format = magickFormat;

                progress?.Report(0.7);
                cancellationToken.ThrowIfCancellationRequested();

                // Ensure target directory exists
                var dir = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                image.Write(targetPath);
                progress?.Report(1.0);
            }, cancellationToken);

            sw.Stop();

            // Validate output
            if (!File.Exists(targetPath))
            {
                return ConversionResult.Failed("Output file was not generated.", sw.Elapsed, DisplayName);
            }

            var fi = new FileInfo(targetPath);
            if (fi.Length == 0)
            {
                return ConversionResult.Failed("Output file is empty (0 bytes).", sw.Elapsed, DisplayName);
            }

            return ConversionResult.Succeeded(targetPath, fi.Length, sw.Elapsed, DisplayName);
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            return ConversionResult.Failed("Conversion was cancelled by user.", sw.Elapsed, DisplayName);
        }
        catch (Exception ex)
        {
            sw.Stop();
            return ConversionResult.Failed($"ImageMagick error: {ex.Message}", sw.Elapsed, DisplayName);
        }
    }

    private static MagickFormat ResolveMagickFormat(string targetId) => targetId.ToLowerInvariant() switch
    {
        "png" => MagickFormat.Png,
        "jpg" or "jpeg" => MagickFormat.Jpeg,
        "webp" => MagickFormat.WebP,
        "tiff" or "tif" => MagickFormat.Tiff,
        "bmp" => MagickFormat.Bmp,
        "gif" => MagickFormat.Gif,
        "ico" => MagickFormat.Ico,
        "avif" => MagickFormat.Avif,
        "svg" => MagickFormat.Svg,
        "pdf" => MagickFormat.Pdf,
        _ => MagickFormat.Png
    };
}
