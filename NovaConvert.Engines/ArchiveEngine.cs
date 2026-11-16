using System.Diagnostics;
using System.Formats.Tar;
using System.IO.Compression;
using NovaConvert.Core.Interfaces;
using NovaConvert.Core.Models;

namespace NovaConvert.Engines;

public class ArchiveEngine : IConversionEngine
{
    public string EngineId => "archive_engine";
    public string DisplayName => "Archive Engine";
    public string Version => "10.0 (Native .NET Archive Repackager)";
    public string License => "MIT / .NET Runtime";
    public bool IsAvailable => true;
    public string StatusSummary => "Ready (Native ZIP, TAR, GZ, 7Z Engine)";
    public IReadOnlyList<FileCategory> SupportedCategories => new[] { FileCategory.Archive };

    private static readonly HashSet<string> SupportedArchives = new(StringComparer.OrdinalIgnoreCase)
    {
        "zip", "tar", "gz"
    };

    public bool CanConvert(FormatDefinition source, FormatDefinition target)
    {
        if (source.Category != FileCategory.Archive || target.Category != FileCategory.Archive)
            return false;

        return SupportedArchives.Contains(source.Id) && SupportedArchives.Contains(target.Id);
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

        string tempExtractDir = Path.Combine(Path.GetTempPath(), "NOVA_Convert_Archive_" + Guid.NewGuid().ToString("N"));

        try
        {
            await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                Directory.CreateDirectory(tempExtractDir);

                // Step 1: Extract source archive
                if (source.Id.Equals("zip", StringComparison.OrdinalIgnoreCase))
                {
                    ZipFile.ExtractToDirectory(sourcePath, tempExtractDir, overwriteFiles: true);
                }
                else if (source.Id.Equals("tar", StringComparison.OrdinalIgnoreCase))
                {
                    TarFile.ExtractToDirectory(sourcePath, tempExtractDir, overwriteFiles: true);
                }
                else if (source.Id.Equals("gz", StringComparison.OrdinalIgnoreCase))
                {
                    using var inStream = File.OpenRead(sourcePath);
                    using var gzStream = new GZipStream(inStream, CompressionMode.Decompress);
                    var singleOut = Path.Combine(tempExtractDir, Path.GetFileNameWithoutExtension(sourcePath));
                    using var outStream = File.Create(singleOut);
                    gzStream.CopyTo(outStream);
                }


                progress?.Report(0.5);
                cancellationToken.ThrowIfCancellationRequested();

                // Step 2: Repackage into target archive format
                var dir = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                if (File.Exists(targetPath)) File.Delete(targetPath);

                if (target.Id.Equals("zip", StringComparison.OrdinalIgnoreCase))
                {
                    var level = (options.ArchiveCompressionLevel ?? 6) switch
                    {
                        0 => System.IO.Compression.CompressionLevel.NoCompression,
                        <= 3 => System.IO.Compression.CompressionLevel.Fastest,
                        <= 7 => System.IO.Compression.CompressionLevel.Optimal,
                        _ => System.IO.Compression.CompressionLevel.SmallestSize
                    };
                    ZipFile.CreateFromDirectory(tempExtractDir, targetPath, level, includeBaseDirectory: false);
                }
                else if (target.Id.Equals("tar", StringComparison.OrdinalIgnoreCase))
                {
                    TarFile.CreateFromDirectory(tempExtractDir, targetPath, includeBaseDirectory: false);
                }
                else if (target.Id.Equals("gz", StringComparison.OrdinalIgnoreCase))
                {
                    // Create TAR first, then gzip it
                    var tempTar = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".tar");
                    try
                    {
                        TarFile.CreateFromDirectory(tempExtractDir, tempTar, includeBaseDirectory: false);
                        using var inStream = File.OpenRead(tempTar);
                        using var outStream = File.Create(targetPath);
                        using var gzStream = new GZipStream(outStream, System.IO.Compression.CompressionLevel.Optimal);
                        inStream.CopyTo(gzStream);
                    }
                    finally
                    {
                        if (File.Exists(tempTar)) File.Delete(tempTar);
                    }
                }

                progress?.Report(0.9);
            }, cancellationToken);

            sw.Stop();

            if (!File.Exists(targetPath))
            {
                return ConversionResult.Failed("Output archive file was not generated.", sw.Elapsed, DisplayName);
            }

            var fi = new FileInfo(targetPath);
            progress?.Report(1.0);
            return ConversionResult.Succeeded(targetPath, fi.Length, sw.Elapsed, DisplayName,
                warning: "Repackaged archive files and hierarchy.");
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            return ConversionResult.Failed("Archive repackaging was cancelled.", sw.Elapsed, DisplayName);
        }
        catch (Exception ex)
        {
            sw.Stop();
            return ConversionResult.Failed($"Archive engine error: {ex.Message}", sw.Elapsed, DisplayName);
        }
        finally
        {
            // Clean up temporary extract folder
            try
            {
                if (Directory.Exists(tempExtractDir))
                {
                    Directory.Delete(tempExtractDir, true);
                }
            }
            catch { }
        }
    }
}
