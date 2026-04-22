namespace AutoAppleMusic.App.Models;

public sealed record AudioActivitySnapshot(
    bool HasNonAppleAudio,
    IReadOnlyList<AudioAppSession> ActiveSources,
    DateTimeOffset ObservedAt)
{
    public static AudioActivitySnapshot Empty { get; } =
        new(false, Array.Empty<AudioAppSession>(), DateTimeOffset.MinValue);
}
