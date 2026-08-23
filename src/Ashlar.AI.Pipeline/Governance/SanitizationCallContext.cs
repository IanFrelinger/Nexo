namespace Ashlar.AI.Pipeline.Governance;

/// <summary>
/// Flows the latest sanitization summary across the governance stack for the current async call.
/// </summary>
internal static class SanitizationCallContext
{
    private static readonly AsyncLocal<MessageSanitizationResult?> Current = new();

    public static MessageSanitizationResult? Result
    {
        get => Current.Value;
        set => Current.Value = value;
    }
}
