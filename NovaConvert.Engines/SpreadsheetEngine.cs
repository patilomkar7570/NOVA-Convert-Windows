using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using NovaConvert.Core.Interfaces;
using NovaConvert.Core.Models;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace NovaConvert.Engines;

public class SpreadsheetEngine : IConversionEngine
{
    public string EngineId => "spreadsheet_engine";
    public string DisplayName => "Spreadsheet Engine";
    public string Version => "3.5.1 (OpenXml + Tabular)";
    public string License => "MIT / OpenXml";
    public bool IsAvailable => true;
    public string StatusSummary => "Ready (Embedded Tabular Engine)";
    public IReadOnlyList<FileCategory> SupportedCategories => new[] { FileCategory.Spreadsheet };

    public bool CanConvert(FormatDefinition source, FormatDefinition target)
    {
        // XLSX -> CSV, JSON, HTML
        if (source.Id.Equals("xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return target.Id.Equals("csv", StringComparison.OrdinalIgnoreCase) ||
                   target.Id.Equals("json", StringComparison.OrdinalIgnoreCase) ||
                   target.Id.Equals("html", StringComparison.OrdinalIgnoreCase);
        }

        // CSV -> JSON, HTML
        if (source.Id.Equals("csv", StringComparison.OrdinalIgnoreCase))
        {
            return target.Id.Equals("json", StringComparison.OrdinalIgnoreCase) ||
                   target.Id.Equals("html", StringComparison.OrdinalIgnoreCase);
        }

        // JSON -> CSV
        if (source.Id.Equals("json", StringComparison.OrdinalIgnoreCase))
        {
            return target.Id.Equals("csv", StringComparison.OrdinalIgnoreCase);
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
                var dir = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                if (source.Id.Equals("xlsx", StringComparison.OrdinalIgnoreCase))
                {
                    ConvertXlsx(sourcePath, targetPath, target.Id, out warning);
                }
                else if (source.Id.Equals("csv", StringComparison.OrdinalIgnoreCase))
                {
                    ConvertCsv(sourcePath, targetPath, target.Id);
                }
                else if (source.Id.Equals("json", StringComparison.OrdinalIgnoreCase))
                {
                    ConvertJsonToCsv(sourcePath, targetPath);
                }
                progress?.Report(0.9);
            }, cancellationToken);

            sw.Stop();

            if (!File.Exists(targetPath))
            {
                return ConversionResult.Failed("Output spreadsheet was not generated.", sw.Elapsed, DisplayName);
            }

            var fi = new FileInfo(targetPath);
            progress?.Report(1.0);
            return ConversionResult.Succeeded(targetPath, fi.Length, sw.Elapsed, DisplayName, warning);
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            return ConversionResult.Failed("Spreadsheet conversion was cancelled.", sw.Elapsed, DisplayName);
        }
        catch (Exception ex)
        {
            sw.Stop();
            return ConversionResult.Failed($"Spreadsheet engine error: {ex.Message}", sw.Elapsed, DisplayName);
        }
    }

    private void ConvertXlsx(string sourcePath, string targetPath, string targetId, out string? warning)
    {
        warning = null;
        using var document = SpreadsheetDocument.Open(sourcePath, false);
        var workbookPart = document.WorkbookPart;
        if (workbookPart == null)
        {
            File.WriteAllText(targetPath, string.Empty, Encoding.UTF8);
            return;
        }

        var sheets = workbookPart.Workbook?.Sheets?.Elements<Sheet>().ToList();
        if (sheets == null || sheets.Count == 0)
        {
            File.WriteAllText(targetPath, string.Empty, Encoding.UTF8);
            return;
        }

        if (sheets.Count > 1 && targetId == "csv")
        {
            warning = $"Workbook contains {sheets.Count} sheets. Only the first sheet was exported to CSV format.";
        }

        var firstSheet = sheets.FirstOrDefault();
        if (firstSheet == null || firstSheet.Id == null)
        {
            File.WriteAllText(targetPath, string.Empty, Encoding.UTF8);
            return;
        }

        var worksheetPart = (WorksheetPart)workbookPart.GetPartById(firstSheet.Id!);
        var sheetData = worksheetPart.Worksheet?.Elements<SheetData>().FirstOrDefault();
        var sharedStringTable = workbookPart.SharedStringTablePart?.SharedStringTable;

        var rowsData = new List<List<string>>();
        if (sheetData != null)
        {
            foreach (var row in sheetData.Elements<Row>())
            {
                var rowList = new List<string>();
                foreach (var cell in row.Elements<Cell>())
                {
                    string value = cell.CellValue?.InnerText ?? string.Empty;
                    if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
                    {
                        if (int.TryParse(value, out int ssid) && sharedStringTable != null)
                        {
                            value = sharedStringTable.ElementAt(ssid).InnerText;
                        }
                    }
                    rowList.Add(value);
                }
                rowsData.Add(rowList);
            }
        }

        if (targetId == "csv")
        {
            var sb = new StringBuilder();
            foreach (var row in rowsData)
            {
                var escaped = row.Select(c => $"\"{c.Replace("\"", "\"\"")}\"");
                sb.AppendLine(string.Join(",", escaped));
            }
            File.WriteAllText(targetPath, sb.ToString(), Encoding.UTF8);
        }
        else if (targetId == "json")
        {
            var json = JsonSerializer.Serialize(rowsData, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(targetPath, json, Encoding.UTF8);
        }
        else if (targetId == "html")
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>Spreadsheet</title></head><body>");
            sb.AppendLine("<table border=\"1\" cellpadding=\"4\" cellspacing=\"0\">");
            foreach (var row in rowsData)
            {
                sb.AppendLine("<tr>");
                foreach (var cell in row)
                {
                    sb.AppendLine($"<td>{System.Net.WebUtility.HtmlEncode(cell)}</td>");
                }
                sb.AppendLine("</tr>");
            }
            sb.AppendLine("</table></body></html>");
            File.WriteAllText(targetPath, sb.ToString(), Encoding.UTF8);
        }
    }

    private void ConvertCsv(string sourcePath, string targetPath, string targetId)
    {
        var lines = File.ReadAllLines(sourcePath, Encoding.UTF8);
        var table = new List<List<string>>();
        foreach (var line in lines)
        {
            // Simple CSV split (handling quotes)
            var parts = line.Split(',');
            table.Add(parts.Select(p => p.Trim('"', ' ')).ToList());
        }

        if (targetId == "json")
        {
            var json = JsonSerializer.Serialize(table, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(targetPath, json, Encoding.UTF8);
        }
        else if (targetId == "html")
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>Data Table</title></head><body>");
            sb.AppendLine("<table border=\"1\" cellpadding=\"4\" cellspacing=\"0\">");
            foreach (var row in table)
            {
                sb.AppendLine("<tr>");
                foreach (var cell in row)
                {
                    sb.AppendLine($"<td>{System.Net.WebUtility.HtmlEncode(cell)}</td>");
                }
                sb.AppendLine("</tr>");
            }
            sb.AppendLine("</table></body></html>");
            File.WriteAllText(targetPath, sb.ToString(), Encoding.UTF8);
        }
    }

    private void ConvertJsonToCsv(string sourcePath, string targetPath)
    {
        var json = File.ReadAllText(sourcePath, Encoding.UTF8);
        var doc = JsonDocument.Parse(json);
        var sb = new StringBuilder();

        if (doc.RootElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var row in doc.RootElement.EnumerateArray())
            {
                if (row.ValueKind == JsonValueKind.Array)
                {
                    var items = row.EnumerateArray().Select(e => $"\"{e.ToString().Replace("\"", "\"\"")}\"");
                    sb.AppendLine(string.Join(",", items));
                }
                else if (row.ValueKind == JsonValueKind.Object)
                {
                    var items = row.EnumerateObject().Select(p => $"\"{p.Value.ToString().Replace("\"", "\"\"")}\"");
                    sb.AppendLine(string.Join(",", items));
                }
            }
        }

        File.WriteAllText(targetPath, sb.ToString(), Encoding.UTF8);
    }
}
