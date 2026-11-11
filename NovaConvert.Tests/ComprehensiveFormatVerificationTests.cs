using ImageMagick;
using NovaConvert.Core.Models;
using NovaConvert.Core.Services;
using NovaConvert.Engines;
using Xunit;

namespace NovaConvert.Tests;

public class ComprehensiveFormatVerificationTests : IDisposable
{
    private readonly string _testDir;
    private readonly FormatRegistry _registry;
    private readonly MagickImageEngine _magickEngine;
    private readonly FFmpegEngine _ffmpegEngine;
    private readonly DocumentEngine _docEngine;
    private readonly SpreadsheetEngine _sheetEngine;
    private readonly ArchiveEngine _archiveEngine;
    private readonly EbookEngine _ebookEngine;
    private readonly PdfEngine _pdfEngine;

    public ComprehensiveFormatVerificationTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "NOVA_Verify_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);

        _registry = new FormatRegistry();
        _magickEngine = new MagickImageEngine();
        _ffmpegEngine = new FFmpegEngine();
        _docEngine = new DocumentEngine();
        _sheetEngine = new SpreadsheetEngine();
        _archiveEngine = new ArchiveEngine();
        _ebookEngine = new EbookEngine();
        _pdfEngine = new PdfEngine();
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDir)) Directory.Delete(_testDir, true);
        }
        catch { }
    }

    [Theory]
    [InlineData("png", "jpg")]
    [InlineData("png", "webp")]
    [InlineData("png", "bmp")]
    [InlineData("png", "tiff")]
    [InlineData("png", "gif")]
    [InlineData("png", "ico")]
    [InlineData("png", "pdf")]
    [InlineData("jpg", "png")]
    public async Task ImageConversions_AllSupportedFormatsWork(string srcId, string tgtId)
    {
        var srcFile = Path.Combine(_testDir, $"test_{srcId}.{srcId}");
        using (var img = new MagickImage(MagickColors.Crimson, 64, 64))
        {
            img.Write(srcFile);
        }

        var srcFmt = _registry.GetFormatById(srcId)!;
        var tgtFmt = _registry.GetFormatById(tgtId)!;
        var outFile = Path.Combine(_testDir, $"out_{srcId}_to_{tgtId}.{tgtFmt.PrimaryExtension.TrimStart('.')}");

        var result = await _magickEngine.ConvertAsync(srcFile, outFile, srcFmt, tgtFmt, ConversionOptions.Default);

        Assert.True(result.Success, $"Failed converting {srcId} to {tgtId}: {result.ErrorMessage}");
        Assert.True(File.Exists(outFile));
        Assert.True(result.OutputSizeBytes > 0);
    }

    [Theory]
    [InlineData("mp3")]
    [InlineData("flac")]
    [InlineData("aac")]
    [InlineData("ogg")]
    [InlineData("opus")]
    [InlineData("m4a")]
    [InlineData("wma")]
    public async Task AudioConversions_WavToAllSupportedAudioFormatsWork(string targetAudioId)
    {
        if (!_ffmpegEngine.IsAvailable) return;

        // Generate synthetic wav
        var srcWav = Path.Combine(_testDir, $"source_{targetAudioId}.wav");
        Create1SecWav(srcWav);

        var srcFmt = _registry.GetFormatById("wav")!;
        var tgtFmt = _registry.GetFormatById(targetAudioId)!;
        var outFile = Path.Combine(_testDir, $"out_wav_to_{targetAudioId}.{tgtFmt.PrimaryExtension.TrimStart('.')}");

        var result = await _ffmpegEngine.ConvertAsync(srcWav, outFile, srcFmt, tgtFmt, ConversionOptions.Default);

        Assert.True(result.Success, $"Failed converting WAV to {targetAudioId}: {result.ErrorMessage}");
        Assert.True(File.Exists(outFile));
        Assert.True(result.OutputSizeBytes > 0);
    }

    [Theory]
    [InlineData("mkv")]
    [InlineData("webm")]
    [InlineData("mov")]
    [InlineData("avi")]
    [InlineData("gif")]
    [InlineData("mp3")] // audio extraction
    public async Task VideoConversions_Mp4ToAllTargetsWork(string targetId)
    {
        if (!_ffmpegEngine.IsAvailable) return;

        // Generate a 1-second synthetic mp4 with test video/audio
        var srcMp4 = Path.Combine(_testDir, "synth_video.mp4");
        if (!File.Exists(srcMp4))
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = _ffmpegEngine.ExecutablePath!,
                Arguments = "-y -f lavfi -i testsrc=duration=1:size=320x240:rate=24 -f lavfi -i sine=frequency=440:duration=1 -c:v libx264 -c:a aac \"" + srcMp4 + "\"",
                CreateNoWindow = true,
                UseShellExecute = false
            };
            using var p = System.Diagnostics.Process.Start(psi);
            p?.WaitForExit();
        }

        var srcFmt = _registry.GetFormatById("mp4")!;
        var tgtFmt = _registry.GetFormatById(targetId)!;
        var outFile = Path.Combine(_testDir, $"out_mp4_to_{targetId}.{tgtFmt.PrimaryExtension.TrimStart('.')}");

        var result = await _ffmpegEngine.ConvertAsync(srcMp4, outFile, srcFmt, tgtFmt, ConversionOptions.Default);

        Assert.True(result.Success, $"Failed converting MP4 to {targetId}: {result.ErrorMessage}");
        Assert.True(File.Exists(outFile));
        Assert.True(result.OutputSizeBytes > 0);
    }

    [Fact]
    public async Task DocumentConversions_TextMarkdownHtmlDocxWork()
    {
        var mdFile = Path.Combine(_testDir, "test.md");
        File.WriteAllText(mdFile, "# Title\n\nThis is a paragraph with **bold** text.");

        var mdFmt = _registry.GetFormatById("md")!;
        var htmlFmt = _registry.GetFormatById("html")!;
        var txtFmt = _registry.GetFormatById("txt")!;

        // MD -> HTML
        var outHtml = Path.Combine(_testDir, "out.html");
        var res1 = await _docEngine.ConvertAsync(mdFile, outHtml, mdFmt, htmlFmt, ConversionOptions.Default);
        Assert.True(res1.Success);
        Assert.True(File.Exists(outHtml));
        Assert.Contains("<h1>Title</h1>", File.ReadAllText(outHtml));

        // HTML -> TXT
        var outTxt = Path.Combine(_testDir, "out.txt");
        var res2 = await _docEngine.ConvertAsync(outHtml, outTxt, htmlFmt, txtFmt, ConversionOptions.Default);
        Assert.True(res2.Success);
        Assert.Contains("Title", File.ReadAllText(outTxt));
    }

    [Fact]
    public async Task EbookConversion_EpubToHtmlAndTxtWork()
    {
        var epubFile = Path.Combine(_testDir, "sample.epub");
        using (var zip = System.IO.Compression.ZipFile.Open(epubFile, System.IO.Compression.ZipArchiveMode.Create))
        {
            var chapter = zip.CreateEntry("OEBPS/chapter1.xhtml");
            using var stream = chapter.Open();
            using var writer = new StreamWriter(stream);
            writer.Write("<html><head><title>Chapter 1</title></head><body><h1>NOVA Ebook</h1><p>Reading effortless text.</p></body></html>");
        }

        var epubFmt = _registry.GetFormatById("epub")!;
        var htmlFmt = _registry.GetFormatById("html")!;
        var txtFmt = _registry.GetFormatById("txt")!;

        var outHtml = Path.Combine(_testDir, "ebook_out.html");
        var resHtml = await _ebookEngine.ConvertAsync(epubFile, outHtml, epubFmt, htmlFmt, ConversionOptions.Default);
        Assert.True(resHtml.Success);
        Assert.Contains("NOVA Ebook", File.ReadAllText(outHtml));

        var outTxt = Path.Combine(_testDir, "ebook_out.txt");
        var resTxt = await _ebookEngine.ConvertAsync(epubFile, outTxt, epubFmt, txtFmt, ConversionOptions.Default);
        Assert.True(resTxt.Success);
        Assert.Contains("Reading effortless text", File.ReadAllText(outTxt));
    }

    private static void Create1SecWav(string path)
    {
        int sampleRate = 22050;
        int numSamples = sampleRate;
        using var fs = File.Create(path);
        using var bw = new BinaryWriter(fs);
        bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        bw.Write(36 + (numSamples * 2));
        bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
        bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
        bw.Write(16);
        bw.Write((short)1); // PCM
        bw.Write((short)1); // mono
        bw.Write(sampleRate);
        bw.Write(sampleRate * 2);
        bw.Write((short)2);
        bw.Write((short)16);
        bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        bw.Write(numSamples * 2);
        for (int i = 0; i < numSamples; i++)
        {
            bw.Write((short)(Math.Sin(2 * Math.PI * 440 * i / sampleRate) * 12000));
        }
    }
}
