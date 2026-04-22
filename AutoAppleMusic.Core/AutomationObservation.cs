namespace AutoAppleMusic.Core;

/// <summary>
/// The current observable state from the host app's audio polling loop.
/// </summary>
public readonly record struct AutomationObservation(
    bool IsExternalAudioActive,
    bool IsAppleMusicAvailable,
    PlaybackState AppleMusicPlaybackState);
