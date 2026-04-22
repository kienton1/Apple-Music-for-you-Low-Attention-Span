namespace AutoAppleMusic.App.Models;

public sealed record AppleMusicCommandResult(
    bool Succeeded,
    AppleMusicAppState State,
    string Message);
