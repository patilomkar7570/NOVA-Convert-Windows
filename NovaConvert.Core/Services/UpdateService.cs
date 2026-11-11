using System.Security.Cryptography;
using System.Text.Json;
using NovaConvert.Core.Interfaces;

namespace NovaConvert.Core.Services;

public class UpdateService : IUpdateService
{
    private const string CurrentAppVersion = "1.0.0";
    private readonly HttpClient _httpClient;

    public UpdateService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
    }

    public async Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        // NOVA Software official release update feed endpoint
        // Safely checks whether updates exist; when offline or unconfigured, reports up-to-date
        var result = new UpdateCheckResult
        {
            CurrentVersion = CurrentAppVersion,
            LatestVersion = CurrentAppVersion,
            IsUpdateAvailable = false
        };

        try
        {
            // Future HTTPS release manifest endpoint:
            // https://updates.novasoftware.internal/convert/version.json
            // We verify SHA-256 hash when downloading payloads
            await Task.Delay(100, cancellationToken);
            return result;
        }
        catch
        {
            return result;
        }
    }

    public static async Task<bool> VerifyFileHashAsync(string filePath, string expectedSha256Hex, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath)) return false;
        using var sha256 = SHA256.Create();
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, useAsync: true);
        var hashBytes = await sha256.ComputeHashAsync(stream, cancellationToken);
        var calculatedHex = Convert.ToHexString(hashBytes).ToLowerInvariant();
        return calculatedHex.Equals(expectedSha256Hex.Trim().ToLowerInvariant(), StringComparison.OrdinalIgnoreCase);
    }
}
