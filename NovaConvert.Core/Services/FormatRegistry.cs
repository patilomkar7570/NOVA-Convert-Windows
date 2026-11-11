using NovaConvert.Core.Interfaces;
using NovaConvert.Core.Models;

namespace NovaConvert.Core.Services;

public class FormatRegistry : IFormatRegistry
{
    private readonly List<FormatDefinition> _formats = new();
    private readonly Dictionary<string, FormatDefinition> _formatsById = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, FormatDefinition> _formatsByExt = new(StringComparer.OrdinalIgnoreCase);

    public FormatRegistry()
    {
        InitializeRegistry();
    }

    private void InitializeRegistry()
    {
        // ------------------ IMAGES ------------------
        Register(new FormatDefinition
        {
            Id = "png",
            Name = "Portable Network Graphics",
            Category = FileCategory.Image,
            PrimaryExtension = ".png",
            MimeType = "image/png",
            Signatures = new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } },
            SupportsLossless = true,
            SupportsTransparency = true,
            SupportsMetadata = true,
            Description = "Lossless raster format ideal for web graphics, screenshots, and logos.",
            CompatibleTargetFormatIds = new[] { "jpg", "webp", "tiff", "bmp", "gif", "ico", "avif", "pdf" }
        });

        Register(new FormatDefinition
        {
            Id = "jpg",
            Name = "JPEG Image",
            Category = FileCategory.Image,
            PrimaryExtension = ".jpg",
            AlternativeExtensions = new[] { ".jpeg", ".jpe", ".jfif" },
            MimeType = "image/jpeg",
            Signatures = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
            SupportsLossless = false,
            SupportsTransparency = false,
            SupportsMetadata = true,
            Description = "Standard lossy photographic format with adjustable compression.",
            CompatibleTargetFormatIds = new[] { "png", "webp", "tiff", "bmp", "gif", "ico", "avif", "pdf" }
        });

        Register(new FormatDefinition
        {
            Id = "webp",
            Name = "WebP Image",
            Category = FileCategory.Image,
            PrimaryExtension = ".webp",
            MimeType = "image/webp",
            Signatures = new[] { new byte[] { 0x52, 0x49, 0x46, 0x46 } }, // RIFF....WEBP verified in inspector
            SupportsLossless = true,
            SupportsTransparency = true,
            SupportsMetadata = true,
            Description = "Modern efficient web image format developed by Google.",
            CompatibleTargetFormatIds = new[] { "png", "jpg", "tiff", "bmp", "gif", "ico", "pdf" }
        });

        Register(new FormatDefinition
        {
            Id = "tiff",
            Name = "Tagged Image File Format",
            Category = FileCategory.Image,
            PrimaryExtension = ".tiff",
            AlternativeExtensions = new[] { ".tif" },
            MimeType = "image/tiff",
            Signatures = new[] { new byte[] { 0x49, 0x49, 0x2A, 0x00 }, new byte[] { 0x4D, 0x4D, 0x00, 0x2A } },
            SupportsLossless = true,
            SupportsTransparency = true,
            SupportsMetadata = true,
            Description = "High-depth raster format popular in publishing and photography.",
            CompatibleTargetFormatIds = new[] { "png", "jpg", "webp", "bmp", "pdf" }
        });

        Register(new FormatDefinition
        {
            Id = "bmp",
            Name = "Windows Bitmap",
            Category = FileCategory.Image,
            PrimaryExtension = ".bmp",
            MimeType = "image/bmp",
            Signatures = new[] { new byte[] { 0x42, 0x4D } },
            SupportsLossless = true,
            SupportsTransparency = false,
            SupportsMetadata = false,
            Description = "Uncompressed Windows native raster format.",
            CompatibleTargetFormatIds = new[] { "png", "jpg", "webp", "tiff", "ico", "pdf" }
        });

        Register(new FormatDefinition
        {
            Id = "gif",
            Name = "Graphics Interchange Format",
            Category = FileCategory.Image,
            PrimaryExtension = ".gif",
            MimeType = "image/gif",
            Signatures = new[] { new byte[] { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 }, new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 } },
            SupportsLossless = true,
            SupportsTransparency = true,
            SupportsMetadata = false,
            Description = "Indexed color format supporting animations.",
            CompatibleTargetFormatIds = new[] { "png", "jpg", "webp", "mp4", "webm" }
        });

        Register(new FormatDefinition
        {
            Id = "ico",
            Name = "Windows Icon",
            Category = FileCategory.Image,
            PrimaryExtension = ".ico",
            MimeType = "image/x-icon",
            Signatures = new[] { new byte[] { 0x00, 0x00, 0x01, 0x00 } },
            SupportsLossless = true,
            SupportsTransparency = true,
            SupportsMetadata = false,
            Description = "Multi-resolution icon format for Windows applications.",
            CompatibleTargetFormatIds = new[] { "png", "jpg", "bmp" }
        });

        Register(new FormatDefinition
        {
            Id = "avif",
            Name = "AV1 Image File Format",
            Category = FileCategory.Image,
            PrimaryExtension = ".avif",
            MimeType = "image/avif",
            SupportsLossless = true,
            SupportsTransparency = true,
            SupportsMetadata = true,
            Description = "Next-generation image format based on AV1 video compression.",
            CompatibleTargetFormatIds = new[] { "png", "jpg", "webp" }
        });

        Register(new FormatDefinition
        {
            Id = "svg",
            Name = "Scalable Vector Graphics",
            Category = FileCategory.Image,
            PrimaryExtension = ".svg",
            MimeType = "image/svg+xml",
            SupportsLossless = true,
            SupportsTransparency = true,
            SupportsMetadata = true,
            Description = "XML-based vector image format.",
            CompatibleTargetFormatIds = new[] { "png", "jpg", "webp", "pdf" }
        });

        // ------------------ AUDIO ------------------
        Register(new FormatDefinition
        {
            Id = "mp3",
            Name = "MPEG Audio Layer III",
            Category = FileCategory.Audio,
            PrimaryExtension = ".mp3",
            MimeType = "audio/mpeg",
            Signatures = new[] { new byte[] { 0x49, 0x44, 0x33 }, new byte[] { 0xFF, 0xFB } },
            SupportsLossless = false,
            SupportsMetadata = true,
            Description = "Universal lossy audio codec with broad compatibility.",
            CompatibleTargetFormatIds = new[] { "wav", "flac", "aac", "m4a", "ogg", "opus", "wma" }
        });

        Register(new FormatDefinition
        {
            Id = "wav",
            Name = "Waveform Audio File Format",
            Category = FileCategory.Audio,
            PrimaryExtension = ".wav",
            MimeType = "audio/wav",
            Signatures = new[] { new byte[] { 0x52, 0x49, 0x46, 0x46 } },
            SupportsLossless = true,
            SupportsMetadata = true,
            Description = "Uncompressed PCM audio standard on Windows.",
            CompatibleTargetFormatIds = new[] { "mp3", "flac", "aac", "m4a", "ogg", "opus", "wma" }
        });

        Register(new FormatDefinition
        {
            Id = "flac",
            Name = "Free Lossless Audio Codec",
            Category = FileCategory.Audio,
            PrimaryExtension = ".flac",
            MimeType = "audio/flac",
            Signatures = new[] { new byte[] { 0x66, 0x4C, 0x61, 0x43 } },
            SupportsLossless = true,
            SupportsMetadata = true,
            Description = "Open lossless compressed audio format with bit-perfect fidelity.",
            CompatibleTargetFormatIds = new[] { "mp3", "wav", "aac", "m4a", "ogg", "opus" }
        });

        Register(new FormatDefinition
        {
            Id = "aac",
            Name = "Advanced Audio Coding",
            Category = FileCategory.Audio,
            PrimaryExtension = ".aac",
            MimeType = "audio/aac",
            Signatures = new[] { new byte[] { 0xFF, 0xF1 }, new byte[] { 0xFF, 0xF9 } },
            SupportsLossless = false,
            SupportsMetadata = true,
            Description = "High-efficiency lossy audio compression standard.",
            CompatibleTargetFormatIds = new[] { "mp3", "wav", "flac", "m4a", "ogg", "opus" }
        });

        Register(new FormatDefinition
        {
            Id = "m4a",
            Name = "MPEG-4 Audio",
            Category = FileCategory.Audio,
            PrimaryExtension = ".m4a",
            MimeType = "audio/mp4",
            SupportsLossless = false,
            SupportsMetadata = true,
            Description = "Apple audio container using AAC or ALAC compression.",
            CompatibleTargetFormatIds = new[] { "mp3", "wav", "flac", "aac", "ogg", "opus" }
        });

        Register(new FormatDefinition
        {
            Id = "ogg",
            Name = "Ogg Vorbis Audio",
            Category = FileCategory.Audio,
            PrimaryExtension = ".ogg",
            AlternativeExtensions = new[] { ".oga" },
            MimeType = "audio/ogg",
            Signatures = new[] { new byte[] { 0x4F, 0x67, 0x67, 0x53 } },
            SupportsLossless = false,
            SupportsMetadata = true,
            Description = "Open-source patent-free lossy audio compression.",
            CompatibleTargetFormatIds = new[] { "mp3", "wav", "flac", "aac", "m4a", "opus" }
        });

        Register(new FormatDefinition
        {
            Id = "opus",
            Name = "Opus Interactive Audio",
            Category = FileCategory.Audio,
            PrimaryExtension = ".opus",
            MimeType = "audio/opus",
            Signatures = new[] { new byte[] { 0x4F, 0x67, 0x67, 0x53 } },
            SupportsLossless = false,
            SupportsMetadata = true,
            Description = "State-of-the-art highly versatile low-latency audio codec.",
            CompatibleTargetFormatIds = new[] { "mp3", "wav", "flac", "aac", "m4a", "ogg" }
        });

        Register(new FormatDefinition
        {
            Id = "wma",
            Name = "Windows Media Audio",
            Category = FileCategory.Audio,
            PrimaryExtension = ".wma",
            MimeType = "audio/x-ms-wma",
            Signatures = new[] { new byte[] { 0x30, 0x26, 0xB2, 0x75 } },
            SupportsLossless = false,
            SupportsMetadata = true,
            Description = "Legacy proprietary Microsoft audio format.",
            CompatibleTargetFormatIds = new[] { "mp3", "wav", "flac", "aac", "m4a", "ogg" }
        });

        // ------------------ VIDEO ------------------
        Register(new FormatDefinition
        {
            Id = "mp4",
            Name = "MPEG-4 Part 14 Video",
            Category = FileCategory.Video,
            PrimaryExtension = ".mp4",
            AlternativeExtensions = new[] { ".m4v" },
            MimeType = "video/mp4",
            SupportsLossless = false,
            SupportsMetadata = true,
            Description = "Most widely compatible video container format.",
            CompatibleTargetFormatIds = new[] { "mkv", "webm", "mov", "avi", "wmv", "gif", "mp3", "wav", "flac", "m4a" }
        });

        Register(new FormatDefinition
        {
            Id = "mkv",
            Name = "Matroska Multimedia Container",
            Category = FileCategory.Video,
            PrimaryExtension = ".mkv",
            MimeType = "video/x-matroska",
            Signatures = new[] { new byte[] { 0x1A, 0x45, 0xDF, 0xA3 } },
            SupportsLossless = false,
            SupportsMetadata = true,
            Description = "Flexible open standard video container holding multi-track audio and subtitles.",
            CompatibleTargetFormatIds = new[] { "mp4", "webm", "mov", "avi", "wmv", "gif", "mp3", "wav", "flac" }
        });

        Register(new FormatDefinition
        {
            Id = "webm",
            Name = "WebM Video",
            Category = FileCategory.Video,
            PrimaryExtension = ".webm",
            MimeType = "video/webm",
            Signatures = new[] { new byte[] { 0x1A, 0x45, 0xDF, 0xA3 } },
            SupportsLossless = false,
            SupportsMetadata = true,
            Description = "Royalty-free HTML5 video container using VP8/VP9 or AV1.",
            CompatibleTargetFormatIds = new[] { "mp4", "mkv", "mov", "avi", "gif", "mp3", "ogg", "opus" }
        });

        Register(new FormatDefinition
        {
            Id = "mov",
            Name = "QuickTime Movie",
            Category = FileCategory.Video,
            PrimaryExtension = ".mov",
            MimeType = "video/quicktime",
            SupportsLossless = false,
            SupportsMetadata = true,
            Description = "Apple QuickTime container widely used in editing suites.",
            CompatibleTargetFormatIds = new[] { "mp4", "mkv", "webm", "avi", "gif", "mp3", "wav", "m4a" }
        });

        Register(new FormatDefinition
        {
            Id = "avi",
            Name = "Audio Video Interleave",
            Category = FileCategory.Video,
            PrimaryExtension = ".avi",
            MimeType = "video/x-msvideo",
            Signatures = new[] { new byte[] { 0x52, 0x49, 0x46, 0x46 } },
            SupportsLossless = false,
            SupportsMetadata = true,
            Description = "Microsoft multimedia container format.",
            CompatibleTargetFormatIds = new[] { "mp4", "mkv", "webm", "mov", "wmv", "mp3", "wav" }
        });

        Register(new FormatDefinition
        {
            Id = "wmv",
            Name = "Windows Media Video",
            Category = FileCategory.Video,
            PrimaryExtension = ".wmv",
            MimeType = "video/x-ms-wmv",
            Signatures = new[] { new byte[] { 0x30, 0x26, 0xB2, 0x75 } },
            SupportsLossless = false,
            SupportsMetadata = true,
            Description = "Microsoft proprietary video compression format.",
            CompatibleTargetFormatIds = new[] { "mp4", "mkv", "webm", "mov", "avi", "mp3", "wav" }
        });

        // ------------------ PDF ------------------
        Register(new FormatDefinition
        {
            Id = "pdf",
            Name = "Portable Document Format",
            Category = FileCategory.Pdf,
            PrimaryExtension = ".pdf",
            MimeType = "application/pdf",
            Signatures = new[] { new byte[] { 0x25, 0x50, 0x44, 0x46 } }, // %PDF
            SupportsLossless = true,
            SupportsMetadata = true,
            Description = "Standard layout-preserving document specification.",
            CompatibleTargetFormatIds = new[] { "png", "jpg", "tiff", "txt" },
            KnownLimitations = new[] { "PDF to text extracts raw text streams; layout styling is not reconstructed." }
        });

        // ------------------ DOCUMENTS ------------------
        Register(new FormatDefinition
        {
            Id = "docx",
            Name = "Microsoft Word Document",
            Category = FileCategory.Document,
            PrimaryExtension = ".docx",
            MimeType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            Signatures = new[] { new byte[] { 0x50, 0x4B, 0x03, 0x04 } }, // PK zip
            SupportsLossless = false,
            SupportsMetadata = true,
            Description = "Modern OpenXML Word document format.",
            CompatibleTargetFormatIds = new[] { "pdf", "txt", "html", "md" },
            KnownLimitations = new[] { "Converting to plain text or markdown discards complex margins and table formatting." }
        });

        Register(new FormatDefinition
        {
            Id = "txt",
            Name = "Plain Text",
            Category = FileCategory.Document,
            PrimaryExtension = ".txt",
            MimeType = "text/plain",
            SupportsLossless = true,
            SupportsMetadata = false,
            Description = "Universal unformatted UTF-8 text.",
            CompatibleTargetFormatIds = new[] { "pdf", "html", "md" }
        });

        Register(new FormatDefinition
        {
            Id = "md",
            Name = "Markdown Document",
            Category = FileCategory.Document,
            PrimaryExtension = ".md",
            AlternativeExtensions = new[] { ".markdown" },
            MimeType = "text/markdown",
            SupportsLossless = true,
            SupportsMetadata = false,
            Description = "Lightweight markup format popular with technical writers.",
            CompatibleTargetFormatIds = new[] { "html", "txt", "pdf" }
        });

        Register(new FormatDefinition
        {
            Id = "html",
            Name = "HyperText Markup Language",
            Category = FileCategory.Document,
            PrimaryExtension = ".html",
            AlternativeExtensions = new[] { ".htm" },
            MimeType = "text/html",
            SupportsLossless = true,
            SupportsMetadata = false,
            Description = "Standard webpage format.",
            CompatibleTargetFormatIds = new[] { "txt", "md", "pdf" }
        });

        Register(new FormatDefinition
        {
            Id = "rtf",
            Name = "Rich Text Format",
            Category = FileCategory.Document,
            PrimaryExtension = ".rtf",
            MimeType = "application/rtf",
            Signatures = new[] { new byte[] { 0x7B, 0x5C, 0x72, 0x74, 0x66 } }, // {\rtf
            SupportsLossless = false,
            SupportsMetadata = false,
            Description = "Cross-platform formatted text format developed by Microsoft.",
            CompatibleTargetFormatIds = new[] { "txt", "html", "pdf" }
        });

        // ------------------ SPREADSHEETS ------------------
        Register(new FormatDefinition
        {
            Id = "xlsx",
            Name = "Microsoft Excel Workbook",
            Category = FileCategory.Spreadsheet,
            PrimaryExtension = ".xlsx",
            MimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            Signatures = new[] { new byte[] { 0x50, 0x4B, 0x03, 0x04 } },
            SupportsLossless = false,
            SupportsMetadata = true,
            Description = "OpenXML spreadsheet with multi-sheet and formula capabilities.",
            CompatibleTargetFormatIds = new[] { "csv", "json", "html", "pdf" },
            KnownLimitations = new[] { "Exporting to CSV flattens data and preserves only the active sheet without formulas." }
        });

        Register(new FormatDefinition
        {
            Id = "csv",
            Name = "Comma-Separated Values",
            Category = FileCategory.Spreadsheet,
            PrimaryExtension = ".csv",
            MimeType = "text/csv",
            SupportsLossless = true,
            SupportsMetadata = false,
            Description = "Delimited plain text data table format.",
            CompatibleTargetFormatIds = new[] { "xlsx", "json", "html" },
            KnownLimitations = new[] { "CSV cannot store formatting, formulas, cell styles, or multiple sheets." }
        });

        Register(new FormatDefinition
        {
            Id = "json",
            Name = "JavaScript Object Notation",
            Category = FileCategory.Spreadsheet,
            PrimaryExtension = ".json",
            MimeType = "application/json",
            SupportsLossless = true,
            SupportsMetadata = false,
            Description = "Structured data format.",
            CompatibleTargetFormatIds = new[] { "csv" }
        });

        // ------------------ E-BOOKS ------------------
        Register(new FormatDefinition
        {
            Id = "epub",
            Name = "Electronic Publication",
            Category = FileCategory.Ebook,
            PrimaryExtension = ".epub",
            MimeType = "application/epub+zip",
            Signatures = new[] { new byte[] { 0x50, 0x4B, 0x03, 0x04 } },
            SupportsLossless = false,
            SupportsMetadata = true,
            Description = "Standard reflowable e-book format based on HTML and XML.",
            CompatibleTargetFormatIds = new[] { "txt", "html", "pdf" },
            KnownLimitations = new[] { "DRM-protected EPUBs cannot be converted. Only DRM-free books are supported." }
        });

        // ------------------ ARCHIVES ------------------
        Register(new FormatDefinition
        {
            Id = "zip",
            Name = "ZIP Archive",
            Category = FileCategory.Archive,
            PrimaryExtension = ".zip",
            MimeType = "application/zip",
            Signatures = new[] { new byte[] { 0x50, 0x4B, 0x03, 0x04 } },
            SupportsLossless = true,
            SupportsMetadata = true,
            Description = "Universal archive format supporting compression and multi-file storage.",
            CompatibleTargetFormatIds = new[] { "tar", "tar.gz", "7z" }
        });

        Register(new FormatDefinition
        {
            Id = "7z",
            Name = "7-Zip Compressed Archive",
            Category = FileCategory.Archive,
            PrimaryExtension = ".7z",
            MimeType = "application/x-7z-compressed",
            Signatures = new[] { new byte[] { 0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C } },
            SupportsLossless = true,
            SupportsMetadata = true,
            Description = "High-ratio compressed archive format developed for 7-Zip.",
            CompatibleTargetFormatIds = new[] { "zip", "tar" }
        });

        Register(new FormatDefinition
        {
            Id = "tar",
            Name = "Tape Archive",
            Category = FileCategory.Archive,
            PrimaryExtension = ".tar",
            MimeType = "application/x-tar",
            SupportsLossless = true,
            SupportsMetadata = true,
            Description = "POSIX archive format preserving file attributes without compression.",
            CompatibleTargetFormatIds = new[] { "zip", "tar.gz", "7z" }
        });

        Register(new FormatDefinition
        {
            Id = "gz",
            Name = "Gzip Compressed Archive",
            Category = FileCategory.Archive,
            PrimaryExtension = ".gz",
            MimeType = "application/gzip",
            Signatures = new[] { new byte[] { 0x1F, 0x8B } },
            SupportsLossless = true,
            SupportsMetadata = false,
            Description = "Single-file gzip compression standard.",
            CompatibleTargetFormatIds = new[] { "zip", "tar" }
        });
    }

    private void Register(FormatDefinition format)
    {
        _formats.Add(format);
        _formatsById[format.Id] = format;
        _formatsByExt[format.PrimaryExtension.TrimStart('.')] = format;
        foreach (var alt in format.AlternativeExtensions)
        {
            _formatsByExt[alt.TrimStart('.')] = format;
        }
    }

    public IReadOnlyList<FormatDefinition> GetAllFormats() => _formats;

    public FormatDefinition? GetFormatById(string formatId)
    {
        if (string.IsNullOrWhiteSpace(formatId)) return null;
        return _formatsById.TryGetValue(formatId, out var f) ? f : null;
    }

    public FormatDefinition? GetFormatByExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension)) return null;
        var clean = extension.Trim().TrimStart('.').ToLowerInvariant();
        return _formatsByExt.TryGetValue(clean, out var f) ? f : null;
    }

    public FormatDefinition? IdentifyBySignature(ReadOnlySpan<byte> headerBytes, string fallbackExtension)
    {
        if (headerBytes.Length == 0)
        {
            return GetFormatByExtension(fallbackExtension);
        }

        // 1. High-priority magic byte matches
        // Check PNG
        if (headerBytes.Length >= 8 && headerBytes[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
            return GetFormatById("png");

        // Check JPEG
        if (headerBytes.Length >= 3 && headerBytes[0] == 0xFF && headerBytes[1] == 0xD8 && headerBytes[2] == 0xFF)
            return GetFormatById("jpg");

        // Check GIF
        if (headerBytes.Length >= 6)
        {
            var headerStr = System.Text.Encoding.ASCII.GetString(headerBytes[..6]);
            if (headerStr == "GIF87a" || headerStr == "GIF89a")
                return GetFormatById("gif");
        }

        // Check BMP
        if (headerBytes.Length >= 2 && headerBytes[0] == 0x42 && headerBytes[1] == 0x4D)
            return GetFormatById("bmp");

        // Check TIFF
        if (headerBytes.Length >= 4)
        {
            if ((headerBytes[0] == 0x49 && headerBytes[1] == 0x49 && headerBytes[2] == 0x2A && headerBytes[3] == 0x00) ||
                (headerBytes[0] == 0x4D && headerBytes[1] == 0x4D && headerBytes[2] == 0x00 && headerBytes[3] == 0x2A))
                return GetFormatById("tiff");
        }

        // Check PDF
        if (headerBytes.Length >= 4 && headerBytes[..4].SequenceEqual(new byte[] { 0x25, 0x50, 0x44, 0x46 }))
            return GetFormatById("pdf");

        // Check 7Z
        if (headerBytes.Length >= 6 && headerBytes[..6].SequenceEqual(new byte[] { 0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C }))
            return GetFormatById("7z");

        // Check GZ
        if (headerBytes.Length >= 2 && headerBytes[0] == 0x1F && headerBytes[1] == 0x8B)
            return GetFormatById("gz");

        // Check FLAC
        if (headerBytes.Length >= 4 && headerBytes[..4].SequenceEqual(new byte[] { 0x66, 0x4C, 0x61, 0x43 }))
            return GetFormatById("flac");

        // Check Ogg (OGG / OPUS)
        if (headerBytes.Length >= 4 && headerBytes[..4].SequenceEqual(new byte[] { 0x4F, 0x67, 0x67, 0x53 }))
        {
            var extFmt = GetFormatByExtension(fallbackExtension);
            if (extFmt?.Id == "opus") return extFmt;
            return GetFormatById("ogg");
        }

        // Check MKV / WebM (EBML)
        if (headerBytes.Length >= 4 && headerBytes[..4].SequenceEqual(new byte[] { 0x1A, 0x45, 0xDF, 0xA3 }))
        {
            var extFmt = GetFormatByExtension(fallbackExtension);
            if (extFmt?.Id == "webm") return extFmt;
            return GetFormatById("mkv");
        }

        // Check RIFF containers: WebP, WAV, AVI
        if (headerBytes.Length >= 12 && headerBytes[..4].SequenceEqual(new byte[] { 0x52, 0x49, 0x46, 0x46 }))
        {
            var subType = System.Text.Encoding.ASCII.GetString(headerBytes.Slice(8, 4));
            if (subType.Equals("WEBP", StringComparison.OrdinalIgnoreCase)) return GetFormatById("webp");
            if (subType.Equals("WAVE", StringComparison.OrdinalIgnoreCase)) return GetFormatById("wav");
            if (subType.Equals("AVI ", StringComparison.OrdinalIgnoreCase)) return GetFormatById("avi");
        }

        // Check MP3
        if (headerBytes.Length >= 3 && headerBytes[0] == 0x49 && headerBytes[1] == 0x44 && headerBytes[2] == 0x33)
            return GetFormatById("mp3");
        if (headerBytes.Length >= 2 && headerBytes[0] == 0xFF && (headerBytes[1] & 0xE0) == 0xE0)
            return GetFormatById("mp3");

        // Check MP4 / MOV / M4A (ftyp / moov at offset 4)
        if (headerBytes.Length >= 8)
        {
            var tag = System.Text.Encoding.ASCII.GetString(headerBytes.Slice(4, 4));
            if (tag == "ftyp" || tag == "moov")
            {
                var extFmt = GetFormatByExtension(fallbackExtension);
                if (extFmt != null && (extFmt.Id == "m4a" || extFmt.Id == "mov"))
                    return extFmt;
                return GetFormatById("mp4");
            }
        }

        // Check RTF
        if (headerBytes.Length >= 5 && headerBytes[..5].SequenceEqual(new byte[] { 0x7B, 0x5C, 0x72, 0x74, 0x66 }))
            return GetFormatById("rtf");

        // Check ZIP containers: ZIP, DOCX, XLSX, PPTX, EPUB
        if (headerBytes.Length >= 4 && headerBytes[0] == 0x50 && headerBytes[1] == 0x4B &&
            (headerBytes[2] == 0x03 || headerBytes[2] == 0x05 || headerBytes[2] == 0x07))
        {
            var extFmt = GetFormatByExtension(fallbackExtension);
            if (extFmt != null && (extFmt.Id == "docx" || extFmt.Id == "xlsx" || extFmt.Id == "epub"))
                return extFmt;
            return GetFormatById("zip");
        }

        // Fallback to extension
        return GetFormatByExtension(fallbackExtension);
    }

    public IReadOnlyList<FormatDefinition> GetCompatibleTargets(FormatDefinition source, IReadOnlyList<IConversionEngine>? activeEngines = null)
    {
        var result = new List<FormatDefinition>();
        foreach (var targetId in source.CompatibleTargetFormatIds)
        {
            var target = GetFormatById(targetId);
            if (target == null) continue;

            if (activeEngines != null && activeEngines.Count > 0)
            {
                if (activeEngines.Any(e => e.IsAvailable && e.CanConvert(source, target)))
                {
                    result.Add(target);
                }
            }
            else
            {
                result.Add(target);
            }
        }
        return result;
    }

    public bool CanConvert(string sourceFormatId, string targetFormatId, IReadOnlyList<IConversionEngine>? activeEngines = null)
    {
        var src = GetFormatById(sourceFormatId);
        var tgt = GetFormatById(targetFormatId);
        if (src == null || tgt == null) return false;

        if (!src.CompatibleTargetFormatIds.Contains(tgt.Id, StringComparer.OrdinalIgnoreCase))
            return false;

        if (activeEngines != null && activeEngines.Count > 0)
        {
            return activeEngines.Any(e => e.IsAvailable && e.CanConvert(src, tgt));
        }

        return true;
    }
}
