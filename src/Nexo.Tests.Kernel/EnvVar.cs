namespace Nexo.Tests.Kernel;

/// <summary>Env var.</summary>
internal static class EnvVar
{
    public static void Run(string name, string? value, Action body)
    {
        var prev = Environment.GetEnvironmentVariable(name);
        try
        {
            Environment.SetEnvironmentVariable(name, value);
            /// <summary>Body.</summary>
            body();
        }
        finally
        {
            Environment.SetEnvironmentVariable(name, prev);
        }
    }
}
