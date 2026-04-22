using System.Diagnostics;
using System.Globalization;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace AutoAppleMusic.App.Services.Windows;

public sealed class WindowsAudioSessionMonitor : IAudioSessionMonitor
{
    private static readonly TimeSpan AudibleHoldWindow = TimeSpan.FromMilliseconds(1500);
    private static readonly HashSet<string> AppleMusicProcessNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "AppleMusic",
        "AMPLibraryAgent",
    };
    private const float PeakThreshold = 0.01f;

    private readonly Dictionary<string, DateTimeOffset> _recentlyAudibleSessions = new(StringComparer.OrdinalIgnoreCase);

    public Task<AudioSessionSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var enumerator = new MMDeviceEnumerator();
        using var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        var sessionCollection = device.AudioSessionManager.Sessions;
        var activeSources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var now = DateTimeOffset.UtcNow;

        for (var index = 0; index < sessionCollection.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var session = sessionCollection[index];
            if (session.IsSystemSoundsSession)
            {
                continue;
            }

            var processName = TryGetProcessName(session);
            var sourceName = ResolveSourceName(session, processName);
            if (IsAppleMusic(processName, sourceName))
            {
                continue;
            }

            var peak = session.AudioMeterInformation.MasterPeakValue;
            var sessionKey = $"{sourceName}:{session.GetProcessID}";

            if (peak >= PeakThreshold)
            {
                _recentlyAudibleSessions[sessionKey] = now;
            }

            if (!ShouldTreatAsActive(session, sessionKey, peak, now))
            {
                continue;
            }

            activeSources.Add(sourceName);
        }

        PruneStaleEntries(now);

        return Task.FromResult(new AudioSessionSnapshot(
            activeSources.Count > 0,
            activeSources.OrderBy(static name => name, StringComparer.OrdinalIgnoreCase).ToArray()));
    }

    private bool ShouldTreatAsActive(AudioSessionControl session, string sessionKey, float peak, DateTimeOffset now)
    {
        if (peak >= PeakThreshold)
        {
            return true;
        }

        if (!_recentlyAudibleSessions.TryGetValue(sessionKey, out var lastAudible))
        {
            return false;
        }

        return session.State == AudioSessionState.AudioSessionStateActive &&
               now - lastAudible <= AudibleHoldWindow;
    }

    private void PruneStaleEntries(DateTimeOffset now)
    {
        var expiredKeys = _recentlyAudibleSessions
            .Where(pair => now - pair.Value > AudibleHoldWindow)
            .Select(pair => pair.Key)
            .ToArray();

        foreach (var key in expiredKeys)
        {
            _recentlyAudibleSessions.Remove(key);
        }
    }

    private static string? TryGetProcessName(AudioSessionControl session)
    {
        try
        {
            var process = Process.GetProcessById((int)session.GetProcessID);
            if (!string.IsNullOrWhiteSpace(process.ProcessName))
            {
                return process.ProcessName;
            }
        }
        catch
        {
            // Fall back to session metadata when the process is unavailable.
        }

        return null;
    }

    private static string ResolveSourceName(AudioSessionControl session, string? processName)
    {
        if (!string.IsNullOrWhiteSpace(processName))
        {
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(processName);
        }

        return string.IsNullOrWhiteSpace(session.DisplayName) ? "Unknown audio" : session.DisplayName;
    }

    private static bool IsAppleMusic(string? processName, string sourceName) =>
        !string.IsNullOrWhiteSpace(processName) && AppleMusicProcessNames.Contains(processName) ||
        sourceName.Contains("apple music", StringComparison.OrdinalIgnoreCase) ||
        sourceName.Contains("applemusic", StringComparison.OrdinalIgnoreCase);
}
