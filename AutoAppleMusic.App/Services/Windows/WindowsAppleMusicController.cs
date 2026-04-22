using System.Diagnostics;
using AutoAppleMusic.Core;
using Windows.Media.Control;

namespace AutoAppleMusic.App.Services.Windows;

public sealed class WindowsAppleMusicController : IAppleMusicController
{
    public async Task<AppleMusicSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        var session = await FindAppleMusicSessionAsync(cancellationToken).ConfigureAwait(false);
        if (session is null)
        {
            return new AppleMusicSnapshot(false, PlaybackState.Unknown, null);
        }

        var playbackInfo = session.GetPlaybackInfo();
        return new AppleMusicSnapshot(true, MapPlaybackState(playbackInfo.PlaybackStatus), session.SourceAppUserModelId);
    }

    public async Task<bool> PauseAsync(CancellationToken cancellationToken)
    {
        var session = await FindAppleMusicSessionAsync(cancellationToken).ConfigureAwait(false);
        return session is not null && await session.TryPauseAsync();
    }

    public async Task<bool> PlayAsync(CancellationToken cancellationToken)
    {
        var session = await FindAppleMusicSessionAsync(cancellationToken).ConfigureAwait(false);
        return session is not null && await session.TryPlayAsync();
    }

    public async Task<bool> LaunchAsync(CancellationToken cancellationToken)
    {
        if (Process.GetProcessesByName("AppleMusic").Length > 0)
        {
            _ = await PlayAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }

        var startAppId = await FindAppleMusicStartAppIdAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(startAppId))
        {
            return false;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"shell:AppsFolder\\{startAppId}",
            UseShellExecute = true,
        });

        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);
        _ = await PlayAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    private static PlaybackState MapPlaybackState(GlobalSystemMediaTransportControlsSessionPlaybackStatus status) =>
        status switch
        {
            GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing => PlaybackState.Playing,
            GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused => PlaybackState.Paused,
            GlobalSystemMediaTransportControlsSessionPlaybackStatus.Stopped => PlaybackState.Stopped,
            _ => PlaybackState.Unknown,
        };

    private static async Task<GlobalSystemMediaTransportControlsSession?> FindAppleMusicSessionAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
        foreach (var session in manager.GetSessions())
        {
            if (session.SourceAppUserModelId.Contains("applemusic", StringComparison.OrdinalIgnoreCase))
            {
                return session;
            }
        }

        return null;
    }

    private static async Task<string?> FindAppleMusicStartAppIdAsync(CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = "-NoProfile -Command \"Get-StartApps | Where-Object { $_.Name -like 'Apple Music*' } | Select-Object -First 1 -ExpandProperty AppID\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        return string.IsNullOrWhiteSpace(output) ? null : output.Trim();
    }
}
