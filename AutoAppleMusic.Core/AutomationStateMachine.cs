namespace AutoAppleMusic.Core;

/// <summary>
/// Deterministic automation reducer for driving Apple Music from a polling loop.
/// </summary>
public sealed class AutomationStateMachine
{
    public AutomationStateMachine()
        : this(AutomationSnapshot.Initial)
    {
    }

    public AutomationStateMachine(AutomationSnapshot snapshot)
    {
        Snapshot = snapshot;
    }

    public AutomationSnapshot Snapshot { get; private set; }

    public AutomationDecision SetEnabled(bool isEnabled)
    {
        if (Snapshot.IsEnabled == isEnabled)
        {
            return AutomationDecision.None(Snapshot);
        }

        var events = new List<AutomationEventKind>
        {
            isEnabled ? AutomationEventKind.AutomationEnabled : AutomationEventKind.AutomationDisabled,
        };

        var next = Snapshot with
        {
            IsEnabled = isEnabled,
            PendingAction = null,
        };

        var actions = new List<DesiredAction>();
        if (isEnabled)
        {
            ApplyDesiredAction(ref next, actions);
        }

        Snapshot = next;
        return new AutomationDecision(next, events.ToArray(), actions.ToArray());
    }

    public AutomationDecision Observe(
        bool isExternalAudioActive,
        bool isAppleMusicAvailable,
        PlaybackState appleMusicPlaybackState) =>
        Observe(new AutomationObservation(isExternalAudioActive, isAppleMusicAvailable, appleMusicPlaybackState));

    /// <summary>
    /// Applies a fresh observation from the host app and returns the next desired actions, if any.
    /// </summary>
    public AutomationDecision Observe(AutomationObservation observation)
    {
        var current = Snapshot;
        var events = new List<AutomationEventKind>();
        var actions = new List<DesiredAction>();

        if (current.HasObservation)
        {
            AddObservationEvents(current, observation, events);
        }

        var canAutoResume = current.CanAutoResume;
        var pendingAction = ResolvePendingAction(current.PendingAction, observation);

        if (IsManualPause(current, observation))
        {
            canAutoResume = false;
            events.Add(AutomationEventKind.AutoResumeBlockedByManualPause);
        }

        if (current.HasObservation &&
            current.IsExternalAudioActive &&
            !observation.IsExternalAudioActive &&
            !canAutoResume)
        {
            canAutoResume = true;
            events.Add(AutomationEventKind.AutoResumeReenabled);
        }

        var next = current with
        {
            HasObservation = true,
            IsExternalAudioActive = observation.IsExternalAudioActive,
            IsAppleMusicAvailable = observation.IsAppleMusicAvailable,
            AppleMusicPlaybackState = observation.AppleMusicPlaybackState,
            CanAutoResume = canAutoResume,
            PendingAction = pendingAction,
        };

        ApplyDesiredAction(ref next, actions);

        Snapshot = next;
        return new AutomationDecision(next, events.ToArray(), actions.ToArray());
    }

    private static void AddObservationEvents(
        AutomationSnapshot current,
        AutomationObservation observation,
        ICollection<AutomationEventKind> events)
    {
        if (!current.IsExternalAudioActive && observation.IsExternalAudioActive)
        {
            events.Add(AutomationEventKind.ExternalAudioStarted);
        }
        else if (current.IsExternalAudioActive && !observation.IsExternalAudioActive)
        {
            events.Add(AutomationEventKind.ExternalAudioStopped);
        }

        if (!current.IsAppleMusicAvailable && observation.IsAppleMusicAvailable)
        {
            events.Add(AutomationEventKind.AppleMusicBecameAvailable);
        }
        else if (current.IsAppleMusicAvailable && !observation.IsAppleMusicAvailable)
        {
            events.Add(AutomationEventKind.AppleMusicBecameUnavailable);
        }

        if (current.AppleMusicPlaybackState != PlaybackState.Playing &&
            observation.AppleMusicPlaybackState == PlaybackState.Playing)
        {
            events.Add(AutomationEventKind.AppleMusicStartedPlaying);
        }
        else if (current.AppleMusicPlaybackState == PlaybackState.Playing &&
                 observation.AppleMusicPlaybackState == PlaybackState.Paused)
        {
            events.Add(AutomationEventKind.AppleMusicPaused);
        }
    }

    private static bool IsManualPause(AutomationSnapshot current, AutomationObservation observation) =>
        current.HasObservation &&
        current.IsEnabled &&
        current.AppleMusicPlaybackState == PlaybackState.Playing &&
        observation.AppleMusicPlaybackState == PlaybackState.Paused &&
        !current.IsExternalAudioActive &&
        !observation.IsExternalAudioActive &&
        current.PendingAction != DesiredAction.PauseAppleMusic;

    private static DesiredAction? ResolvePendingAction(
        DesiredAction? pendingAction,
        AutomationObservation observation) =>
        pendingAction switch
        {
            DesiredAction.PauseAppleMusic when observation.AppleMusicPlaybackState != PlaybackState.Playing => null,
            DesiredAction.PlayAppleMusic when observation.IsAppleMusicAvailable &&
                                             observation.AppleMusicPlaybackState == PlaybackState.Playing => null,
            DesiredAction.LaunchAppleMusic when observation.IsAppleMusicAvailable => null,
            _ => pendingAction,
        };

    private static void ApplyDesiredAction(ref AutomationSnapshot snapshot, ICollection<DesiredAction> actions)
    {
        var desiredAction = DetermineDesiredAction(snapshot);
        if (desiredAction is null)
        {
            snapshot = snapshot with { PendingAction = null };
            return;
        }

        if (snapshot.PendingAction == desiredAction)
        {
            return;
        }

        actions.Add(desiredAction.Value);
        snapshot = snapshot with { PendingAction = desiredAction };
    }

    private static DesiredAction? DetermineDesiredAction(AutomationSnapshot snapshot)
    {
        if (!snapshot.IsEnabled || !snapshot.HasObservation)
        {
            return null;
        }

        if (snapshot.IsExternalAudioActive)
        {
            return snapshot.AppleMusicPlaybackState == PlaybackState.Playing
                ? DesiredAction.PauseAppleMusic
                : null;
        }

        if (!snapshot.CanAutoResume)
        {
            return null;
        }

        if (!snapshot.IsAppleMusicAvailable)
        {
            return DesiredAction.LaunchAppleMusic;
        }

        return snapshot.AppleMusicPlaybackState == PlaybackState.Playing
            ? null
            : DesiredAction.PlayAppleMusic;
    }
}
