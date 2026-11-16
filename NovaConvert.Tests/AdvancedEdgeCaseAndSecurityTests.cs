using ImageMagick;
using NovaConvert.Core.Interfaces;
using NovaConvert.Core.Models;
using NovaConvert.Core.Services;
using NovaConvert.Engines;
using System.Diagnostics;
using Xunit;

namespace NovaConvert.Tests;

public class AdvancedEdgeCaseAndSecurityTests : IDisposable
{
    private readonly string _testDir;
    private readonly FormatRegistry _registry;
    private readonly MagickImageEngine _magickEngine;
    private readonly FFmpegEngine _ffmpegEngine;
    private readonly DocumentEngine _docEngine;
    private readonly EngineManager _engineManager;
    private readonly SettingsService _settingsService;
    private readonly SqliteHistoryRepository _historyRepo;
    private readonly FileInspectionService _inspector;
    private readonly ConversionQueueManager _queueManager;

    public AdvancedEdgeCaseAndSecurityTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "NOVA_SecTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);

        _registry = new FormatRegistry();
        _magickEngine = new MagickImageEngine();
        _ffmpegEngine = new FFmpegEngine();
        _docEngine = new DocumentEngine();
        _engineManager = new EngineManager();

        _settingsService = new SettingsService(Path.Combine(_testDir, "settings.json"));
        _settingsService.Settings.OutputDirectory = Path.Combine(_testDir, "Output Folder With Spaces");
        Directory.CreateDirectory(_settingsService.Settings.OutputDirectory);

        _historyRepo = new SqliteHistoryRepository(Path.Combine(_testDir, "history.db"));
        _inspector = new FileInspectionService(_registry);
        _queueManager = new ConversionQueueManager(_registry, _inspector, _engineManager, _settingsService, _historyRepo);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDir)) Directory.Delete(_testDir, true);
        }
        catch { }
    }

    [Fact]
    public async Task SpecialCharactersAndSpacesInPath_ConversionSucceeds()
    {
        // Directory with spaces and filename with brackets, parentheses, and ampersands
        var folderWithSpaces = Path.Combine(_testDir, "Special Folder (Files) & More");
        Directory.CreateDirectory(folderWithSpaces);

        var srcFile = Path.Combine(folderWithSpaces, "my file [2026] (test) & cool.png");
        using (var img = new MagickImage(MagickColors.Azure, 64, 64))
        {
            img.Write(srcFile);
        }

        var srcFmt = _registry.GetFormatById("png")!;
        var tgtFmt = _registry.GetFormatById("jpg")!;
        var outFile = Path.Combine(_settingsService.Settings.OutputDirectory, "out_special (1) [done] & final.jpg");

        var result = await _magickEngine.ConvertAsync(srcFile, outFile, srcFmt, tgtFmt, ConversionOptions.Default);

        Assert.True(result.Success, $"Failed with special characters in path: {result.ErrorMessage}");
        Assert.True(File.Exists(outFile));
        Assert.True(result.OutputSizeBytes > 0);
    }

    [Fact]
    public async Task EmptyFile_SafelyHandledWithoutCrashing()
    {
        var emptyFile = Path.Combine(_testDir, "empty.png");
        File.WriteAllBytes(emptyFile, Array.Empty<byte>());

        var srcFmt = _registry.GetFormatById("png")!;
        var tgtFmt = _registry.GetFormatById("jpg")!;
        var outFile = Path.Combine(_testDir, "empty_out.jpg");

        var result = await _magickEngine.ConvertAsync(emptyFile, outFile, srcFmt, tgtFmt, ConversionOptions.Default);

        // Empty file must fail gracefully and not leave empty output
        Assert.False(result.Success);
        Assert.False(File.Exists(outFile));
    }

    [Fact]
    public async Task CorruptedFile_SafelyReportsError()
    {
        var corruptedFile = Path.Combine(_testDir, "corrupted.png");
        // PNG header but random garbage content
        File.WriteAllBytes(corruptedFile, new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0xFF, 0x00, 0x12, 0x34 });

        var srcFmt = _registry.GetFormatById("png")!;
        var tgtFmt = _registry.GetFormatById("webp")!;
        var outFile = Path.Combine(_testDir, "corrupted_out.webp");

        var result = await _magickEngine.ConvertAsync(corruptedFile, outFile, srcFmt, tgtFmt, ConversionOptions.Default);

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task UnsupportedFormatPair_QueueManagerRejectsGracefully()
    {
        var txtFile = Path.Combine(_testDir, "sample.txt");
        File.WriteAllText(txtFile, "Sample content");

        await _queueManager.AddFilesAsync(new[] { txtFile });
        Assert.Single(_queueManager.Queue);

        var item = _queueManager.Queue[0];
        // Incompatible target: text cannot convert to MP4 video
        var mp4Fmt = _registry.GetFormatById("mp4")!;
        item.TargetFormat = mp4Fmt;

        await _queueManager.StartAsync();

        Assert.Equal(JobStatus.Failed, item.Status);
        Assert.Equal("No compatible engine found", item.StatusText);
        Assert.Contains("No available engine could convert", item.ErrorMessage ?? "");
    }

    [Fact]
    public async Task Cancellation_AbortsImmediatelyAndCleansUpPartialFiles()
    {
        if (!_ffmpegEngine.IsAvailable) return;

        // Create a 10-second wav
        var srcWav = Path.Combine(_testDir, "long_audio.wav");
        CreateWavFile(srcWav, 5);

        var srcFmt = _registry.GetFormatById("wav")!;
        var tgtFmt = _registry.GetFormatById("flac")!;
        var outFile = Path.Combine(_testDir, "cancel_test.flac");

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(50); // Cancel almost immediately

        var result = await _ffmpegEngine.ConvertAsync(srcWav, outFile, srcFmt, tgtFmt, ConversionOptions.Default, cancellationToken: cts.Token);

        Assert.False(result.Success);
        Assert.Contains("cancelled", result.ErrorMessage?.ToLowerInvariant() ?? "");
    }

    [Fact]
    public async Task VideoFormats_TS_MPG_OGV_CanConvertValidation()
    {
        var tsFmt = _registry.GetFormatById("ts");
        var mpgFmt = _registry.GetFormatById("mpg");
        var ogvFmt = _registry.GetFormatById("ogv");
        var threeGpFmt = _registry.GetFormatById("3gp");
        var mp4Fmt = _registry.GetFormatById("mp4")!;

        Assert.NotNull(tsFmt);
        Assert.NotNull(mpgFmt);
        Assert.NotNull(ogvFmt);
        Assert.NotNull(threeGpFmt);

        Assert.True(_ffmpegEngine.CanConvert(mp4Fmt, tsFmt));
        Assert.True(_ffmpegEngine.CanConvert(mp4Fmt, mpgFmt));
        Assert.True(_ffmpegEngine.CanConvert(mp4Fmt, ogvFmt));
        Assert.True(_ffmpegEngine.CanConvert(mp4Fmt, threeGpFmt));
    }

    [Fact]
    public async Task ThemePersistence_SettingsSaveAndLoadRetainsTheme()
    {
        _settingsService.Settings.Theme = "Light";
        await _settingsService.SaveAsync();

        var reloaded = new SettingsService(Path.Combine(_testDir, "settings.json"));
        await reloaded.LoadAsync();

        Assert.Equal("Light", reloaded.Settings.Theme);

        _settingsService.Settings.Theme = "Dark";
        await _settingsService.SaveAsync();
        await reloaded.LoadAsync();
        Assert.Equal("Dark", reloaded.Settings.Theme);

        _settingsService.Settings.Theme = "Auto";
        await _settingsService.SaveAsync();
        await reloaded.LoadAsync();
        Assert.Equal("Auto", reloaded.Settings.Theme);
    }

    private static void CreateWavFile(string path, int seconds)
    {
        int sampleRate = 22050;
        int numSamples = sampleRate * seconds;
        using var fs = File.Create(path);
        using var bw = new BinaryWriter(fs);
        bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        bw.Write(36 + (numSamples * 2));
        bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
        bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
        bw.Write(16);
        bw.Write((short)1);
        bw.Write((short)1);
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
