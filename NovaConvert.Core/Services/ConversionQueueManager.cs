using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using NovaConvert.Core.Interfaces;
using NovaConvert.Core.Models;

namespace NovaConvert.Core.Services;

public class ConversionQueueManager : IConversionQueueManager
{
    private readonly IFormatRegistry _registry;
    private readonly IFileInspectionService _inspector;
    private readonly IEngineManager _engineManager;
    private readonly ISettingsService _settingsService;
    private readonly IHistoryRepository _historyRepository;

    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _itemCancellationTokens = new();
    private CancellationTokenSource? _globalCts;
    private readonly object _stateLock = new();

    public ObservableCollection<ConversionJobItem> Queue { get; } = new();

    public bool IsRunning { get; private set; }
    public bool IsPaused { get; private set; }
    public int TotalCount => Queue.Count;
    public int CompletedCount => Queue.Count(i => i.Status == JobStatus.Completed);
    public int FailedCount => Queue.Count(i => i.Status == JobStatus.Failed);

    public event EventHandler? QueueStateChanged;

    public ConversionQueueManager(
        IFormatRegistry registry,
        IFileInspectionService inspector,
        IEngineManager engineManager,
        ISettingsService settingsService,
        IHistoryRepository historyRepository)
    {
        _registry = registry;
        _inspector = inspector;
        _engineManager = engineManager;
        _settingsService = settingsService;
        _historyRepository = historyRepository;
    }

    public async Task AddFilesAsync(IEnumerable<string> filePaths, CancellationToken cancellationToken = default)
    {
        foreach (var path in filePaths)
        {
            if (string.IsNullOrWhiteSpace(path)) continue;

            // If it's a directory, recursively inspect all files in the directory
            if (Directory.Exists(path))
            {
                var filesInDir = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
                await AddFilesAsync(filesInDir, cancellationToken);
                continue;
            }

            if (!File.Exists(path)) continue;

            // Avoid duplicate active paths
            if (Queue.Any(q => q.SourcePath.Equals(path, StringComparison.OrdinalIgnoreCase) && q.Status != JobStatus.Completed))
            {
                continue;
            }

            var inspection = await _inspector.InspectFileAsync(path, cancellationToken);
            if (inspection.DetectedFormat == null) continue;

            var compatibleTargets = _registry.GetCompatibleTargets(inspection.DetectedFormat, _engineManager.Engines);
            var defaultTarget = compatibleTargets.FirstOrDefault();

            var job = new ConversionJobItem
            {
                SourcePath = path,
                FileName = inspection.FileName,
                FileSizeBytes = inspection.FileSizeBytes,
                Category = inspection.DetectedFormat.Category,
                SourceFormat = inspection.DetectedFormat,
                TargetFormat = defaultTarget,
                AvailableTargetFormats = compatibleTargets,
                HasExtensionMismatch = inspection.HasExtensionMismatch,
                MismatchWarning = inspection.MismatchWarning,
                Options = new ConversionOptions
                {
                    Preset = _settingsService.Settings.DefaultQualityPreset,
                    PreserveMetadata = _settingsService.Settings.PreserveMetadataByDefault
                }
            };

            Queue.Add(job);
        }

        NotifyStateChanged();
    }

    public void RemoveItem(ConversionJobItem item)
    {
        if (item.IsRunning)
        {
            CancelItem(item);
        }
        Queue.Remove(item);
        NotifyStateChanged();
    }

    public void ClearCompleted()
    {
        var completed = Queue.Where(i => i.Status == JobStatus.Completed).ToList();
        foreach (var item in completed)
        {
            Queue.Remove(item);
        }
        NotifyStateChanged();
    }

    public void ClearAll()
    {
        CancelAll();
        Queue.Clear();
        NotifyStateChanged();
    }

    public void ApplyTargetFormatToAll(FormatDefinition target)
    {
        foreach (var item in Queue)
        {
            if (item.Status != JobStatus.Converting && item.AvailableTargetFormats.Any(t => t.Id == target.Id))
            {
                item.TargetFormat = item.AvailableTargetFormats.First(t => t.Id == target.Id);
            }
        }
    }

    public async Task StartAsync()
    {
        lock (_stateLock)
        {
            if (IsRunning && !IsPaused) return;
            IsRunning = true;
            IsPaused = false;
            _globalCts = new CancellationTokenSource();
        }

        NotifyStateChanged();

        var pending = Queue.Where(i => i.Status == JobStatus.Queued || i.Status == JobStatus.Paused || i.Status == JobStatus.Failed).ToList();
        int maxConcurrency = Math.Max(1, _settingsService.Settings.MaxConcurrentJobs);
        using var semaphore = new SemaphoreSlim(maxConcurrency);

        var tasks = pending.Select(async item =>
        {
            await semaphore.WaitAsync(_globalCts.Token);
            try
            {
                if (_globalCts.Token.IsCancellationRequested || IsPaused) return;
                await ProcessItemAsync(item, _globalCts.Token);
            }
            finally
            {
                semaphore.Release();
            }
        });

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException) { }
        finally
        {
            lock (_stateLock)
            {
                IsRunning = false;
            }
            NotifyStateChanged();
        }
    }

    public void Pause()
    {
        lock (_stateLock)
        {
            IsPaused = true;
            _globalCts?.Cancel();
        }
        NotifyStateChanged();
    }

    public void Resume()
    {
        _ = StartAsync();
    }

    public void CancelItem(ConversionJobItem item)
    {
        if (_itemCancellationTokens.TryRemove(item.Id, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }
        item.Status = JobStatus.Cancelled;
        item.StatusText = "Cancelled";
        NotifyStateChanged();
    }

    public void CancelAll()
    {
        lock (_stateLock)
        {
            _globalCts?.Cancel();
            foreach (var kvp in _itemCancellationTokens)
            {
                kvp.Value.Cancel();
                kvp.Value.Dispose();
            }
            _itemCancellationTokens.Clear();
            IsRunning = false;
            IsPaused = false;
        }

        foreach (var item in Queue)
        {
            if (item.IsRunning)
            {
                item.Status = JobStatus.Cancelled;
                item.StatusText = "Cancelled";
            }
        }
        NotifyStateChanged();
    }

    public async Task RetryFailedAsync()
    {
        foreach (var item in Queue.Where(i => i.Status == JobStatus.Failed))
        {
            item.Status = JobStatus.Queued;
            item.StatusText = "Queued for retry";
            item.ErrorMessage = null;
            item.ProgressPercentage = 0;
        }
        await StartAsync();
    }

    private async Task ProcessItemAsync(ConversionJobItem item, CancellationToken globalToken)
    {
        if (item.TargetFormat == null)
        {
            item.Status = JobStatus.Failed;
            item.StatusText = "No target format selected";
            item.ErrorMessage = "Please select an output format before converting.";
            return;
        }

        var engine = _engineManager.ResolveBestEngine(item.SourceFormat, item.TargetFormat);
        if (engine == null)
        {
            item.Status = JobStatus.Failed;
            item.StatusText = "No compatible engine found";
            item.ErrorMessage = $"No available engine could convert {item.SourceFormat.Name} to {item.TargetFormat.Name}.";
            return;
        }

        using var itemCts = CancellationTokenSource.CreateLinkedTokenSource(globalToken);
        _itemCancellationTokens[item.Id] = itemCts;

        item.Status = JobStatus.Converting;
        item.StatusText = "Converting...";
        item.ProgressPercentage = 0;
        item.ErrorMessage = null;

        var progress = new Progress<double>(val =>
        {
            item.ProgressPercentage = Math.Clamp(val * 100.0, 0, 100.0);
            item.StatusText = $"Converting ({item.ProgressPercentage:0}%)";
        });

        string outputPath = _settingsService.ResolveOutputPath(item.SourcePath, item.TargetFormat.PrimaryExtension);
        item.OutputPath = outputPath;

        try
        {
            var result = await engine.ConvertAsync(
                item.SourcePath,
                outputPath,
                item.SourceFormat,
                item.TargetFormat,
                item.Options,
                progress,
                itemCts.Token);

            if (result.Success && result.OutputValidated)
            {
                item.Status = JobStatus.Completed;
                item.StatusText = "Completed";
                item.ProgressPercentage = 100;
                item.OutputPath = result.OutputPath;
                item.OutputSizeBytes = result.OutputSizeBytes;
                item.WarningMessage = result.WarningMessage;
                item.EngineUsed = result.EngineUsed;

                // Log to persistent SQLite database
                await _historyRepository.AddEntryAsync(new HistoryEntry
                {
                    TimestampUtc = DateTime.UtcNow,
                    SourceFilePath = item.SourcePath,
                    OutputFilePath = outputPath,
                    SourceFormat = item.SourceFormat.PrimaryExtension.ToUpperInvariant(),
                    TargetFormat = item.TargetFormat.PrimaryExtension.ToUpperInvariant(),
                    SourceSizeBytes = item.FileSizeBytes,
                    OutputSizeBytes = result.OutputSizeBytes,
                    DurationSeconds = result.Duration.TotalSeconds,
                    EngineUsed = result.EngineUsed ?? engine.DisplayName,
                    Success = true
                });
            }
            else
            {
                item.Status = JobStatus.Failed;
                item.StatusText = "Conversion Failed";
                item.ErrorMessage = result.ErrorMessage ?? "Conversion failed without details.";
                try { if (File.Exists(outputPath)) File.Delete(outputPath); } catch { }

                await _historyRepository.AddEntryAsync(new HistoryEntry
                {
                    TimestampUtc = DateTime.UtcNow,
                    SourceFilePath = item.SourcePath,
                    OutputFilePath = outputPath,
                    SourceFormat = item.SourceFormat.PrimaryExtension.ToUpperInvariant(),
                    TargetFormat = item.TargetFormat.PrimaryExtension.ToUpperInvariant(),
                    SourceSizeBytes = item.FileSizeBytes,
                    OutputSizeBytes = 0,
                    DurationSeconds = result.Duration.TotalSeconds,
                    EngineUsed = result.EngineUsed ?? engine.DisplayName,
                    Success = false,
                    ErrorMessage = item.ErrorMessage
                });
            }
        }
        catch (OperationCanceledException)
        {
            item.Status = JobStatus.Cancelled;
            item.StatusText = "Cancelled";
            try { if (File.Exists(outputPath)) File.Delete(outputPath); } catch { }
        }
        catch (Exception ex)
        {
            item.Status = JobStatus.Failed;
            item.StatusText = "Error";
            item.ErrorMessage = ex.Message;
            try { if (File.Exists(outputPath)) File.Delete(outputPath); } catch { }
        }
        finally
        {
            _itemCancellationTokens.TryRemove(item.Id, out _);
            NotifyStateChanged();
        }
    }

    private void NotifyStateChanged()
    {
        QueueStateChanged?.Invoke(this, EventArgs.Empty);
    }
}
