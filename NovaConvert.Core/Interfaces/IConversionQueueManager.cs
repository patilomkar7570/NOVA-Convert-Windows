using System.Collections.ObjectModel;
using NovaConvert.Core.Models;

namespace NovaConvert.Core.Interfaces;

public interface IConversionQueueManager
{
    ObservableCollection<ConversionJobItem> Queue { get; }
    bool IsRunning { get; }
    bool IsPaused { get; }
    int TotalCount { get; }
    int CompletedCount { get; }
    int FailedCount { get; }

    Task AddFilesAsync(IEnumerable<string> filePaths, CancellationToken cancellationToken = default);
    void RemoveItem(ConversionJobItem item);
    void ClearCompleted();
    void ClearAll();
    void ApplyTargetFormatToAll(FormatDefinition target);
    Task StartAsync();
    void Pause();
    void Resume();
    void CancelItem(ConversionJobItem item);
    void CancelAll();
    Task RetryFailedAsync();

    event EventHandler? QueueStateChanged;
}
