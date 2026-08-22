namespace Ashlar.Hosting;

internal static partial class AshlarKernelRegistrar
{
    /// <summary>
    /// Mirrors ASHLAR_EPHEMERAL / ASHLAR_EPHEMERAL_MODELS handling used by ephemeral lifecycle and trust wiring.
    /// </summary>
    private static bool EphemeralModelsEnabled()
    {
        var ephemeralAll = string.Equals(Environment.GetEnvironmentVariable("ASHLAR_EPHEMERAL"), "1", StringComparison.OrdinalIgnoreCase);
        return ephemeralAll || string.Equals(Environment.GetEnvironmentVariable("ASHLAR_EPHEMERAL_MODELS"), "1", StringComparison.OrdinalIgnoreCase);
    }
}
