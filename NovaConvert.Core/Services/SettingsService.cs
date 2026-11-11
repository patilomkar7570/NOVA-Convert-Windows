using System.Text.Json;
using NovaConvert.Core.Interfaces;
using NovaConvert.Core.Models;

namespace NovaConvert.Core.Services;

public class SettingsService : ISettingsService
{
    private readonly string _settingsFilePath;
    public AppSettings Settings { get; private set; } = new();

    public SettingsService(string? customPath = null)
    {
        if (string.IsNullOrWhiteSpace(customPath))
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var dir = Path.Combine(appData, "NOVA Convert");
            Directory.CreateDirectory(dir);
            _settingsFilePath = Path.Combine(dir, "settings.json");
        }
        else
        {
            _settingsFilePath = customPath;
        }

        // Initialize default output directory if empty
        if (string.IsNullOrWhiteSpace(Settings.OutputDirectory))
        {
            var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            Settings.OutputDirectory = Path.Combine(docs, "NOVA Converts");
        }
    }

    public async Task LoadAsync()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = await File.ReadAllTextAsync(_settingsFilePath);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json);
                if (loaded != null)
                {
                    Settings = loaded;
                }
            }
        }
        catch
        {
            // Fall back to defaults
        }

        if (string.IsNullOrWhiteSpace(Settings.OutputDirectory))
        {
            var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            Settings.OutputDirectory = Path.Combine(docs, "NOVA Converts");
        }
    }

    public async Task SaveAsync()
    {
        try
        {
            var dir = Path.GetDirectoryName(_settingsFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_settingsFilePath, json);
        }
        catch
        {
            // Non-fatal
        }
    }

    public string ResolveOutputPath(string sourcePath, string targetExtension)
    {
        if (!targetExtension.StartsWith('.'))
        {
            targetExtension = "." + targetExtension;
        }

        string targetDir;
        if (Settings.SaveInSourceDirectory && !string.IsNullOrWhiteSpace(sourcePath) && File.Exists(sourcePath))
        {
            targetDir = Path.GetDirectoryName(sourcePath) ?? Settings.OutputDirectory;
        }
        else
        {
            targetDir = Settings.OutputDirectory;
        }

        Directory.CreateDirectory(targetDir);

        string sourceBaseName = Path.GetFileNameWithoutExtension(sourcePath);
        string candidate = Path.Combine(targetDir, $"{sourceBaseName}{targetExtension}");

        if (Settings.OverwritePolicy == OverwriteAction.CreateUniqueName)
        {
            int counter = 1;
            while (File.Exists(candidate))
            {
                candidate = Path.Combine(targetDir, $"{sourceBaseName} ({counter}){targetExtension}");
                counter++;
            }
        }

        return candidate;
    }
}
