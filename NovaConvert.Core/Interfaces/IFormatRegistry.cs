using NovaConvert.Core.Models;

namespace NovaConvert.Core.Interfaces;

public interface IFormatRegistry
{
    IReadOnlyList<FormatDefinition> GetAllFormats();
    FormatDefinition? GetFormatById(string formatId);
    FormatDefinition? GetFormatByExtension(string extension);
    FormatDefinition? IdentifyBySignature(ReadOnlySpan<byte> headerBytes, string fallbackExtension);
    IReadOnlyList<FormatDefinition> GetCompatibleTargets(FormatDefinition source, IReadOnlyList<IConversionEngine>? activeEngines = null);
    bool CanConvert(string sourceFormatId, string targetFormatId, IReadOnlyList<IConversionEngine>? activeEngines = null);
}
