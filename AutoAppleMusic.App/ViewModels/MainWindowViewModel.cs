using System.Windows.Threading;
using AutoAppleMusic.App.Infrastructure;
using AutoAppleMusic.App.Models;
using AutoAppleMusic.App.Services;

namespace AutoAppleMusic.App.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
{
    private readonly IAutomationRuntime _automationRuntime;
    private readonly Dispatcher _dispatcher;
    private readonly AsyncRelayCommand _toggleAutomationCommand;
    private readonly DispatcherTimer _pollTimer;
    private readonly SemaphoreSlim _runtimeOperationGate = new(1, 1);

    private bool _isAutomationEnabled;
    private bool _isBusy;
    private string _statusLine = "Automation is paused.";
    private string _statusDetail = "Turn it on when you want Apple Music to slip behind YouTube and return when the room goes quiet.";
    private string _toggleLabel = "Automation off";
    private string _toggleGlyph = ">";

    public MainWindowViewModel(IAutomationRuntime automationRuntime, Dispatcher dispatcher)
    {
        _automationRuntime = automationRuntime;
        _dispatcher = dispatcher;
        _toggleAutomationCommand = new AsyncRelayCommand(ToggleAutomationAsync, () => !IsBusy);
        _pollTimer = new DispatcherTimer(DispatcherPriority.Background, dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(900),
        };
        _pollTimer.Tick += OnPollTimerTick;
        _pollTimer.Start();
        _ = RefreshAsync();
    }

    public bool IsAutomationEnabled
    {
        get => _isAutomationEnabled;
        private set
        {
            if (SetProperty(ref _isAutomationEnabled, value))
            {
                ToggleLabel = value ? "Automation on" : "Automation off";
                ToggleGlyph = value ? "II" : ">";
            }
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                _toggleAutomationCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string ToggleLabel
    {
        get => _toggleLabel;
        private set => SetProperty(ref _toggleLabel, value);
    }

    public string ToggleGlyph
    {
        get => _toggleGlyph;
        private set => SetProperty(ref _toggleGlyph, value);
    }

    public string StatusLine
    {
        get => _statusLine;
        private set => SetProperty(ref _statusLine, value);
    }

    public string StatusDetail
    {
        get => _statusDetail;
        private set => SetProperty(ref _statusDetail, value);
    }

    public AsyncRelayCommand ToggleAutomationCommand => _toggleAutomationCommand;

    private async Task ToggleAutomationAsync()
    {
        IsBusy = true;
        var gateAcquired = false;

        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            await _runtimeOperationGate.WaitAsync(timeout.Token).ConfigureAwait(false);
            gateAcquired = true;

            try
            {
                var status = await _automationRuntime.SetEnabledAsync(!IsAutomationEnabled, timeout.Token).ConfigureAwait(false);
                await _dispatcher.InvokeAsync(() => ApplyStatus(status));
            }
            finally
            {
                if (gateAcquired)
                {
                    _runtimeOperationGate.Release();
                }
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async void OnPollTimerTick(object? sender, EventArgs e)
    {
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        if (IsBusy || !_runtimeOperationGate.Wait(0))
        {
            return;
        }

        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            var status = await _automationRuntime.GetStatusAsync(timeout.Token).ConfigureAwait(false);
            await _dispatcher.InvokeAsync(() => ApplyStatus(status));
        }
        catch
        {
            // Keep the last visible state if polling fails. The next cycle can recover.
        }
        finally
        {
            _runtimeOperationGate.Release();
        }
    }

    private void ApplyStatus(RuntimeStatus status)
    {
        IsAutomationEnabled = status.IsAutomationEnabled;
        ToggleGlyph = status.ToggleGlyph;
        ToggleLabel = status.ToggleLabel;
        StatusLine = status.StatusLine;
        StatusDetail = status.StatusDetail;
    }
}
