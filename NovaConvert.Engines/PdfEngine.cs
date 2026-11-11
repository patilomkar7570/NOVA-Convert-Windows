using NovaConvert.Core.Interfaces;
using NovaConvert.Core.Models;
using System.Diagnostics;
using System.Text;
using UglyToad.PdfPig;

namespace NovaConvert.Engines;

public class PdfEngine : IConversionEngine
{
    public string EngineId => "pdf_engine";
    public string DisplayName => "PDF Processing Engine";
    public string Version => "1.7.0 (PdfPig)";
    public string License => "Apache 2.0";
    public bool IsAvailable => true;
    public string StatusSummary => "Ready (Embedded PdfPig text engine)";
    public IReadOnlyList<FileCategory> SupportedCategories => new[] { FileCategory.Pdf };

    public bool CanConvert(FormatDefinition source, FormatDefinition target)
    {
        // PDF to TXT
        if (source.Id.Equals("pdf", StringComparison.OrdinalIgnoreCase) &&
            target.Id.Equals("txt", StringComparison.OrdinalIgnoreCase))
        {
            return true;
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
                using var document = PdfDocument.Open(sourcePath);
                var sb = new StringBuilder();

                int totalPages = document.NumberOfPages;
                for (int i = 1; i <= totalPages; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var page = document.GetPage(i);
                    sb.AppendLine($"--- Page {i} ---");
                    sb.AppendLine(page.Text);
                    sb.AppendLine();

                    progress?.Report(0.1 + (0.8 * i / totalPages));
                }

                var dir = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                File.WriteAllText(targetPath, sb.ToString(), Encoding.UTF8);
            }, cancellationToken);

            sw.Stop();

            if (!File.Exists(targetPath))
            {
                return ConversionResult.Failed("Output text file was not created.", sw.Elapsed, DisplayName);
            }

            var fi = new FileInfo(targetPath);
            progress?.Report(1.0);
            return ConversionResult.Succeeded(targetPath, fi.Length, sw.Elapsed, DisplayName,
                warning: "PDF text extraction captures text contents; complex formatting and styling were discarded.");
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            return ConversionResult.Failed("PDF conversion was cancelled by user.", sw.Elapsed, DisplayName);
        }
        catch (Exception ex)
        {
            sw.Stop();
            return ConversionResult.Failed($"PDF engine error: {ex.Message}", sw.Elapsed, DisplayName);
        }
    }
}
