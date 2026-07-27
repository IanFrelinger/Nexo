namespace Nexo.Bricks.DepExtract.Profile;

/// <summary>
/// Per-call override for compile-gate hardness (AdaptInstallLoop / requireCompile).
/// Lives in the C++ profile plugin — not in fixed-engine ports.
/// </summary>
public static class CompileGateRequestContext
{
    private static readonly AsyncLocal<bool?> RequireSuccessOverride = new();

    /// <summary>When set, overrides <see cref="GxxCompileValidatorOptions.RequireSuccess"/> for the ambient async flow.</summary>
    public static bool? RequireSuccess
    {
        get => RequireSuccessOverride.Value;
        set => RequireSuccessOverride.Value = value;
    }

    /// <summary>Resolves effective require-success (override → options → COMPILE_GATE env).</summary>
    public static bool Resolve(bool optionsRequireSuccess)
    {
        if (RequireSuccess is { } ambient)
            return ambient;
        if (optionsRequireSuccess)
            return true;
        return string.Equals(
            Environment.GetEnvironmentVariable("COMPILE_GATE"),
            "1",
            StringComparison.Ordinal);
    }
}
