using NovaConvert.Core.Interfaces;
using NovaConvert.Core.Models;

namespace NovaConvert.Core.Services;

public class FileInspectionService : IFileInspectionService
{
    private readonly IFormatRegistry _registry;

    public FileInspectionService(IFormatRegistry registry)
    {
        _registry = registry;
    }

    public async Task<FileInspectionResult> InspectFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return new FileInspectionResult
            {
                FilePath = filePath ?? string.Empty,
                FileName = Path.GetFileName(filePath ?? string.Empty),
                FileSizeBytes = 0,
                DeclaredExtension = string.Empty,
                IsCorruptedOrInvalid = true,
                ErrorMessage = "File does not exist or path is invalid."
            };
        }

        var fileInfo = new FileInfo(filePath);
        var ext = fileInfo.Extension.ToLowerInvariant();
        var declaredFormat = _registry.GetFormatByExtension(ext);

        byte[] headerBytes = new byte[Math.Min(4096, (int)Math.Min(fileInfo.Length, 4096))];
        try
        {
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
            {
                if (headerBytes.Length > 0)
                {
                    int bytesRead = await stream.ReadAsync(headerBytes.AsMemory(0, headerBytes.Length), cancellationToken);
                    if (bytesRead < headerBytes.Length)
                    {
                        Array.Resize(ref headerBytes, bytesRead);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            return new FileInspectionResult
            {
                FilePath = filePath,
                FileName = fileInfo.Name,
                FileSizeBytes = fileInfo.Length,
                DeclaredExtension = ext,
                ExtensionFormat = declaredFormat,
                IsCorruptedOrInvalid = true,
                ErrorMessage = $"Could not read file header: {ex.Message}"
            };
        }

        var detectedFormat = _registry.IdentifyBySignature(headerBytes, ext);
        var metadata = new Dictionary<string, string>();
        metadata["FileSize"] = FormatFileSize(fileInfo.Length);
        metadata["Extension"] = ext;

        bool hasMismatch = false;
        string? mismatchWarning = null;

        if (detectedFormat != null && declaredFormat != null)
        {
            // If categories differ or IDs differ and neither is an alias
            if (!detectedFormat.Id.Equals(declaredFormat.Id, StringComparison.OrdinalIgnoreCase) &&
                !detectedFormat.AlternativeExtensions.Any(a => a.TrimStart('.').Equals(ext.TrimStart('.'), StringComparison.OrdinalIgnoreCase)))
            {
                hasMismatch = true;
                mismatchWarning = $"File is named '{fileInfo.Name}' with extension '{ext}', but file signature identifies it as '{detectedFormat.Name}' ({detectedFormat.PrimaryExtension.ToUpperInvariant()}). NOVA Convert will use its true format.";
            }
        }
        else if (detectedFormat != null && declaredFormat == null)
        {
            hasMismatch = true;
            mismatchWarning = $"File extension '{ext}' is unrecognized, but internal signature identifies this file as '{detectedFormat.Name}'.";
        }

        return new FileInspectionResult
        {
            FilePath = filePath,
            FileName = fileInfo.Name,
            FileSizeBytes = fileInfo.Length,
            DeclaredExtension = ext,
            DetectedFormat = detectedFormat ?? declaredFormat,
            ExtensionFormat = declaredFormat,
            HasExtensionMismatch = hasMismatch,
            MismatchWarning = mismatchWarning,
            Metadata = metadata,
            IsCorruptedOrInvalid = false
        };
    }

    private static string FormatFileSize(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int counter = 0;
        decimal number = bytes;
        while (Math.Round(number / 1024) >= 1)
        {
            number /= 1024;
            counter++;
        }
        return $"{number:n1} {suffixes[counter]}";
    }
}
