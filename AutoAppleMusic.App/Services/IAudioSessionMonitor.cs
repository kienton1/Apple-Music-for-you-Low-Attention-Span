namespace AutoAppleMusic.App.Services;

public interface IAudioSessionMonitor
{
    Task<AudioSessionSnapshot> GetSnapshotAsync(CancellationToken cancellationToken);
}
