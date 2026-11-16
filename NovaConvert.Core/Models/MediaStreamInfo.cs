using System.Text.Json.Serialization;

namespace NovaConvert.Core.Models;

public record MediaAnalysisResult
{
    public required string FilePath { get; init; }
    public string ContainerFormat { get; init; } = string.Empty;
    public string ContainerLongName { get; init; } = string.Empty;
    public TimeSpan Duration { get; init; } = TimeSpan.Zero;
    public long FileSizeBytes { get; init; }
    public int? OverallBitrateKbps { get; init; }

    public List<VideoStreamInfo> VideoStreams { get; init; } = new();
    public List<AudioStreamInfo> AudioStreams { get; init; } = new();
    public List<SubtitleStreamInfo> SubtitleStreams { get; init; } = new();
    public Dictionary<string, string> Tags { get; init; } = new();

    public VideoStreamInfo? PrimaryVideo => VideoStreams.FirstOrDefault();
    public AudioStreamInfo? PrimaryAudio => AudioStreams.FirstOrDefault();

    public bool HasVideo => VideoStreams.Count > 0;
    public bool HasAudio => AudioStreams.Count > 0;
    public bool HasSubtitles => SubtitleStreams.Count > 0;

    public string VideoSummary
    {
        get
        {
            if (PrimaryVideo == null) return "No video stream";
            var codec = PrimaryVideo.Codec.ToUpperInvariant();
            var res = $"{PrimaryVideo.Width}×{PrimaryVideo.Height}";
            var fps = PrimaryVideo.FrameRate > 0 ? $"{Math.Round(PrimaryVideo.FrameRate, 2)} FPS" : "";
            var pix = !string.IsNullOrEmpty(PrimaryVideo.PixelFormat) ? PrimaryVideo.PixelFormat : "";
            var parts = new[] { codec, res, fps, pix }.Where(p => !string.IsNullOrEmpty(p));
            return string.Join(" | ", parts);
        }
    }

    public string AudioSummary
    {
        get
        {
            if (PrimaryAudio == null) return "No audio stream";
            var codec = PrimaryAudio.Codec.ToUpperInvariant();
            var layout = !string.IsNullOrEmpty(PrimaryAudio.ChannelLayout) ? PrimaryAudio.ChannelLayout : $"{PrimaryAudio.Channels} ch";
            var rate = PrimaryAudio.SampleRateHz > 0 ? $"{PrimaryAudio.SampleRateHz / 1000.0:0.#} kHz" : "";
            var br = PrimaryAudio.BitrateKbps.HasValue ? $"{PrimaryAudio.BitrateKbps} kbps" : "";
            var parts = new[] { codec, layout, rate, br }.Where(p => !string.IsNullOrEmpty(p));
            return string.Join(" | ", parts);
        }
    }

    public string FormattedDuration => Duration.TotalHours >= 1
        ? Duration.ToString(@"hh\:mm\:ss")
        : Duration.ToString(@"mm\:ss");
}

public record VideoStreamInfo
{
    public int Index { get; init; }
    public string Codec { get; init; } = string.Empty;
    public string CodecLongName { get; init; } = string.Empty;
    public string Profile { get; init; } = string.Empty;
    public int Width { get; init; }
    public int Height { get; init; }
    public string AspectRatio { get; init; } = string.Empty;
    public double FrameRate { get; init; }
    public string PixelFormat { get; init; } = string.Empty;
    public string ColorSpace { get; init; } = string.Empty;
    public string ColorTransfer { get; init; } = string.Empty;
    public string ColorPrimaries { get; init; } = string.Empty;
    public bool IsHdr { get; init; }
    public int? BitrateKbps { get; init; }
    public TimeSpan Duration { get; init; }
    public long TotalFrames { get; init; }
}

public record AudioStreamInfo
{
    public int Index { get; init; }
    public string Codec { get; init; } = string.Empty;
    public string CodecLongName { get; init; } = string.Empty;
    public int Channels { get; init; }
    public string ChannelLayout { get; init; } = string.Empty;
    public int SampleRateHz { get; init; }
    public int? BitrateKbps { get; init; }
    public string Language { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public TimeSpan Duration { get; init; }
}

public record SubtitleStreamInfo
{
    public int Index { get; init; }
    public string Codec { get; init; } = string.Empty;
    public string Language { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public bool IsDefault { get; init; }
    public bool IsForced { get; init; }
}

public record HardwareAccelInfo
{
    public bool HasNvenc { get; init; }
    public bool HasQsv { get; init; }
    public bool HasAmf { get; init; }
    public List<string> SupportedEncoders { get; init; } = new();
    public List<string> SupportedHwaccels { get; init; } = new();

    public string Summary
    {
        get
        {
            var list = new List<string>();
            if (HasNvenc) list.Add("NVIDIA NVENC");
            if (HasQsv) list.Add("Intel QuickSync");
            if (HasAmf) list.Add("AMD AMF");
            return list.Count > 0 ? string.Join(", ", list) : "CPU (Software)";
        }
    }
}
