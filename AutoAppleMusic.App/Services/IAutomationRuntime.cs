using AutoAppleMusic.App.Models;

namespace AutoAppleMusic.App.Services;

public interface IAutomationRuntime : IAsyncDisposable
{
    Task<RuntimeStatus> GetStatusAsync(CancellationToken cancellationToken);

    Task<RuntimeStatus> SetEnabledAsync(bool isEnabled, CancellationToken cancellationToken);
}
