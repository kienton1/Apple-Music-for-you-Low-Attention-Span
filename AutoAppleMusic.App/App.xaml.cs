using System.Windows;
using AutoAppleMusic.App.Services;
using AutoAppleMusic.App.Services.Windows;
using AutoAppleMusic.App.ViewModels;

namespace AutoAppleMusic.App;

public partial class App : Application
{
    private IAutomationRuntime? _automationRuntime;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var audioMonitor = new WindowsAudioSessionMonitor();
        var appleMusicController = new WindowsAppleMusicController();
        _automationRuntime = new AppAutomationRuntime(audioMonitor, appleMusicController);

        var viewModel = new MainWindowViewModel(_automationRuntime, Dispatcher);
        var mainWindow = new MainWindow
        {
            DataContext = viewModel
        };

        MainWindow = mainWindow;
        mainWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_automationRuntime is not null)
        {
            await _automationRuntime.DisposeAsync();
        }

        base.OnExit(e);
    }
}
