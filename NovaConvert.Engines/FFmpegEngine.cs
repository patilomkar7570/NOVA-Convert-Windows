using NovaConvert.Core.Interfaces;
using NovaConvert.Core.Models;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace NovaConvert.Engines;

public class FFmpegEngine : IConversionEngine
{
    public string EngineId => "ffmpeg";
    public string DisplayName => "FFmpeg Media Engine";
    public string Version { get; private set; } = "7.x (GPL)";
    public string License => "GNU General Public License (GPL) / LGPL";
    public bool IsAvailable { get; private set; }
    public string StatusSummary { get; private set; } = "Initializing...";
    public string? ExecutablePath { get; private set; }

    public IReadOnlyList<FileCategory> SupportedCategories => new[] { FileCategory.Audio, FileCategory.Video };

    private static readonly HashSet<string> AudioFormats = new(StringComparer.OrdinalIgnoreCase)
    {
        "mp3", "wav", "flac", "aac", "m4a", "ogg", "opus", "wma", "aiff", "aif"
    };

    private static readonly HashSet<string> VideoFormats = new(StringComparer.OrdinalIgnoreCase)
    {
        "mp4", "mkv", "webm", "mov", "avi", "wmv", "flv", "gif", "ts", "mpg", "mpeg", "3gp", "ogv"
    };

    public FFmpegEngine()
    {
        DetectFFmpeg();
    }

    private void DetectFFmpeg()
    {
        // 1. Look in application bundled directory
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var bundledPath = Path.Combine(baseDir, "engines", "ffmpeg", "ffmpeg.exe");

        // Also check development workspace engines folder
        var workspacePath = Path.Combine(Directory.GetCurrentDirectory(), "engines", "ffmpeg", "ffmpeg.exe");

        if (File.Exists(bundledPath))
        {
            ExecutablePath = bundledPath;
            IsAvailable = true;
            StatusSummary = "Ready (Bundled FFmpeg)";
        }
        else if (File.Exists(workspacePath))
        {
            ExecutablePath = workspacePath;
            IsAvailable = true;
            StatusSummary = "Ready (Workspace Bundled FFmpeg)";
        }
        else
        {
            // 2. Check system PATH
            var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            foreach (var p in pathEnv.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var candidate = Path.Combine(p.Trim(), "ffmpeg.exe");
                if (File.Exists(candidate))
                {
                    ExecutablePath = candidate;
                    IsAvailable = true;
                    StatusSummary = "Ready (System PATH)";
                    break;
                }
            }
        }

        if (ExecutablePath == null)
        {
            IsAvailable = false;
            StatusSummary = "FFmpeg executable not found";
        }
    }

    public bool CanConvert(FormatDefinition source, FormatDefinition target)
    {
        if (!IsAvailable) return false;

        // Audio to Audio
        if (source.Category == FileCategory.Audio && target.Category == FileCategory.Audio)
        {
            return AudioFormats.Contains(source.Id) && AudioFormats.Contains(target.Id);
        }

        // Video to Video
        if (source.Category == FileCategory.Video && target.Category == FileCategory.Video)
        {
            return VideoFormats.Contains(source.Id) && VideoFormats.Contains(target.Id);
        }

        // Audio extraction: Video to Audio
        if (source.Category == FileCategory.Video && target.Category == FileCategory.Audio)
        {
            return VideoFormats.Contains(source.Id) && AudioFormats.Contains(target.Id);
        }

        return false;
    }

    public async Task<ConversionResult> ConvertAsync(
        string sourcePath,
        string targetPath,
        FormatDefinition source,
        FormatDefinition target,
        ConversionOptions options,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsAvailable || string.IsNullOrEmpty(ExecutablePath))
        {
            return ConversionResult.Failed("FFmpeg engine is not available on this system.", default, DisplayName);
        }

        var sw = Stopwatch.StartNew();
        progress?.Report(0.05);

        try
        {
            var dir = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }

            var args = BuildFFmpegArguments(sourcePath, targetPath, source, target, options);

            var psi = new ProcessStartInfo
            {
                FileName = ExecutablePath,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            var errorLogs = new StringBuilder();

            process.ErrorDataReceived += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(e.Data)) return;
                errorLogs.AppendLine(e.Data);

                // Parse progress from FFmpeg stderr "time=00:01:23.45"
                var match = Regex.Match(e.Data, @"time=(\d+):(\d+):(\d+\.\d+)");
                if (match.Success)
                {
                    double hours = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                    double mins = double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                    double secs = double.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
                    double currentSeconds = (hours * 3600) + (mins * 60) + secs;
                    // Provide smooth progress reporting
                    if (currentSeconds > 0)
                    {
                        progress?.Report(Math.Min(0.95, 0.1 + (currentSeconds / 100.0)));
                    }
                }
            };

            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();

            using var reg = cancellationToken.Register(() =>
            {
                try
                {
                    if (!process.HasExited) process.Kill(entireProcessTree: true);
                }
                catch { }
            });

            await process.WaitForExitAsync(cancellationToken);
            sw.Stop();

            if (cancellationToken.IsCancellationRequested)
            {
                return ConversionResult.Failed("Conversion was cancelled by user.", sw.Elapsed, DisplayName);
            }

            if (process.ExitCode != 0)
            {
                return ConversionResult.Failed($"FFmpeg exited with code {process.ExitCode}. Log: {errorLogs.ToString().Trim()}", sw.Elapsed, DisplayName);
            }

            // Output validation
            if (!File.Exists(targetPath))
            {
                return ConversionResult.Failed("Output media file was not generated.", sw.Elapsed, DisplayName);
            }

            var fi = new FileInfo(targetPath);
            if (fi.Length == 0)
            {
                return ConversionResult.Failed("Output media file is 0 bytes.", sw.Elapsed, DisplayName);
            }

            progress?.Report(1.0);
            return ConversionResult.Succeeded(targetPath, fi.Length, sw.Elapsed, DisplayName);
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            return ConversionResult.Failed("Conversion was cancelled by user.", sw.Elapsed, DisplayName);
        }
        catch (Exception ex)
        {
            sw.Stop();
            return ConversionResult.Failed($"FFmpeg execution failed: {ex.Message}", sw.Elapsed, DisplayName);
        }
    }

    private string BuildFFmpegArguments(string sourcePath, string targetPath, FormatDefinition source, FormatDefinition target, ConversionOptions options)
    {
        var sb = new StringBuilder();
        var safeSource = sourcePath.Replace("\"", "\\\"");
        var safeTarget = targetPath.Replace("\"", "\\\"");

        sb.Append("-y -hide_banner "); // Overwrite output and hide banner
        sb.Append($"-i \"{safeSource}\" ");

        // If converting video to GIF
        if (target.Id.Equals("gif", StringComparison.OrdinalIgnoreCase))
        {
            int fps = options.VideoFps ?? 15;
            sb.Append($"-vf \"fps={fps},scale=480:-1:flags=lanczos,split[s0][s1];[s0]palettegen[p];[s1][p]paletteuse\" ");
            sb.Append($"\"{safeTarget}\"");
            return sb.ToString();
        }

        // If audio extraction or audio conversion
        if (target.Category == FileCategory.Audio || options.ExtractAudioOnly)
        {
            sb.Append("-vn "); // No video

            // Audio codec & quality settings
            if (target.Id.Equals("ogg", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(options.AudioCodec))
            {
                sb.Append("-c:a libvorbis -q:a 4 ");
            }
            else
            {
                var codec = ResolveAudioCodec(target.Id, options.AudioCodec);
                if (!string.IsNullOrEmpty(codec)) sb.Append($"-c:a {codec} ");

                // Bitrate
                int bitrate = options.AudioBitrateKbps ?? ResolveDefaultAudioBitrate(options.Preset);
                sb.Append($"-b:a {bitrate}k ");
            }

            // Sample rate
            if (options.AudioSampleRateHz.HasValue)
            {
                sb.Append($"-ar {options.AudioSampleRateHz.Value} ");
            }

            // Channels
            if (options.AudioChannels.HasValue)
            {
                sb.Append($"-ac {options.AudioChannels.Value} ");
            }
        }
        else if (target.Category == FileCategory.Video)
        {
            // Video settings
            var vcodec = ResolveVideoCodec(target.Id, options.VideoCodec);
            if (!string.IsNullOrEmpty(vcodec)) sb.Append($"-c:v {vcodec} ");

            // Resolution / scaling with sanitization
            if (!string.IsNullOrWhiteSpace(options.VideoResolution))
            {
                var cleanRes = Regex.Replace(options.VideoResolution.Trim(), @"[^0-9xX:\-]", "");
                if (!string.IsNullOrEmpty(cleanRes))
                {
                    cleanRes = cleanRes.Replace('x', ':').Replace('X', ':');
                    sb.Append($"-vf scale={cleanRes} ");
                }
            }

            // Frame rate
            if (options.VideoFps.HasValue && options.VideoFps.Value > 0)
            {
                sb.Append($"-r {options.VideoFps.Value} ");
            }

            // Video bitrate
            if (options.VideoBitrateKbps.HasValue && options.VideoBitrateKbps.Value > 0)
            {
                sb.Append($"-b:v {options.VideoBitrateKbps.Value}k ");
            }
            else
            {
                // Sensible preset CRF
                int crf = options.Preset switch
                {
                    QualityPreset.BestQuality => 18,
                    QualityPreset.Balanced => 23,
                    QualityPreset.SmallerFile => 28,
                    _ => 22
                };
                if (vcodec == "libx264" || vcodec == "libx265")
                {
                    sb.Append($"-crf {crf} -preset medium ");
                }
            }

            // Audio codec for video
            var acodec = ResolveAudioCodecForContainer(target.Id);
            sb.Append($"-c:a {acodec} -b:a 192k ");
        }

        // Metadata preservation
        if (!options.PreserveMetadata)
        {
            sb.Append("-map_metadata -1 ");
        }

        sb.Append($"\"{safeTarget}\"");
        return sb.ToString();
    }

    private static string ResolveAudioCodec(string targetId, string? userCodec)
    {
        if (!string.IsNullOrWhiteSpace(userCodec) && Regex.IsMatch(userCodec, @"^[a-zA-Z0-9_\-]+$")) return userCodec;
        return targetId.ToLowerInvariant() switch
        {
            "mp3" => "libmp3lame",
            "wav" => "pcm_s16le",
            "flac" => "flac",
            "aac" or "m4a" => "aac",
            "ogg" => "libvorbis",
            "opus" => "libopus",
            "wma" => "wmav2",
            "aiff" or "aif" => "pcm_s16be",
            _ => "aac"
        };
    }

    private static string ResolveVideoCodec(string targetId, string? userCodec)
    {
        if (!string.IsNullOrWhiteSpace(userCodec) && Regex.IsMatch(userCodec, @"^[a-zA-Z0-9_\-]+$")) return userCodec;
        return targetId.ToLowerInvariant() switch
        {
            "mp4" or "mov" => "libx264",
            "webm" => "libvpx-vp9",
            "mkv" => "libx264",
            "avi" => "mpeg4",
            "wmv" => "wmv2",
            "flv" => "flv1",
            "ts" => "libx264",
            "mpg" or "mpeg" => "mpeg2video",
            "3gp" => "h263",
            "ogv" => "libtheora",
            _ => "libx264"
        };
    }

    private static string ResolveAudioCodecForContainer(string containerId) => containerId.ToLowerInvariant() switch
    {
        "webm" => "libopus",
        "flv" => "mp3",
        "ogv" => "libvorbis",
        "3gp" => "amr_nb",
        "mpg" or "mpeg" => "mp2",
        _ => "aac"
    };

    private static int ResolveDefaultAudioBitrate(QualityPreset preset) => preset switch
    {
        QualityPreset.BestQuality => 320,
        QualityPreset.Balanced => 192,
        QualityPreset.SmallerFile => 128,
        _ => 192
    };
}
