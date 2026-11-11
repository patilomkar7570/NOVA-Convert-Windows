namespace NovaConvert.Core.Models;

public record EngineInfo
{
    public required string EngineId { get; init; }
    public required string DisplayName { get; init; }
    public required string Version { get; init; }
    public required string License { get; init; }
    public required bool IsAvailable { get; init; }
    public required string StatusSummary { get; init; }
    public string? ExecutablePath { get; init; }
    public IReadOnlyList<FileCategory> SupportedCategories { get; init; } = Array.Empty<FileCategory>();
}
