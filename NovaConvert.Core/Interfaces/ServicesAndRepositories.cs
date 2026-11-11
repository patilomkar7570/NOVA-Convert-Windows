using NovaConvert.Core.Models;

namespace NovaConvert.Core.Interfaces;

public interface IFileInspectionService
{
    Task<FileInspectionResult> InspectFileAsync(string filePath, CancellationToken cancellationToken = default);
}

public record HistoryEntry
{
    public long Id { get; init; }
    public DateTime TimestampUtc { get; init; }
    public required string SourceFilePath { get; init; }
    public required string OutputFilePath { get; init; }
    public required string SourceFormat { get; init; }
    public required string TargetFormat { get; init; }
    public long SourceSizeBytes { get; init; }
    public long OutputSizeBytes { get; init; }
    public double DurationSeconds { get; init; }
    public required string EngineUsed { get; init; }
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}

public interface IHistoryRepository
{
    Task InitializeAsync();
    Task AddEntryAsync(HistoryEntry entry);
    Task<IReadOnlyList<HistoryEntry>> GetRecentEntriesAsync(int limit = 100);
    Task ClearHistoryAsync();
}

public class AppSettings
{
    public string OutputDirectory { get; set; } = string.Empty;
    public bool SaveInSourceDirectory { get; set; } = false;
    public OverwriteAction OverwritePolicy { get; set; } = OverwriteAction.CreateUniqueName;
    public string Theme { get; set; } = "Dark"; // "Dark", "Light", "System"
    public bool AutoCheckUpdates { get; set; } = true;
    public int MaxConcurrentJobs { get; set; } = 2;
    public bool PreserveMetadataByDefault { get; set; } = true;
    public QualityPreset DefaultQualityPreset { get; set; } = QualityPreset.Auto;
}

public interface ISettingsService
{
    AppSettings Settings { get; }
    Task LoadAsync();
    Task SaveAsync();
    string ResolveOutputPath(string sourcePath, string targetExtension);
}

public record UpdateCheckResult
{
    public bool IsUpdateAvailable { get; init; }
    public string CurrentVersion { get; init; } = "1.0.0";
    public string LatestVersion { get; init; } = "1.0.0";
    public string? ReleaseNotes { get; init; }
    public string? DownloadUrl { get; init; }
    public string? ExpectedSha256 { get; init; }
}

public interface IUpdateService
{
    Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default);
}
