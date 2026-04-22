namespace AutoAppleMusic.Core;

public enum AutomationEventKind
{
    AutomationEnabled = 0,
    AutomationDisabled = 1,
    ExternalAudioStarted = 2,
    ExternalAudioStopped = 3,
    AppleMusicBecameAvailable = 4,
    AppleMusicBecameUnavailable = 5,
    AppleMusicStartedPlaying = 6,
    AppleMusicPaused = 7,
    AutoResumeBlockedByManualPause = 8,
    AutoResumeReenabled = 9,
}
