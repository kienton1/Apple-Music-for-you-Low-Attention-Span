namespace AutoAppleMusic.Core;

public sealed record AutomationDecision(
    AutomationSnapshot Snapshot,
    IReadOnlyList<AutomationEventKind> Events,
    IReadOnlyList<DesiredAction> Actions)
{
    public static AutomationDecision None(AutomationSnapshot snapshot) =>
        new(snapshot, Array.Empty<AutomationEventKind>(), Array.Empty<DesiredAction>());
}
