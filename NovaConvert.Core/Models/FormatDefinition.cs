namespace NovaConvert.Core.Models;

public record FormatDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required FileCategory Category { get; init; }
    public required string PrimaryExtension { get; init; }
    public IReadOnlyList<string> AlternativeExtensions { get; init; } = Array.Empty<string>();
    public required string MimeType { get; init; }
    public IReadOnlyList<byte[]> Signatures { get; init; } = Array.Empty<byte[]>();
    public int SignatureOffset { get; init; } = 0;
    public bool SupportsLossless { get; init; } = false;
    public bool SupportsTransparency { get; init; } = false;
    public bool SupportsMetadata { get; init; } = true;
    public string Description { get; init; } = string.Empty;
    public IReadOnlyList<string> CompatibleTargetFormatIds { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> KnownLimitations { get; init; } = Array.Empty<string>();

    public bool MatchesExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension)) return false;
        var cleanExt = extension.Trim().TrimStart('.').ToLowerInvariant();
        if (PrimaryExtension.TrimStart('.').Equals(cleanExt, StringComparison.OrdinalIgnoreCase))
            return true;
        return AlternativeExtensions.Any(alt => alt.TrimStart('.').Equals(cleanExt, StringComparison.OrdinalIgnoreCase));
    }
}
