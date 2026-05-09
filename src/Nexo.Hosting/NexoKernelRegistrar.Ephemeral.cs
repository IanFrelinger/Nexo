namespace Nexo.Hosting;

internal static partial class NexoKernelRegistrar
{
    /// <summary>
    /// Mirrors NEXO_EPHEMERAL / NEXO_EPHEMERAL_MODELS handling used by ephemeral lifecycle and trust wiring.
    /// </summary>
    private static bool EphemeralModelsEnabled()
    {
        var ephemeralAll = string.Equals(Environment.GetEnvironmentVariable("NEXO_EPHEMERAL"), "1", StringComparison.OrdinalIgnoreCase);
        return ephemeralAll || string.Equals(Environment.GetEnvironmentVariable("NEXO_EPHEMERAL_MODELS"), "1", StringComparison.OrdinalIgnoreCase);
    }
}
