using System.Diagnostics;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Nexo.Verify.Adapters;
using Nexo.Verify.Asserts;

namespace Nexo.Verify
{
    /// <summary>
    /// Main verification runner that loads specs and executes scenarios
    /// </summary>
    public static class VerifyRunner
    {
        /// <summary>
        /// Run verification for a spec file or directory
        /// </summary>
        /// <param name="path">Path to .verify.yaml file or directory containing .verify.yaml files</param>
        /// <param name="console">Console for output</param>
        /// <returns>Exit code (0 for success, 1 for failure)</returns>
        public static async Task<int> RunAsync(string path, IConsole? console = null)
        {
            console ??= new ConsoleWrapper();
            
            try
            {
                var specs = LoadSpecs(path);
                var allPassed = true;

                foreach (var specPath in specs)
                {
                    console.WriteLine($"Loading spec: {specPath}");
                    var spec = LoadSpec(specPath);
                    
                    foreach (var mode in spec.Modes)
                    {
                        console.WriteLine($"\nExecuting mode: {mode.Name}");
                        
                        using var scope = new EnvironmentScope(mode.Env);
                        var stopwatch = Stopwatch.StartNew();
                        
                        var result = await ScenarioApi.RunAsync(spec.Scenario, spec.Inputs, mode.Env);
                        stopwatch.Stop();
                        
                        var elapsedMs = (int)stopwatch.Elapsed.TotalMilliseconds;
                        console.WriteLine($"Scenario completed in {elapsedMs}ms (Exit code: {result.ExitCode})");
                        
                        if (!result.Completed)
                        {
                            console.WriteLine($"✗ Scenario failed: {result.ErrorMessage}");
                            allPassed = false;
                            continue;
                        }

                        // Execute assertions
                        var modePassed = true;
                        foreach (var assert in mode.Asserts)
                        {
                            console.WriteLine($"  Verifying: {assert.Type}");
                            var passed = VerifyAssertExecutor.Execute(assert, result, elapsedMs, console);
                            if (!passed)
                            {
                                modePassed = false;
                                allPassed = false;
                            }
                        }

                        if (modePassed)
                        {
                            console.WriteLine($"✓ Mode '{mode.Name}' passed all assertions");
                        }
                        else
                        {
                            console.WriteLine($"✗ Mode '{mode.Name}' failed one or more assertions");
                        }
                    }
                }

                if (allPassed)
                {
                    console.WriteLine("\n✓ All verification tests passed");
                    return 0;
                }
                else
                {
                    console.WriteLine("\n✗ One or more verification tests failed");
                    return 1;
                }
            }
            catch (Exception ex)
            {
                console.WriteLine($"ERROR: Verification failed: {ex.Message}");
                return 1;
            }
        }

        /// <summary>
        /// Load all .verify.yaml files from a path
        /// </summary>
        private static IEnumerable<string> LoadSpecs(string path)
        {
            if (Directory.Exists(path))
            {
                return Directory.GetFiles(path, "*.verify.yaml", SearchOption.AllDirectories);
            }
            else if (File.Exists(path))
            {
                return new[] { path };
            }
            else
            {
                throw new FileNotFoundException($"Spec file or directory not found: {path}");
            }
        }

        /// <summary>
        /// Load a single verification spec from YAML
        /// </summary>
        private static VerifySpec LoadSpec(string filePath)
        {
            var yaml = File.ReadAllText(filePath);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            
            return deserializer.Deserialize<VerifySpec>(yaml);
        }
    }

    /// <summary>
    /// Console interface for output
    /// </summary>
    public partial interface IConsole
    {
        void WriteLine(string message);
    }

    /// <summary>
    /// Default console implementation
    /// </summary>
    public sealed class ConsoleWrapper : IConsole
    {
        public void WriteLine(string message) => Console.WriteLine(message);
    }

    /// <summary>
    /// Helper class to temporarily set environment variables
    /// </summary>
    internal sealed class EnvironmentScope : IDisposable
    {
        private readonly Dictionary<string, string?> _originalValues = new();
        private readonly IDictionary<string, string>? _newValues;

        public EnvironmentScope(IDictionary<string, string>? newValues)
        {
            _newValues = newValues;
            
            if (newValues != null)
            {
                foreach (var kvp in newValues)
                {
                    _originalValues[kvp.Key] = Environment.GetEnvironmentVariable(kvp.Key);
                    Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
                }
            }
        }

        public void Dispose()
        {
            if (_newValues != null)
            {
                foreach (var kvp in _newValues)
                {
                    if (_originalValues.TryGetValue(kvp.Key, out var originalValue))
                    {
                        Environment.SetEnvironmentVariable(kvp.Key, originalValue);
                    }
                    else
                    {
                        Environment.SetEnvironmentVariable(kvp.Key, null);
                    }
                }
            }
        }
    }
}
