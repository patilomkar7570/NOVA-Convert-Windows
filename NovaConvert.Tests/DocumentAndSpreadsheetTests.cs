using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using NovaConvert.Core.Models;
using NovaConvert.Core.Services;
using NovaConvert.Engines;
using Xunit;

namespace NovaConvert.Tests;

public class DocumentAndSpreadsheetTests : IDisposable
{
    private readonly string _testDir;
    private readonly DocumentEngine _docEngine;
    private readonly SpreadsheetEngine _sheetEngine;
    private readonly FormatRegistry _registry;

    public DocumentAndSpreadsheetTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "NOVA_Test_Docs_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
        _docEngine = new DocumentEngine();
        _sheetEngine = new SpreadsheetEngine();
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

    private string CreateSampleDocx(string textContent)
    {
        var path = Path.Combine(_testDir, $"sample_{Guid.NewGuid():N}.docx");
        using (var wordDoc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document))
        {
            var mainPart = wordDoc.AddMainDocumentPart();
            mainPart.Document = new Document(new Body(new Paragraph(new Run(new Text(textContent)))));
            mainPart.Document.Save();
        }
        return path;
    }

    [Fact]
    public async Task ConvertAsync_DocxToTxt_ExtractsTextAccurately()
    {
        var sampleText = "Welcome to NOVA Convert desktop edition.";
        var srcDocx = CreateSampleDocx(sampleText);
        var targetTxt = Path.Combine(_testDir, "extracted.txt");
        var srcFmt = _registry.GetFormatById("docx")!;
        var tgtFmt = _registry.GetFormatById("txt")!;

        var result = await _docEngine.ConvertAsync(srcDocx, targetTxt, srcFmt, tgtFmt, ConversionOptions.Default);

        Assert.True(result.Success);
        Assert.True(File.Exists(targetTxt));
        var content = File.ReadAllText(targetTxt);
        Assert.Contains(sampleText, content);
    }

    [Fact]
    public async Task ConvertAsync_DocxToHtml_GeneratesValidHtmlMarkup()
    {
        var sampleText = "Formatted NOVA Document Paragraph";
        var srcDocx = CreateSampleDocx(sampleText);
        var targetHtml = Path.Combine(_testDir, "converted.html");
        var srcFmt = _registry.GetFormatById("docx")!;
        var tgtFmt = _registry.GetFormatById("html")!;

        var result = await _docEngine.ConvertAsync(srcDocx, targetHtml, srcFmt, tgtFmt, ConversionOptions.Default);

        Assert.True(result.Success);
        Assert.True(File.Exists(targetHtml));
        var content = File.ReadAllText(targetHtml);
        Assert.Contains("<!DOCTYPE html>", content);
        Assert.Contains(sampleText, content);
    }

    [Fact]
    public async Task ConvertAsync_CsvToJson_ConvertsTabularStructure()
    {
        var csvPath = Path.Combine(_testDir, "data.csv");
        File.WriteAllLines(csvPath, new[]
        {
            "Name,Age,Role",
            "Alex,30,Developer",
            "Sam,28,Designer"
        });

        var jsonPath = Path.Combine(_testDir, "data.json");
        var srcFmt = _registry.GetFormatById("csv")!;
        var tgtFmt = _registry.GetFormatById("json")!;

        var result = await _sheetEngine.ConvertAsync(csvPath, jsonPath, srcFmt, tgtFmt, ConversionOptions.Default);

        Assert.True(result.Success);
        Assert.True(File.Exists(jsonPath));
        var content = File.ReadAllText(jsonPath);
        Assert.Contains("Alex", content);
        Assert.Contains("Designer", content);
    }
}
