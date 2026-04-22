namespace AutoAppleMusic.App.Models;

public sealed record AudioAppSession(
    int ProcessId,
    string ProcessName,
    string DisplayName,
    float PeakLevel);
