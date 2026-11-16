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

public enum ConversionMode
{
    FormatConversion,
    VideoEncoderConversion,
    AudioEncoderConversion,
    DeviceCompatibility
}

public enum StreamHandlingMode
{
    FullReencode,
    RemuxStreamCopy,
    PartialReencode
}

public enum VideoCodecType
{
    Auto,
    Copy,
    H264,
    H265,
    AV1,
    VP9,
    VP8,
    MPEG4,
    MPEG2,
    ProRes
}

public enum AudioCodecType
{
    Auto,
    Copy,
    AAC,
    MP3,
    FLAC,
    ALAC,
    Opus,
    Vorbis,
    PCM,
    AC3,
    EAC3,
    WMA,
    AIFF
}

public enum DevicePresetType
{
    None,
    AppleIphoneIpad,
    AndroidPhone,
    WindowsPC,
    MacOS,
    SmartTV,
    WebBrowser,
    SocialMedia,
    Universal
}

public enum VideoResolutionPreset
{
    Original,
    Res4K,
    Res1440p,
    Res1080p,
    Res720p,
    Res480p,
    Custom
}

public enum VideoFpsPreset
{
    Original,
    Fps24,
    Fps25,
    Fps30,
    Fps50,
    Fps60,
    Custom
}

public enum H264Profile
{
    Auto,
    Baseline,
    Main,
    High
}

public enum HwAccelType
{
    Auto,
    Software,
    Nvenc,
    Qsv,
    Amf
}
