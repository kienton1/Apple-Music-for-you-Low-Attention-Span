namespace AutoAppleMusic.App.Models;

public sealed record AppleMusicAppState(
    bool IsInstalled,
    bool IsRunning,
    bool HasControllableSession,
    bool IsPlaying,
    string SessionSourceId,
    string Detail)
{
    public static AppleMusicAppState Unavailable(string detail) =>
        new(false, false, false, false, string.Empty, detail);
}
