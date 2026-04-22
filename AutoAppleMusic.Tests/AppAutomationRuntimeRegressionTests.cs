using AutoAppleMusic.App.Services;
using AutoAppleMusic.Core;

namespace AutoAppleMusic.Tests;

public sealed class AppAutomationRuntimeRegressionTests
{
    [Fact]
    public async Task Enabling_after_a_disabled_observation_executes_the_immediate_resume_command()
    {
        var audioMonitor = new StubAudioSessionMonitor(
            new AudioSessionSnapshot(false, []));
        var appleMusicController = new StubAppleMusicController(
            new AppleMusicSnapshot(true, PlaybackState.Paused, "AppleMusic"));

        await using var runtime = new AppAutomationRuntime(audioMonitor, appleMusicController);

        var disabledStatus = await runtime.GetStatusAsync(CancellationToken.None);
        Assert.False(disabledStatus.IsAutomationEnabled);

        await runtime.SetEnabledAsync(true, CancellationToken.None);

        Assert.Equal(1, appleMusicController.PlayCallCount);
    }

    [Fact]
    public async Task Failed_pause_is_retried_on_the_next_poll_while_external_audio_is_still_active()
    {
        var audioMonitor = new StubAudioSessionMonitor(
            new AudioSessionSnapshot(false, []),
            new AudioSessionSnapshot(true, ["YouTube"]),
            new AudioSessionSnapshot(true, ["YouTube"]));
        var appleMusicController = new StubAppleMusicController(
            new AppleMusicSnapshot(true, PlaybackState.Playing, "AppleMusic"))
        {
            PauseResult = false,
        };

        await using var runtime = new AppAutomationRuntime(audioMonitor, appleMusicController);

        await runtime.SetEnabledAsync(true, CancellationToken.None);
        await runtime.GetStatusAsync(CancellationToken.None);
        await runtime.GetStatusAsync(CancellationToken.None);

        Assert.Equal(2, appleMusicController.PauseCallCount);
    }

    private sealed class StubAudioSessionMonitor(params AudioSessionSnapshot[] snapshots) : IAudioSessionMonitor
    {
        private readonly Queue<AudioSessionSnapshot> _snapshots = new(snapshots);
        private AudioSessionSnapshot _lastSnapshot = snapshots[^1];

        public Task<AudioSessionSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_snapshots.Count > 0)
            {
                _lastSnapshot = _snapshots.Dequeue();
            }

            return Task.FromResult(_lastSnapshot);
        }
    }

    private sealed class StubAppleMusicController(AppleMusicSnapshot initialSnapshot) : IAppleMusicController
    {
        private AppleMusicSnapshot _snapshot = initialSnapshot;

        public bool PauseResult { get; init; } = true;

        public bool PlayResult { get; init; } = true;

        public bool LaunchResult { get; init; } = true;

        public int PauseCallCount { get; private set; }

        public int PlayCallCount { get; private set; }

        public int LaunchCallCount { get; private set; }

        public Task<AppleMusicSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_snapshot);
        }

        public Task<bool> PauseAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            PauseCallCount++;

            if (PauseResult)
            {
                _snapshot = _snapshot with { PlaybackState = PlaybackState.Paused };
            }

            return Task.FromResult(PauseResult);
        }

        public Task<bool> PlayAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            PlayCallCount++;

            if (PlayResult)
            {
                _snapshot = _snapshot with
                {
                    IsAvailable = true,
                    PlaybackState = PlaybackState.Playing,
                };
            }

            return Task.FromResult(PlayResult);
        }

        public Task<bool> LaunchAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LaunchCallCount++;

            if (LaunchResult)
            {
                _snapshot = _snapshot with { IsAvailable = true };
            }

            return Task.FromResult(LaunchResult);
        }
    }
}
