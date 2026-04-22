using AutoAppleMusic.Core;

namespace AutoAppleMusic.App.Services;

public interface IAppleMusicController
{
    Task<AppleMusicSnapshot> GetSnapshotAsync(CancellationToken cancellationToken);

    Task<bool> PauseAsync(CancellationToken cancellationToken);

    Task<bool> PlayAsync(CancellationToken cancellationToken);

    Task<bool> LaunchAsync(CancellationToken cancellationToken);
}
