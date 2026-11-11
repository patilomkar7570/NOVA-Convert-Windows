using NovaConvert.Core.Interfaces;
using NovaConvert.Core.Models;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace NovaConvert.Engines;

public class EbookEngine : IConversionEngine
{
    public string EngineId => "ebook_engine";
    public string DisplayName => "E-Book Engine";
    public string Version => "1.0.0 (Native EPUB Processor)";
    public string License => "MIT";
    public bool IsAvailable => true;
    public string StatusSummary => "Ready (Embedded DRM-compliant EPUB engine)";
    public IReadOnlyList<FileCategory> SupportedCategories => new[] { FileCategory.Ebook };

    public bool CanConvert(FormatDefinition source, FormatDefinition target)
    {
        if (source.Id.Equals("epub", StringComparison.OrdinalIgnoreCase))
        {
            return target.Id.Equals("txt", StringComparison.OrdinalIgnoreCase) ||
                   target.Id.Equals("html", StringComparison.OrdinalIgnoreCase);
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
            string? warning = null;
            await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var zip = ZipFile.OpenRead(sourcePath);

                // Check for DRM encryption
                var encryptionEntry = zip.GetEntry("META-INF/encryption.xml") ?? zip.GetEntry("META-INF/rights.xml");
                if (encryptionEntry != null)
                {
                    throw new InvalidOperationException("This e-book is protected by DRM (Digital Rights Management). NOVA Convert strictly respects digital copyright laws and cannot convert DRM-locked content.");
                }

                // Gather all HTML/XHTML chapters
                var chapterEntries = zip.Entries
                    .Where(e => e.FullName.EndsWith(".html", StringComparison.OrdinalIgnoreCase) ||
                                e.FullName.EndsWith(".xhtml", StringComparison.OrdinalIgnoreCase) ||
                                e.FullName.EndsWith(".htm", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(e => e.FullName)
                    .ToList();

                if (chapterEntries.Count == 0)
                {
                    throw new InvalidOperationException("No readable text chapters found in the e-book package.");
                }

                var dir = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                var sb = new StringBuilder();

                if (target.Id.Equals("html", StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendLine("<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>E-Book</title></head><body>");
                    for (int i = 0; i < chapterEntries.Count; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        using var stream = chapterEntries[i].Open();
                        using var reader = new StreamReader(stream, Encoding.UTF8);
                        var html = reader.ReadToEnd();
                        // Extract content inside body tag if present
                        var bodyMatch = Regex.Match(html, @"<body[^>]*>(.*?)</body>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                        sb.AppendLine(bodyMatch.Success ? bodyMatch.Groups[1].Value : html);
                        sb.AppendLine("<hr/>");
                        progress?.Report(0.1 + (0.8 * (i + 1) / chapterEntries.Count));
                    }
                    sb.AppendLine("</body></html>");
                }
                else // txt
                {
                    for (int i = 0; i < chapterEntries.Count; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        using var stream = chapterEntries[i].Open();
                        using var reader = new StreamReader(stream, Encoding.UTF8);
                        var html = reader.ReadToEnd();
                        var text = Regex.Replace(html, "<.*?>", string.Empty);
                        text = System.Net.WebUtility.HtmlDecode(text);
                        sb.AppendLine(text.Trim());
                        sb.AppendLine();
                        progress?.Report(0.1 + (0.8 * (i + 1) / chapterEntries.Count));
                    }
                    warning = "E-book was converted to plain text; reflow formatting and styling were discarded.";
                }

                File.WriteAllText(targetPath, sb.ToString(), Encoding.UTF8);
            }, cancellationToken);

            sw.Stop();

            if (!File.Exists(targetPath))
            {
                return ConversionResult.Failed("Output e-book conversion file was not created.", sw.Elapsed, DisplayName);
            }

            var fi = new FileInfo(targetPath);
            progress?.Report(1.0);
            return ConversionResult.Succeeded(targetPath, fi.Length, sw.Elapsed, DisplayName, warning);
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            return ConversionResult.Failed("E-book conversion was cancelled.", sw.Elapsed, DisplayName);
        }
        catch (Exception ex)
        {
            sw.Stop();
            return ConversionResult.Failed($"E-book engine error: {ex.Message}", sw.Elapsed, DisplayName);
        }
    }
}
