namespace AutoAppleMusic.App.Services;

public sealed record AudioSessionSnapshot(
    bool IsExternalAudioActive,
    IReadOnlyList<string> ActiveSources);
