namespace NovaConvert.Core.Models;

public record ConversionOptions
{
    public QualityPreset Preset { get; init; } = QualityPreset.Auto;
    public int? QualityValue { get; init; } = 85; // 1 - 100 for lossy image/video/audio
    public bool Lossless { get; init; } = false;
    public bool PreserveMetadata { get; init; } = true;

    // Image-specific
    public int? ResizeWidth { get; init; }
    public int? ResizeHeight { get; init; }
    public bool PreserveAspectRatio { get; init; } = true;
    public string? ColorProfile { get; init; }

    // Audio-specific
    public int? AudioBitrateKbps { get; init; } // e.g. 192, 256, 320
    public int? AudioSampleRateHz { get; init; } // e.g. 44100, 48000
    public int? AudioChannels { get; init; } // 1 (mono), 2 (stereo)
    public string? AudioCodec { get; init; }

    // Video-specific
    public int? VideoBitrateKbps { get; init; }
    public int? VideoFps { get; init; }
    public string? VideoResolution { get; init; } // "1920x1080", "1280x720", etc.
    public string? VideoCodec { get; init; } // "libx264", "libvpx-vp9", etc.
    public bool ExtractAudioOnly { get; init; } = false;

    // PDF-specific
    public int? PdfDpi { get; init; } = 300;
    public string? PdfPageRange { get; init; } // "1-5", "all"

    // Archive-specific
    public int? ArchiveCompressionLevel { get; init; } = 6; // 0 (Store) - 9 (Ultra)

    // Document-specific
    public bool WarnOnLossyLayout { get; init; } = true;

    public static ConversionOptions Default => new();
}

public record ConversionResult
{
    public required bool Success { get; init; }
    public string? OutputPath { get; init; }
    public long OutputSizeBytes { get; init; }
    public TimeSpan Duration { get; init; }
    public string? ErrorMessage { get; init; }
    public string? WarningMessage { get; init; }
    public string? EngineUsed { get; init; }
    public bool OutputValidated { get; init; }

    public static ConversionResult Succeeded(string outputPath, long size, TimeSpan duration, string engine, string? warning = null) =>
        new()
        {
            Success = true,
            OutputPath = outputPath,
            OutputSizeBytes = size,
            Duration = duration,
            EngineUsed = engine,
            OutputValidated = true,
            WarningMessage = warning
        };

    public static ConversionResult Failed(string error, TimeSpan duration = default, string? engine = null) =>
        new()
        {
            Success = false,
            ErrorMessage = error,
            Duration = duration,
            EngineUsed = engine,
            OutputValidated = false
        };
}

public record FileInspectionResult
{
    public required string FilePath { get; init; }
    public required string FileName { get; init; }
    public required long FileSizeBytes { get; init; }
    public required string DeclaredExtension { get; init; }
    public FormatDefinition? DetectedFormat { get; init; }
    public FormatDefinition? ExtensionFormat { get; init; }
    public bool HasExtensionMismatch { get; init; }
    public string? MismatchWarning { get; init; }
    public Dictionary<string, string> Metadata { get; init; } = new();
    public bool IsCorruptedOrInvalid { get; init; }
    public string? ErrorMessage { get; init; }
}
