namespace AutoAppleMusic.App.Models;

public sealed record RuntimeStatus(
    bool IsAutomationEnabled,
    string ToggleGlyph,
    string ToggleLabel,
    string StatusLine,
    string StatusDetail);
