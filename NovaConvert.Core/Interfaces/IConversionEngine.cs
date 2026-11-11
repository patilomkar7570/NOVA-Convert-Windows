using NovaConvert.Core.Models;

namespace NovaConvert.Core.Interfaces;

public interface IConversionEngine
{
    string EngineId { get; }
    string DisplayName { get; }
    string Version { get; }
    string License { get; }
    bool IsAvailable { get; }
    string StatusSummary { get; }
    IReadOnlyList<FileCategory> SupportedCategories { get; }

    bool CanConvert(FormatDefinition source, FormatDefinition target);
    Task<ConversionResult> ConvertAsync(
        string sourcePath,
        string targetPath,
        FormatDefinition source,
        FormatDefinition target,
        ConversionOptions options,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);
}

public interface IEngineManager
{
    IReadOnlyList<IConversionEngine> Engines { get; }
    IConversionEngine? ResolveBestEngine(FormatDefinition source, FormatDefinition target);
    Task InitializeAsync();
}
