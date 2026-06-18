namespace Nexo.Spike.S1.Transforms;

internal static class SeedVariants
{
    public static string Pick(IReadOnlyList<string> options, int seed) =>
        options[PositiveMod(seed, options.Count)];

    public static int PickInt(IReadOnlyList<int> options, int seed) =>
        options[PositiveMod(seed, options.Count)];

    private static int PositiveMod(int value, int modulus) =>
        ((value % modulus) + modulus) % modulus;

    public static readonly IReadOnlyList<string> LeadingZeroLiterals =
        ["007", "0012", "00042", "0800", "0001", "0123", "00099", "0042"];

    public static readonly IReadOnlyList<string> ThousandsLiterals =
        ["1,000", "2,500", "10,000", "12,345", "9,999", "1,234", "50,000", "3,333"];

    public static readonly IReadOnlyList<string> LocaleCommaLiterals =
        ["1,5", "2,75", "0,5", "3,14", "9,99", "1,25", "4,50", "6,75"];

    public static readonly IReadOnlyList<string> SignedZeroLiterals =
        ["+0", "-0", "+0", "-0", "+0", "-0", "+0", "-0"];

    public static readonly IReadOnlyList<string> ScientificLiterals =
        ["1e3", "2e2", "1.5e1", "3E4", "9e-1", "1e2", "5e3", "2.5e2"];

    public static readonly IReadOnlyList<string> YesNoLiterals =
        ["yes", "no", "YES", "NO", "Yes", "No", "yEs", "nO"];

    public static readonly IReadOnlyList<string> YnLiterals =
        ["Y", "N", "y", "n", "Y", "N", "y", "n"];

    public static readonly IReadOnlyList<string> WhitespaceCells =
        ["   ", "\t", "  \t  ", " \t", "\t\t", "   ", "\t ", "  "];

    public static readonly IReadOnlyList<string> ZeroOnePairs =
        ["0", "1", "0", "1", "0", "1", "0", "1"];

    public static readonly IReadOnlyList<int> OffByOneThresholds = [4, 5, 6, 3, 7, 4, 5, 6];

    public static readonly IReadOnlyList<int> BoundaryInclusiveLimits = [1, 2, 1, 2, 1, 2, 1, 2];

    public static readonly IReadOnlyList<int> SamplingWindowSizes = [2, 3, 2, 4, 2, 3, 2, 5];
}
