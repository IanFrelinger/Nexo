using System.Text.RegularExpressions;

namespace Ashlar.API.Middleware.Ingress;

/// <summary>Parses <c>YES &lt;token&gt;</c> approval lines shared by SMS simulation and AWS SNS ingress.</summary>
public static class SmsYesTokenParser
{
    private static readonly Regex YesTokenPattern = new(@"^\s*YES\s+(?<token>\S+)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    /// <summary>Parses a <c>YES &lt;token&gt;</c> approval line from an SMS body.</summary>
    public static bool TryParse(string body, out string token)
    {
        token = string.Empty;
        var match = YesTokenPattern.Match(body ?? string.Empty);
        if (!match.Success)
            return false;
        token = match.Groups["token"].Value.Trim();
        return !string.IsNullOrEmpty(token);
    }
}
