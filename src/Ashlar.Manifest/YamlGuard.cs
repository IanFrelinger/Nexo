using System.Text.RegularExpressions;

namespace Ashlar.Manifest;

/// <summary>
/// Pre-parse guards both loaders apply before YAML ever reaches the deserializer.
///
/// <para>Two rules, both fail-closed. SIZE: a manifest or policy over 1 MB is rejected —
/// no honest configuration document is that large, and the cap bounds parser work on
/// garbage. ALIASES: YAML anchors/aliases are rejected outright, because YamlDotNet
/// expands them and a small document can be crafted to expand exponentially (the classic
/// billion-laughs shape); ashlar documents never need them. The scan is textual and
/// deliberately a little over-eager — a scalar that genuinely needs a token shaped like
/// <c>&amp;name</c> or <c>*name</c> in anchor position does not belong in these files, and
/// the rejection says exactly what to change.</para>
/// </summary>
public static partial class YamlGuard
{
    /// <summary>1 MB. Configuration, not cargo.</summary>
    public const int MaxBytes = 1024 * 1024;

    [GeneratedRegex(@"(^|[\s\[,{])[&*][A-Za-z0-9_]", RegexOptions.Multiline)]
    private static partial Regex AnchorOrAlias();

    /// <summary>
    /// Returns false with a reason when the raw document violates a guard.
    /// </summary>
    public static bool Check(string yaml, string documentName, out string reason)
    {
        if (yaml.Length > MaxBytes)
        {
            reason = $"REJECTED: {documentName} is {yaml.Length:N0} characters; the limit is {MaxBytes:N0}. "
                   + "These are configuration documents — if something this large seems necessary, it belongs elsewhere.";
            return false;
        }

        var match = AnchorOrAlias().Match(yaml);
        if (match.Success)
        {
            reason = $"REJECTED: {documentName} contains a YAML anchor or alias ('{match.Value.Trim()}…'). "
                   + "Anchors and aliases are not permitted — they enable exponential-expansion attacks and "
                   + "ashlar documents never need them. Write the value out literally.";
            return false;
        }

        reason = string.Empty;
        return true;
    }
}
