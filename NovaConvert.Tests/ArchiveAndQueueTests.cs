using NovaConvert.Core.Models;
using NovaConvert.Core.Services;
using NovaConvert.Engines;
using System.IO.Compression;
using Xunit;

namespace NovaConvert.Tests;

public class ArchiveAndQueueTests : IDisposable
{
    private readonly string _testDir;
    private readonly ArchiveEngine _archiveEngine;
    private readonly FormatRegistry _registry;

    public ArchiveAndQueueTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "NOVA_Test_Archive_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
        _archiveEngine = new ArchiveEngine();
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

    [Fact]
    public async Task ConvertAsync_ZipToTar_RepackagesArchiveIntegrity()
    {
        var contentDir = Path.Combine(_testDir, "source_content");
        Directory.CreateDirectory(contentDir);
        File.WriteAllText(Path.Combine(contentDir, "file1.txt"), "Content of file 1");
        File.WriteAllText(Path.Combine(contentDir, "file2.txt"), "Content of file 2");

        var zipPath = Path.Combine(_testDir, "source.zip");
        ZipFile.CreateFromDirectory(contentDir, zipPath);

        var tarPath = Path.Combine(_testDir, "repackaged.tar");
        var srcFmt = _registry.GetFormatById("zip")!;
        var tgtFmt = _registry.GetFormatById("tar")!;

        var result = await _archiveEngine.ConvertAsync(zipPath, tarPath, srcFmt, tgtFmt, ConversionOptions.Default);

        Assert.True(result.Success);
        Assert.True(File.Exists(tarPath));
        Assert.True(result.OutputSizeBytes > 0);
    }

    [Fact]
    public void SettingsService_ResolveOutputPath_GeneratesUniqueNameOnCollision()
    {
        var settingsFile = Path.Combine(_testDir, "test_settings.json");
        var settingsService = new SettingsService(settingsFile);
        settingsService.Settings.OutputDirectory = _testDir;
        settingsService.Settings.OverwritePolicy = OverwriteAction.CreateUniqueName;

        var existingFile = Path.Combine(_testDir, "document.pdf");
        File.WriteAllText(existingFile, "dummy");

        var resolved = settingsService.ResolveOutputPath(existingFile, ".pdf");

        Assert.Equal(Path.Combine(_testDir, "document (1).pdf"), resolved);
    }

    [Fact]
    public async Task SqliteHistoryRepository_PersistsAndRetrievesEntries()
    {
        var dbPath = Path.Combine(_testDir, "test_history.db");
        var repo = new SqliteHistoryRepository(dbPath);
        await repo.InitializeAsync();

        var entry = new NovaConvert.Core.Interfaces.HistoryEntry
        {
            TimestampUtc = DateTime.UtcNow,
            SourceFilePath = "C:\\test\\photo.png",
            OutputFilePath = "C:\\test\\photo.jpg",
            SourceFormat = "PNG",
            TargetFormat = "JPG",
            SourceSizeBytes = 1024,
            OutputSizeBytes = 512,
            DurationSeconds = 0.45,
            EngineUsed = "ImageMagick",
            Success = true
        };

        await repo.AddEntryAsync(entry);

        var list = await repo.GetRecentEntriesAsync(10);
        Assert.NotEmpty(list);
        var retrieved = list.First();
        Assert.Equal("PNG", retrieved.SourceFormat);
        Assert.Equal("JPG", retrieved.TargetFormat);
        Assert.True(retrieved.Success);
    }

    [Fact]
    public async Task UpdateService_VerifyFileHashAsync_ValidatesCorrectSha256()
    {
        var sampleFile = Path.Combine(_testDir, "sha_test.bin");
        byte[] data = System.Text.Encoding.UTF8.GetBytes("NOVA Convert Secure Verification");
        await File.WriteAllBytesAsync(sampleFile, data);

        // Precalculated SHA256 of "NOVA Convert Secure Verification"
        using var sha = System.Security.Cryptography.SHA256.Create();
        var expectedHashHex = Convert.ToHexString(sha.ComputeHash(data));

        bool isValid = await UpdateService.VerifyFileHashAsync(sampleFile, expectedHashHex);
        Assert.True(isValid);

        bool isInvalid = await UpdateService.VerifyFileHashAsync(sampleFile, "0000000000000000000000000000000000000000000000000000000000000000");
        Assert.False(isInvalid);
    }
}
