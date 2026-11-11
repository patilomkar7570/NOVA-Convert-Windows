using NovaConvert.Core.Models;
using NovaConvert.Core.Services;
using NovaConvert.Engines;
using Xunit;

namespace NovaConvert.Tests;

public class AudioAndVideoConversionTests : IDisposable
{
    private readonly string _testDir;
    private readonly FFmpegEngine _engine;
    private readonly FormatRegistry _registry;

    public AudioAndVideoConversionTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "NOVA_Test_Audio_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
        _engine = new FFmpegEngine();
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

    private string CreateSyntheticWav(int durationSeconds = 1, int sampleRate = 44100)
    {
        var path = Path.Combine(_testDir, $"synth_{Guid.NewGuid():N}.wav");
        int numSamples = sampleRate * durationSeconds;
        short[] samples = new short[numSamples];
        double frequency = 440.0; // A4

        for (int i = 0; i < numSamples; i++)
        {
            double t = (double)i / sampleRate;
            samples[i] = (short)(Math.Sin(2 * Math.PI * frequency * t) * 16000);
        }

        using var fs = File.Create(path);
        using var bw = new BinaryWriter(fs);

        // RIFF header
        bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        bw.Write(36 + (numSamples * 2));
        bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

        // fmt chunk
        bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
        bw.Write(16); // chunk size
        bw.Write((short)1); // PCM
        bw.Write((short)1); // Mono
        bw.Write(sampleRate);
        bw.Write(sampleRate * 2); // Byte rate
        bw.Write((short)2); // Block align
        bw.Write((short)16); // Bits per sample

        // data chunk
        bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        bw.Write(numSamples * 2);
        for (int i = 0; i < numSamples; i++)
        {
            bw.Write(samples[i]);
        }

        bw.Flush();
        return path;
    }

    [Fact]
    public async Task ConvertAsync_WavToMp3_SucceedsWithValidOutput()
    {
        if (!_engine.IsAvailable) return;

        var srcWav = CreateSyntheticWav(1);
        var targetMp3 = Path.Combine(_testDir, "output.mp3");
        var srcFmt = _registry.GetFormatById("wav")!;
        var tgtFmt = _registry.GetFormatById("mp3")!;

        var result = await _engine.ConvertAsync(srcWav, targetMp3, srcFmt, tgtFmt, new ConversionOptions
        {
            AudioBitrateKbps = 192
        });

        Assert.True(result.Success);
        Assert.True(File.Exists(targetMp3));
        Assert.True(result.OutputSizeBytes > 0);
    }

    [Fact]
    public async Task ConvertAsync_WavToFlac_LosslessSucceeds()
    {
        if (!_engine.IsAvailable) return;

        var srcWav = CreateSyntheticWav(1);
        var targetFlac = Path.Combine(_testDir, "output.flac");
        var srcFmt = _registry.GetFormatById("wav")!;
        var tgtFmt = _registry.GetFormatById("flac")!;

        var result = await _engine.ConvertAsync(srcWav, targetFlac, srcFmt, tgtFmt, new ConversionOptions
        {
            Lossless = true
        });

        Assert.True(result.Success);
        Assert.True(File.Exists(targetFlac));
        Assert.True(result.OutputSizeBytes > 0);
    }

    [Fact]
    public async Task ConvertAsync_RespectsCancellation()
    {
        if (!_engine.IsAvailable) return;

        var srcWav = CreateSyntheticWav(3);
        var targetMp3 = Path.Combine(_testDir, "cancel_test.mp3");
        var srcFmt = _registry.GetFormatById("wav")!;
        var tgtFmt = _registry.GetFormatById("mp3")!;

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        var result = await _engine.ConvertAsync(srcWav, targetMp3, srcFmt, tgtFmt, ConversionOptions.Default, cancellationToken: cts.Token);

        Assert.False(result.Success);
        Assert.Contains("cancelled", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }
}
