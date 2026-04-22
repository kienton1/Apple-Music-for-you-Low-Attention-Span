using AutoAppleMusic.App.Models;
using AutoAppleMusic.Core;

namespace AutoAppleMusic.App.Services;

public sealed class AppAutomationRuntime : IAutomationRuntime
{
    private readonly IAudioSessionMonitor _audioMonitor;
    private readonly IAppleMusicController _appleMusicController;
    private AutomationStateMachine _stateMachine = new();

    public AppAutomationRuntime(
        IAudioSessionMonitor audioMonitor,
        IAppleMusicController appleMusicController)
    {
        _audioMonitor = audioMonitor;
        _appleMusicController = appleMusicController;
    }

    public async Task<RuntimeStatus> GetStatusAsync(CancellationToken cancellationToken)
    {
        return await RefreshAsync(Array.Empty<DesiredAction>(), cancellationToken).ConfigureAwait(false);
    }

    public async Task<RuntimeStatus> SetEnabledAsync(bool isEnabled, CancellationToken cancellationToken)
    {
        var decision = _stateMachine.SetEnabled(isEnabled);
        await ExecuteActionsAsync(decision.Actions, cancellationToken).ConfigureAwait(false);
        return await RefreshAsync(decision.Actions, cancellationToken).ConfigureAwait(false);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private async Task<RuntimeStatus> RefreshAsync(
        IReadOnlyList<DesiredAction> priorActions,
        CancellationToken cancellationToken)
    {
        var audioSnapshot = await _audioMonitor.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
        var appleMusicSnapshot = await _appleMusicController.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);

        var decision = _stateMachine.Observe(
            audioSnapshot.IsExternalAudioActive,
            appleMusicSnapshot.IsAvailable,
            appleMusicSnapshot.PlaybackState);

        await ExecuteActionsAsync(decision.Actions, cancellationToken).ConfigureAwait(false);
        return BuildStatus(
            decision.Snapshot,
            audioSnapshot,
            appleMusicSnapshot,
            CombineActions(priorActions, decision.Actions));
    }

    private async Task ExecuteActionsAsync(IReadOnlyList<DesiredAction> actions, CancellationToken cancellationToken)
    {
        foreach (var action in actions)
        {
            bool succeeded;

            try
            {
                succeeded = action switch
                {
                    DesiredAction.PauseAppleMusic =>
                        await _appleMusicController.PauseAsync(cancellationToken).ConfigureAwait(false),
                    DesiredAction.PlayAppleMusic =>
                        await _appleMusicController.PlayAsync(cancellationToken).ConfigureAwait(false),
                    DesiredAction.LaunchAppleMusic =>
                        await _appleMusicController.LaunchAsync(cancellationToken).ConfigureAwait(false),
                    _ => true,
                };
            }
            catch
            {
                ClearPendingAction(action);
                throw;
            }

            if (!succeeded)
            {
                ClearPendingAction(action);
            }
        }
    }

    private void ClearPendingAction(DesiredAction action)
    {
        var snapshot = _stateMachine.Snapshot;
        if (snapshot.PendingAction != action)
        {
            return;
        }

        _stateMachine = new AutomationStateMachine(snapshot with { PendingAction = null });
    }

    private static IReadOnlyList<DesiredAction> CombineActions(
        IReadOnlyList<DesiredAction> priorActions,
        IReadOnlyList<DesiredAction> currentActions)
    {
        if (priorActions.Count == 0)
        {
            return currentActions;
        }

        if (currentActions.Count == 0)
        {
            return priorActions;
        }

        var combined = new DesiredAction[priorActions.Count + currentActions.Count];
        for (var index = 0; index < priorActions.Count; index++)
        {
            combined[index] = priorActions[index];
        }

        for (var index = 0; index < currentActions.Count; index++)
        {
            combined[priorActions.Count + index] = currentActions[index];
        }

        return combined;
    }

    private static RuntimeStatus BuildStatus(
        AutomationSnapshot snapshot,
        AudioSessionSnapshot audioSnapshot,
        AppleMusicSnapshot appleMusicSnapshot,
        IReadOnlyList<DesiredAction> actions)
    {
        var toggleGlyph = snapshot.IsEnabled ? "II" : ">";
        var toggleLabel = snapshot.IsEnabled ? "Automation on" : "Automation off";

        if (!snapshot.IsEnabled)
        {
            return new RuntimeStatus(
                snapshot.IsEnabled,
                toggleGlyph,
                toggleLabel,
                "Automation is paused.",
                "Press play when you want Apple Music to automatically get out of the way for YouTube or any other audio.");
        }

        if (snapshot.IsExternalAudioActive)
        {
            return new RuntimeStatus(
                snapshot.IsEnabled,
                toggleGlyph,
                toggleLabel,
                "External audio is active.",
                $"Apple Music stays paused while {FormatSources(audioSnapshot.ActiveSources)} is playing.");
        }

        if (!snapshot.CanAutoResume)
        {
            return new RuntimeStatus(
                snapshot.IsEnabled,
                toggleGlyph,
                toggleLabel,
                "Manual pause detected.",
                "Apple Music will wait for one fresh external-audio cycle before auto-resume is allowed again.");
        }

        if (!appleMusicSnapshot.IsAvailable)
        {
            return new RuntimeStatus(
                snapshot.IsEnabled,
                toggleGlyph,
                toggleLabel,
                "Apple Music is not open yet.",
                actions.Contains(DesiredAction.LaunchAppleMusic)
                    ? "The app is launching Apple Music and will resume playback from your last session."
                    : "The app is ready to launch Apple Music when silence returns.");
        }

        if (actions.Contains(DesiredAction.PlayAppleMusic))
        {
            return new RuntimeStatus(
                snapshot.IsEnabled,
                toggleGlyph,
                toggleLabel,
                "Resuming Apple Music.",
                "Silence is back, so the app is asking Apple Music to continue your last playback.");
        }

        if (appleMusicSnapshot.PlaybackState == PlaybackState.Playing)
        {
            return new RuntimeStatus(
                snapshot.IsEnabled,
                toggleGlyph,
                toggleLabel,
                "Apple Music is playing.",
                "Automation is active and ready to pause when any other audio starts.");
        }

        return new RuntimeStatus(
            snapshot.IsEnabled,
            toggleGlyph,
            toggleLabel,
            "Waiting for Apple Music.",
            "No external audio is active, so Apple Music is allowed to play whenever its session becomes available.");
    }

    private static string FormatSources(IReadOnlyList<string> activeSources)
    {
        if (activeSources.Count == 0)
        {
            return "other apps";
        }

        if (activeSources.Count == 1)
        {
            return activeSources[0];
        }

        return string.Join(", ", activeSources);
    }
}
