namespace NovaConvert.Core.Models;

public enum FileCategory
{
    Image,
    Audio,
    Video,
    Document,
    Spreadsheet,
    Presentation,
    Pdf,
    Ebook,
    Archive
}

public enum QualityPreset
{
    Auto,
    BestQuality,
    Balanced,
    SmallerFile,
    Lossless,
    Custom
}

public enum JobStatus
{
    Queued,
    Inspecting,
    Converting,
    Completed,
    Failed,
    Cancelled,
    Paused
}

public enum OverwriteAction
{
    CreateUniqueName,
    Overwrite,
    Skip,
    Prompt
}
