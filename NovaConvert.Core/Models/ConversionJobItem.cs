using System.ComponentModel;
using System.Runtime.CompilerServices;
using NovaConvert.Core.Models;

namespace NovaConvert.Core.Models;

public class ConversionJobItem : INotifyPropertyChanged
{
    private JobStatus _status = JobStatus.Queued;
    private double _progressPercentage;
    private string _statusText = "Ready";
    private FormatDefinition? _targetFormat;
    private string? _outputPath;
    private long _outputSizeBytes;
    private string? _errorMessage;
    private string? _warningMessage;
    private string? _engineUsed;

    public Guid Id { get; } = Guid.NewGuid();
    public required string SourcePath { get; init; }
    public required string FileName { get; init; }
    public required long FileSizeBytes { get; init; }
    public required FileCategory Category { get; init; }
    public required FormatDefinition SourceFormat { get; init; }

    public IReadOnlyList<FormatDefinition> AvailableTargetFormats { get; init; } = Array.Empty<FormatDefinition>();
    public ConversionOptions Options { get; set; } = new();

    public bool HasExtensionMismatch { get; init; }
    public string? MismatchWarning { get; init; }

    public FormatDefinition? TargetFormat
    {
        get => _targetFormat;
        set
        {
            if (_targetFormat != value)
            {
                _targetFormat = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanConvert));
            }
        }
    }

    public JobStatus Status
    {
        get => _status;
        set
        {
            if (_status != value)
            {
                _status = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsRunning));
                OnPropertyChanged(nameof(IsCompleted));
                OnPropertyChanged(nameof(IsFailed));
                OnPropertyChanged(nameof(CanConvert));
            }
        }
    }

    public double ProgressPercentage
    {
        get => _progressPercentage;
        set
        {
            if (Math.Abs(_progressPercentage - value) > 0.001)
            {
                _progressPercentage = value;
                OnPropertyChanged();
            }
        }
    }

    public string StatusText
    {
        get => _statusText;
        set
        {
            if (_statusText != value)
            {
                _statusText = value;
                OnPropertyChanged();
            }
        }
    }

    public string? OutputPath
    {
        get => _outputPath;
        set
        {
            if (_outputPath != value)
            {
                _outputPath = value;
                OnPropertyChanged();
            }
        }
    }

    public long OutputSizeBytes
    {
        get => _outputSizeBytes;
        set
        {
            if (_outputSizeBytes != value)
            {
                _outputSizeBytes = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FormattedOutputSize));
            }
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (_errorMessage != value)
            {
                _errorMessage = value;
                OnPropertyChanged();
            }
        }
    }

    public string? WarningMessage
    {
        get => _warningMessage;
        set
        {
            if (_warningMessage != value)
            {
                _warningMessage = value;
                OnPropertyChanged();
            }
        }
    }

    public string? EngineUsed
    {
        get => _engineUsed;
        set
        {
            if (_engineUsed != value)
            {
                _engineUsed = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsRunning => Status == JobStatus.Converting || Status == JobStatus.Inspecting;
    public bool IsCompleted => Status == JobStatus.Completed;
    public bool IsFailed => Status == JobStatus.Failed;
    public bool CanConvert => Status != JobStatus.Converting && TargetFormat != null;

    public string FormattedSourceSize => FormatBytes(FileSizeBytes);
    public string FormattedOutputSize => OutputSizeBytes > 0 ? FormatBytes(OutputSizeBytes) : "-";

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private static string FormatBytes(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB" };
        int counter = 0;
        decimal number = bytes;
        while (Math.Round(number / 1024) >= 1 && counter < suffixes.Length - 1)
        {
            number /= 1024;
            counter++;
        }
        return $"{number:n1} {suffixes[counter]}";
    }
}
