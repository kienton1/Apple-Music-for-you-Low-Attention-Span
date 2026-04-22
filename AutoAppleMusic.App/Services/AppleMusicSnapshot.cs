using AutoAppleMusic.Core;

namespace AutoAppleMusic.App.Services;

public sealed record AppleMusicSnapshot(
    bool IsAvailable,
    PlaybackState PlaybackState,
    string? SourceAppId);
