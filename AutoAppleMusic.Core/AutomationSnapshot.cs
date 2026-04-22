namespace AutoAppleMusic.Core;

/// <summary>
/// Full state of the automation reducer after the latest event or observation.
/// </summary>
public readonly record struct AutomationSnapshot(
    bool IsEnabled,
    bool HasObservation,
    bool IsExternalAudioActive,
    bool IsAppleMusicAvailable,
    PlaybackState AppleMusicPlaybackState,
    bool CanAutoResume,
    DesiredAction? PendingAction)
{
    public static AutomationSnapshot Initial { get; } = new(
        IsEnabled: false,
        HasObservation: false,
        IsExternalAudioActive: false,
        IsAppleMusicAvailable: false,
        AppleMusicPlaybackState: PlaybackState.Unknown,
        CanAutoResume: true,
        PendingAction: null);
}
