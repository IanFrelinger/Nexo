namespace Nexo.Tests.Kernel;

internal static class EnvVar
{
    public static void Run(string name, string? value, Action body)
    {
        var prev = Environment.GetEnvironmentVariable(name);
        try
        {
            Environment.SetEnvironmentVariable(name, value);
            body();
        }
        finally
        {
            Environment.SetEnvironmentVariable(name, prev);
        }
    }
}

[Xunit.CollectionDefinition("EnvironmentSensitive", DisableParallelization = true)]
public sealed class EnvironmentSensitiveCollection { }
