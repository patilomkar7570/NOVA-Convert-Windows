using Microsoft.Win32;
using NovaConvert.Core.Interfaces;
using NovaConvert.Core.Models;
using NovaConvert.UI.Common;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace NovaConvert.UI.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly IConversionQueueManager _queueManager;
    private readonly IFormatRegistry _registry;
    private readonly IEngineManager _engineManager;
    private readonly ISettingsService _settingsService;
    private readonly IHistoryRepository _historyRepository;
    private readonly IUpdateService _updateService;

    private string _activeView = "Converter"; // "Converter", "History", "Engines", "Settings", "Advanced"
    private bool _isDarkMode = true;
    private FormatDefinition? _selectedBatchTarget;
    private QualityPreset _selectedPreset = QualityPreset.Auto;
    private ConversionJobItem? _selectedJobItem;

    private string? _toastMessage;
    private string? _toastFilePath;
    private bool _isToastVisible;

    public IConversionQueueManager QueueManager => _queueManager;
    public ObservableCollection<FormatDefinition> BatchTargets { get; } = new();
    public ObservableCollection<HistoryEntry> RecentHistory { get; } = new();
    public IReadOnlyList<IConversionEngine> Engines => _engineManager.Engines;

    private string _selectedTheme = "Auto";
    public ObservableCollection<string> AvailableThemes { get; } = new() { "Auto", "Dark", "Light" };

    public string ActiveView
    {
        get => _activeView;
        set
        {
            if (_activeView != value)
            {
                _activeView = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsConverterView));
                OnPropertyChanged(nameof(IsHistoryOpen));
                OnPropertyChanged(nameof(IsEnginesOpen));
                OnPropertyChanged(nameof(IsSettingsOpen));
                OnPropertyChanged(nameof(IsAdvancedOpen));
            }
        }
    }

    public bool IsConverterView => _activeView == "Converter";
    public bool IsHistoryOpen => _activeView == "History";
    public bool IsEnginesOpen => _activeView == "Engines";
    public bool IsSettingsOpen => _activeView == "Settings";
    public bool IsAdvancedOpen => _activeView == "Advanced";

    public string SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            if (_selectedTheme != value)
            {
                _selectedTheme = value;
                _settingsService.Settings.Theme = value;
                _ = _settingsService.SaveAsync();
                OnPropertyChanged();
                ApplyTheme();
            }
        }
    }

    public bool IsDarkMode
    {
        get => _isDarkMode;
        set
        {
            if (_isDarkMode != value)
            {
                _isDarkMode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LogoPath));
            }
        }
    }

    public string LogoPath => IsDarkMode ? "Assets/Brand/nova-logo-white.png" : "Assets/Brand/nova-logo.png";

    public FormatDefinition? SelectedBatchTarget
    {
        get => _selectedBatchTarget;
        set
        {
            if (_selectedBatchTarget != value)
            {
                _selectedBatchTarget = value;
                OnPropertyChanged();
                if (value != null)
                {
                    _queueManager.ApplyTargetFormatToAll(value);
                }
            }
        }
    }

    public QualityPreset SelectedPreset
    {
        get => _selectedPreset;
        set
        {
            if (_selectedPreset != value)
            {
                _selectedPreset = value;
                OnPropertyChanged();
                foreach (var item in _queueManager.Queue)
                {
                    item.Options = item.Options with { Preset = value };
                }
            }
        }
    }

    public ConversionJobItem? SelectedJobItem
    {
        get => _selectedJobItem;
        set { if (_selectedJobItem != value) { _selectedJobItem = value; OnPropertyChanged(); } }
    }

    public string OutputFolder => _settingsService.Settings.OutputDirectory;

    public string? ToastMessage
    {
        get => _toastMessage;
        set { if (_toastMessage != value) { _toastMessage = value; OnPropertyChanged(); } }
    }

    public string? ToastFilePath
    {
        get => _toastFilePath;
        set { if (_toastFilePath != value) { _toastFilePath = value; OnPropertyChanged(); } }
    }

    public bool IsToastVisible
    {
        get => _isToastVisible;
        set { if (_isToastVisible != value) { _isToastVisible = value; OnPropertyChanged(); } }
    }

    // Commands
    public ICommand AddFilesCommand { get; }
    public ICommand AddFolderCommand { get; }
    public ICommand ConvertAllCommand { get; }
    public ICommand PauseResumeCommand { get; }
    public ICommand CancelAllCommand { get; }
    public ICommand ClearCompletedCommand { get; }
    public ICommand ClearAllCommand { get; }
    public ICommand RemoveItemCommand { get; }
    public ICommand ChooseOutputFolderCommand { get; }
    public ICommand OpenOutputFolderCommand { get; }
    public ICommand OpenFileCommand { get; }
    public ICommand OpenHistoryCommand { get; }
    public ICommand OpenEnginesCommand { get; }
    public ICommand OpenSettingsCommand { get; }
    public ICommand OpenAdvancedOptionsCommand { get; }
    public ICommand CloseModalCommand { get; }
    public ICommand ToggleThemeCommand { get; }
    public ICommand ClearHistoryCommand { get; }
    public ICommand RetryFailedCommand { get; }

    public MainViewModel(
        IConversionQueueManager queueManager,
        IFormatRegistry registry,
        IEngineManager engineManager,
        ISettingsService settingsService,
        IHistoryRepository historyRepository,
        IUpdateService updateService)
    {
        _queueManager = queueManager;
        _registry = registry;
        _engineManager = engineManager;
        _settingsService = settingsService;
        _historyRepository = historyRepository;
        _updateService = updateService;

        _queueManager.QueueStateChanged += (s, e) =>
        {
            UpdateBatchTargets();
            OnPropertyChanged(nameof(QueueManager));
            CheckCompletionAndShowToast();
        };

        _isDarkMode = _settingsService.Settings.Theme != "Light";

        // Initialize Commands
        AddFilesCommand = new AsyncRelayCommand(OnAddFilesAsync);
        AddFolderCommand = new AsyncRelayCommand(OnAddFolderAsync);
        ConvertAllCommand = new AsyncRelayCommand(OnConvertAllAsync);
        PauseResumeCommand = new RelayCommand(OnPauseResume);
        CancelAllCommand = new RelayCommand(() => _queueManager.CancelAll());
        ClearCompletedCommand = new RelayCommand(() => _queueManager.ClearCompleted());
        ClearAllCommand = new RelayCommand(() => _queueManager.ClearAll());
        RemoveItemCommand = new RelayCommand(item =>
        {
            if (item is ConversionJobItem job) _queueManager.RemoveItem(job);
        });
        ChooseOutputFolderCommand = new RelayCommand(OnChooseOutputFolder);
        OpenOutputFolderCommand = new RelayCommand(OnOpenOutputFolder);
        OpenFileCommand = new RelayCommand(path =>
        {
            if (path is string p && File.Exists(p))
            {
                try { Process.Start(new ProcessStartInfo(p) { UseShellExecute = true }); } catch { }
            }
        });
        OpenHistoryCommand = new AsyncRelayCommand(OnOpenHistoryAsync);
        OpenEnginesCommand = new RelayCommand(() => ActiveView = "Engines");
        OpenSettingsCommand = new RelayCommand(() => ActiveView = "Settings");
        OpenAdvancedOptionsCommand = new RelayCommand(item =>
        {
            if (item is ConversionJobItem job)
            {
                SelectedJobItem = job;
                ActiveView = "Advanced";
            }
        });
        CloseModalCommand = new RelayCommand(() => ActiveView = "Converter");
        ToggleThemeCommand = new RelayCommand(() => IsDarkMode = !IsDarkMode);
        ClearHistoryCommand = new AsyncRelayCommand(async () =>
        {
            await _historyRepository.ClearHistoryAsync();
            RecentHistory.Clear();
        });
        RetryFailedCommand = new AsyncRelayCommand(() => _queueManager.RetryFailedAsync());
    }

    public async Task InitializeAsync()
    {
        await _settingsService.LoadAsync();
        await _historyRepository.InitializeAsync();
        await _engineManager.InitializeAsync();
        _selectedTheme = string.IsNullOrWhiteSpace(_settingsService.Settings.Theme) ? "Auto" : _settingsService.Settings.Theme;
        OnPropertyChanged(nameof(SelectedTheme));
        ApplyTheme();
        OnPropertyChanged(nameof(OutputFolder));
    }

    public async Task HandleDroppedFilesAsync(string[] paths)
    {
        await _queueManager.AddFilesAsync(paths);
    }

    private async Task OnAddFilesAsync()
    {
        var dialog = new OpenFileDialog
        {
            Multiselect = true,
            Title = "Select Files to Convert — NOVA Convert",
            Filter = "All Supported Files|*.png;*.jpg;*.jpeg;*.webp;*.tiff;*.bmp;*.gif;*.ico;*.svg;*.avif;*.tga;*.psd;*.mp3;*.wav;*.flac;*.aac;*.m4a;*.ogg;*.opus;*.wma;*.aiff;*.aif;*.mp4;*.mkv;*.mov;*.webm;*.avi;*.wmv;*.flv;*.pdf;*.docx;*.txt;*.md;*.html;*.rtf;*.xlsx;*.csv;*.json;*.epub;*.zip;*.7z;*.tar;*.gz|All Files (*.*)|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            await _queueManager.AddFilesAsync(dialog.FileNames);
        }
    }

    private async Task OnAddFolderAsync()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select Folder Containing Files to Convert — NOVA Convert"
        };

        if (dialog.ShowDialog() == true)
        {
            await _queueManager.AddFilesAsync(new[] { dialog.FolderName });
        }
    }

    private async Task OnConvertAllAsync()
    {
        await _queueManager.StartAsync();
    }

    private void OnPauseResume()
    {
        if (_queueManager.IsPaused)
        {
            _queueManager.Resume();
        }
        else if (_queueManager.IsRunning)
        {
            _queueManager.Pause();
        }
        else
        {
            _ = _queueManager.StartAsync();
        }
    }

    private void OnChooseOutputFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select Output Folder for Converted Files"
        };

        if (dialog.ShowDialog() == true)
        {
            _settingsService.Settings.OutputDirectory = dialog.FolderName;
            _ = _settingsService.SaveAsync();
            OnPropertyChanged(nameof(OutputFolder));
        }
    }

    private void OnOpenOutputFolder()
    {
        var folder = _settingsService.Settings.OutputDirectory;
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
        try
        {
            Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true });
        }
        catch { }
    }

    private async Task OnOpenHistoryAsync()
    {
        ActiveView = "History";
        RecentHistory.Clear();
        var entries = await _historyRepository.GetRecentEntriesAsync(100);
        foreach (var e in entries)
        {
            RecentHistory.Add(e);
        }
    }

    private void UpdateBatchTargets()
    {
        BatchTargets.Clear();
        var allTargets = _queueManager.Queue
            .SelectMany(q => q.AvailableTargetFormats)
            .DistinctBy(f => f.Id)
            .ToList();

        foreach (var t in allTargets)
        {
            BatchTargets.Add(t);
        }

        if (SelectedBatchTarget != null && !BatchTargets.Contains(SelectedBatchTarget))
        {
            SelectedBatchTarget = null;
        }
    }

    private void CheckCompletionAndShowToast()
    {
        if (!_queueManager.IsRunning && _queueManager.TotalCount > 0 &&
            _queueManager.Queue.All(i => i.Status == JobStatus.Completed || i.Status == JobStatus.Failed || i.Status == JobStatus.Cancelled))
        {
            int completed = _queueManager.CompletedCount;
            int failed = _queueManager.FailedCount;

            if (completed > 0)
            {
                var lastCompleted = _queueManager.Queue.LastOrDefault(i => i.Status == JobStatus.Completed);
                ToastFilePath = lastCompleted?.OutputPath;
                ToastMessage = failed == 0
                    ? $"Successfully converted {completed} file{(completed > 1 ? "s" : "")}!"
                    : $"Converted {completed} file{(completed > 1 ? "s" : "")}, {failed} failed.";
                IsToastVisible = true;
            }
        }
    }

    private bool GetEffectiveDarkMode()
    {
        if (string.Equals(_selectedTheme, "Dark", StringComparison.OrdinalIgnoreCase))
            return true;
        if (string.Equals(_selectedTheme, "Light", StringComparison.OrdinalIgnoreCase))
            return false;

        // "Auto" -> detect Windows system theme
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var val = key?.GetValue("AppsUseLightTheme");
            if (val is int lightInt)
            {
                return lightInt == 0; // 0 = Dark, 1 = Light
            }
        }
        catch
        {
            // Non-fatal fallback
        }
        return true; // Default dark
    }

    private void ApplyTheme()
    {
        var app = Application.Current;
        if (app == null) return;

        bool isDark = GetEffectiveDarkMode();
        _isDarkMode = isDark;
        OnPropertyChanged(nameof(IsDarkMode));
        OnPropertyChanged(nameof(LogoPath));

        var bg = isDark ? Color.FromRgb(15, 17, 23) : Color.FromRgb(248, 250, 252);
        var surface = isDark ? Color.FromRgb(24, 27, 36) : Color.FromRgb(255, 255, 255);
        var card = isDark ? Color.FromRgb(30, 34, 46) : Color.FromRgb(241, 245, 249);
        var border = isDark ? Color.FromRgb(46, 52, 70) : Color.FromRgb(226, 232, 240);
        var textPrimary = isDark ? Color.FromRgb(241, 245, 249) : Color.FromRgb(15, 23, 42);
        var textSecondary = isDark ? Color.FromRgb(148, 163, 184) : Color.FromRgb(100, 116, 139);

        app.Resources["AppBgBrush"] = new SolidColorBrush(bg);
        app.Resources["AppSurfaceBrush"] = new SolidColorBrush(surface);
        app.Resources["AppCardBrush"] = new SolidColorBrush(card);
        app.Resources["AppBorderBrush"] = new SolidColorBrush(border);
        app.Resources["TextPrimaryBrush"] = new SolidColorBrush(textPrimary);
        app.Resources["TextSecondaryBrush"] = new SolidColorBrush(textSecondary);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
