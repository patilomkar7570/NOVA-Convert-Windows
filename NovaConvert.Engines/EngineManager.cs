using NovaConvert.Core.Interfaces;
using NovaConvert.Core.Models;

namespace NovaConvert.Engines;

public class EngineManager : IEngineManager
{
    private readonly List<IConversionEngine> _engines = new();
    public IReadOnlyList<IConversionEngine> Engines => _engines;

    public EngineManager()
    {
        InitializeEngines();
    }

    private void InitializeEngines()
    {
        _engines.Clear();
        _engines.Add(new MagickImageEngine());
        _engines.Add(new FFmpegEngine());
        _engines.Add(new PdfEngine());
        _engines.Add(new DocumentEngine());
        _engines.Add(new SpreadsheetEngine());
        _engines.Add(new ArchiveEngine());
        _engines.Add(new EbookEngine());
    }

    public Task InitializeAsync()
    {
        InitializeEngines();
        return Task.CompletedTask;
    }

    public IConversionEngine? ResolveBestEngine(FormatDefinition source, FormatDefinition target)
    {
        // 1. Direct check among active engines
        foreach (var engine in _engines)
        {
            if (engine.IsAvailable && engine.CanConvert(source, target))
            {
                return engine;
            }
        }

        return null;
    }
}
