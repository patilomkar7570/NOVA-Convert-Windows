using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using NovaConvert.Core.Interfaces;
using NovaConvert.Core.Models;
using System.Diagnostics;
using System.Text;

namespace NovaConvert.Engines;

public class DocumentEngine : IConversionEngine
{
    public string EngineId => "document_engine";
    public string DisplayName => "Document Engine";
    public string Version => "3.5.1 (OpenXml + Native Text)";
    public string License => "MIT / Microsoft OpenXml License";
    public bool IsAvailable => true;
    public string StatusSummary { get; private set; } = "Ready (Built-in OpenXml engine)";
    public string? LibreOfficePath { get; private set; }
    public string? PandocPath { get; private set; }

    public IReadOnlyList<FileCategory> SupportedCategories => new[] { FileCategory.Document };

    private static readonly HashSet<string> BuiltInSupportedInput = new(StringComparer.OrdinalIgnoreCase)
    {
        "docx", "txt", "md", "html"
    };

    public DocumentEngine()
    {
        DetectExternalOfficeEngines();
    }

    private void DetectExternalOfficeEngines()
    {
        // Check for LibreOffice
        var loCandidates = new[]
        {
            @"C:\Program Files\LibreOffice\program\soffice.exe",
            @"C:\Program Files (x86)\LibreOffice\program\soffice.exe"
        };
        foreach (var c in loCandidates)
        {
            if (File.Exists(c))
            {
                LibreOfficePath = c;
                break;
            }
        }

        // Check for Pandoc on PATH
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var p in pathEnv.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var pandocCandidate = Path.Combine(p.Trim(), "pandoc.exe");
            if (File.Exists(pandocCandidate))
            {
                PandocPath = pandocCandidate;
                break;
            }
        }

        if (LibreOfficePath != null)
        {
            StatusSummary = "Ready (OpenXml + LibreOffice detected)";
        }
    }

    public bool CanConvert(FormatDefinition source, FormatDefinition target)
    {
        // Native OpenXml conversions:
        // docx -> txt, html, md
        if (source.Id.Equals("docx", StringComparison.OrdinalIgnoreCase))
        {
            if (target.Id.Equals("txt", StringComparison.OrdinalIgnoreCase) ||
                target.Id.Equals("html", StringComparison.OrdinalIgnoreCase) ||
                target.Id.Equals("md", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // docx -> pdf via LibreOffice if available
            if (target.Id.Equals("pdf", StringComparison.OrdinalIgnoreCase) && LibreOfficePath != null)
            {
                return true;
            }
        }

        // txt -> html, md
        if (source.Id.Equals("txt", StringComparison.OrdinalIgnoreCase))
        {
            return target.Id.Equals("html", StringComparison.OrdinalIgnoreCase) ||
                   target.Id.Equals("md", StringComparison.OrdinalIgnoreCase);
        }

        // md -> html, txt
        if (source.Id.Equals("md", StringComparison.OrdinalIgnoreCase))
        {
            return target.Id.Equals("html", StringComparison.OrdinalIgnoreCase) ||
                   target.Id.Equals("txt", StringComparison.OrdinalIgnoreCase);
        }

        // html -> txt, md
        if (source.Id.Equals("html", StringComparison.OrdinalIgnoreCase))
        {
            return target.Id.Equals("txt", StringComparison.OrdinalIgnoreCase) ||
                   target.Id.Equals("md", StringComparison.OrdinalIgnoreCase);
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
            // LibreOffice headless conversion if docx -> pdf
            if (source.Id.Equals("docx", StringComparison.OrdinalIgnoreCase) &&
                target.Id.Equals("pdf", StringComparison.OrdinalIgnoreCase) &&
                LibreOfficePath != null)
            {
                return await ConvertWithLibreOfficeAsync(sourcePath, targetPath, cancellationToken);
            }

            await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var dir = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                if (source.Id.Equals("docx", StringComparison.OrdinalIgnoreCase))
                {
                    ConvertDocx(sourcePath, targetPath, target.Id);
                }
                else if (source.Id.Equals("txt", StringComparison.OrdinalIgnoreCase))
                {
                    ConvertTxt(sourcePath, targetPath, target.Id);
                }
                else if (source.Id.Equals("md", StringComparison.OrdinalIgnoreCase))
                {
                    ConvertMd(sourcePath, targetPath, target.Id);
                }
                else if (source.Id.Equals("html", StringComparison.OrdinalIgnoreCase))
                {
                    ConvertHtml(sourcePath, targetPath, target.Id);
                }
                progress?.Report(0.9);
            }, cancellationToken);

            sw.Stop();

            if (!File.Exists(targetPath))
            {
                return ConversionResult.Failed("Output document was not generated.", sw.Elapsed, DisplayName);
            }

            var fi = new FileInfo(targetPath);
            progress?.Report(1.0);
            return ConversionResult.Succeeded(targetPath, fi.Length, sw.Elapsed, DisplayName,
                warning: "Structural elements and formatting may differ from native word processor view.");
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            return ConversionResult.Failed("Document conversion was cancelled.", sw.Elapsed, DisplayName);
        }
        catch (Exception ex)
        {
            sw.Stop();
            return ConversionResult.Failed($"Document engine error: {ex.Message}", sw.Elapsed, DisplayName);
        }
    }

    private void ConvertDocx(string sourcePath, string targetPath, string targetId)
    {
        using var doc = WordprocessingDocument.Open(sourcePath, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body == null)
        {
            File.WriteAllText(targetPath, string.Empty, Encoding.UTF8);
            return;
        }

        var sb = new StringBuilder();

        if (targetId == "html")
        {
            sb.AppendLine("<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>Converted Document</title></head><body>");
            foreach (var element in body.Elements())
            {
                if (element is Paragraph p)
                {
                    var text = p.InnerText;
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        sb.AppendLine($"<p>{System.Net.WebUtility.HtmlEncode(text)}</p>");
                    }
                }
                else if (element is Table t)
                {
                    sb.AppendLine("<table border=\"1\">");
                    foreach (var row in t.Elements<TableRow>())
                    {
                        sb.AppendLine("<tr>");
                        foreach (var cell in row.Elements<TableCell>())
                        {
                            sb.AppendLine($"<td>{System.Net.WebUtility.HtmlEncode(cell.InnerText)}</td>");
                        }
                        sb.AppendLine("</tr>");
                    }
                    sb.AppendLine("</table>");
                }
            }
            sb.AppendLine("</body></html>");
        }
        else if (targetId == "md")
        {
            foreach (var element in body.Elements())
            {
                if (element is Paragraph p)
                {
                    var text = p.InnerText;
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        sb.AppendLine(text);
                        sb.AppendLine();
                    }
                }
                else if (element is Table t)
                {
                    foreach (var row in t.Elements<TableRow>())
                    {
                        var cells = row.Elements<TableCell>().Select(c => c.InnerText.Replace("|", "\\|")).ToList();
                        sb.AppendLine("| " + string.Join(" | ", cells) + " |");
                    }
                    sb.AppendLine();
                }
            }
        }
        else // txt
        {
            foreach (var p in body.Elements<Paragraph>())
            {
                sb.AppendLine(p.InnerText);
            }
        }

        File.WriteAllText(targetPath, sb.ToString(), Encoding.UTF8);
    }

    private void ConvertTxt(string sourcePath, string targetPath, string targetId)
    {
        var content = File.ReadAllText(sourcePath, Encoding.UTF8);
        if (targetId == "html")
        {
            var html = $"<!DOCTYPE html><html><head><meta charset=\"utf-8\"></head><body><pre>{System.Net.WebUtility.HtmlEncode(content)}</pre></body></html>";
            File.WriteAllText(targetPath, html, Encoding.UTF8);
        }
        else // md
        {
            File.WriteAllText(targetPath, content, Encoding.UTF8);
        }
    }

    private void ConvertMd(string sourcePath, string targetPath, string targetId)
    {
        var content = File.ReadAllText(sourcePath, Encoding.UTF8);
        if (targetId == "html")
        {
            var lines = content.Split('\n');
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html><html><head><meta charset=\"utf-8\"></head><body>");
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("# "))
                    sb.AppendLine($"<h1>{System.Net.WebUtility.HtmlEncode(trimmed[2..])}</h1>");
                else if (trimmed.StartsWith("## "))
                    sb.AppendLine($"<h2>{System.Net.WebUtility.HtmlEncode(trimmed[3..])}</h2>");
                else if (trimmed.StartsWith("### "))
                    sb.AppendLine($"<h3>{System.Net.WebUtility.HtmlEncode(trimmed[4..])}</h3>");
                else if (!string.IsNullOrEmpty(trimmed))
                    sb.AppendLine($"<p>{System.Net.WebUtility.HtmlEncode(trimmed)}</p>");
            }
            sb.AppendLine("</body></html>");
            File.WriteAllText(targetPath, sb.ToString(), Encoding.UTF8);
        }
        else // txt
        {
            File.WriteAllText(targetPath, content, Encoding.UTF8);
        }
    }

    private void ConvertHtml(string sourcePath, string targetPath, string targetId)
    {
        var content = File.ReadAllText(sourcePath, Encoding.UTF8);
        // Clean HTML tags to text
        var text = System.Text.RegularExpressions.Regex.Replace(content, "<.*?>", string.Empty);
        text = System.Net.WebUtility.HtmlDecode(text);
        File.WriteAllText(targetPath, text.Trim(), Encoding.UTF8);
    }

    private async Task<ConversionResult> ConvertWithLibreOfficeAsync(string sourcePath, string targetPath, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var targetDir = Path.GetDirectoryName(targetPath) ?? Directory.GetCurrentDirectory();

        var psi = new ProcessStartInfo
        {
            FileName = LibreOfficePath!,
            Arguments = $"--headless --convert-to pdf --outdir \"{targetDir}\" \"{sourcePath}\"",
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        if (process == null)
        {
            return ConversionResult.Failed("Failed to launch LibreOffice process.", sw.Elapsed, DisplayName);
        }

        await process.WaitForExitAsync(cancellationToken);
        sw.Stop();

        // LibreOffice outputs with same base name but .pdf in targetDir
        var expectedOut = Path.Combine(targetDir, Path.GetFileNameWithoutExtension(sourcePath) + ".pdf");
        if (File.Exists(expectedOut) && !expectedOut.Equals(targetPath, StringComparison.OrdinalIgnoreCase))
        {
            if (File.Exists(targetPath)) File.Delete(targetPath);
            File.Move(expectedOut, targetPath);
        }

        if (File.Exists(targetPath))
        {
            var fi = new FileInfo(targetPath);
            return ConversionResult.Succeeded(targetPath, fi.Length, sw.Elapsed, "LibreOffice");
        }

        return ConversionResult.Failed("LibreOffice did not generate output PDF.", sw.Elapsed, "LibreOffice");
    }
}
