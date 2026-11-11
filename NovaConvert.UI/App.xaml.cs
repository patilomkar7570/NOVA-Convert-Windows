using Microsoft.Extensions.DependencyInjection;
using NovaConvert.Core.Interfaces;
using NovaConvert.Core.Services;
using NovaConvert.Engines;
using NovaConvert.UI.ViewModels;
using System.Windows;

namespace NovaConvert.UI;

public partial class App : Application
{
    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    private async void Application_Startup(object sender, StartupEventArgs e)
    {
        var services = new ServiceCollection();

        // Core Services
        services.AddSingleton<IFormatRegistry, FormatRegistry>();
        services.AddSingleton<IFileInspectionService, FileInspectionService>();
        services.AddSingleton<IEngineManager, EngineManager>();
        services.AddSingleton<IHistoryRepository, SqliteHistoryRepository>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IUpdateService, UpdateService>();
        services.AddSingleton<IConversionQueueManager, ConversionQueueManager>();

        // ViewModels & Views
        services.AddSingleton<MainViewModel>();
        services.AddTransient<MainWindow>();

        ServiceProvider = services.BuildServiceProvider();

        // Initialize Services & ViewModel
        var vm = ServiceProvider.GetRequiredService<MainViewModel>();
        await vm.InitializeAsync();

        // Launch MainWindow
        var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
        mainWindow.DataContext = vm;
        mainWindow.Show();
    }
}
