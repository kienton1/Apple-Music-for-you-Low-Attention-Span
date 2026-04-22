namespace AutoAppleMusic.App.Models;

public sealed record AutomationStatus(
    bool IsEnabled,
    bool HasNonAppleAudio,
    bool IsManualResumeBlocked,
    bool IsAppleMusicAvailable,
    bool IsAppleMusicPlaying,
    string StatusLine,
    string Detail);
