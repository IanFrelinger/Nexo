using Nexo.CLI.Runtime;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;

namespace Nexo.Tests.CLI.Tests.Commands;

/// <summary>Tests for runtime studio agent set reader.</summary>
public sealed class RuntimeStudioAgentSetReaderTests : UnitTestBase
{
    public override Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var temp = Path.Combine(Path.GetTempPath(), "nexo-rs-read-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temp);
            try
            {
                var path = Path.Combine(temp, "agents.json");
                File.WriteAllText(
                    path,
                    """
                    {
                      "BackgroundAgents": {
                        "Agents": [
                          { "Id": "z", "ModelProvider": "ollama", "ModelName": "m-z" },
                          { "Id": "a", "ModelProvider": "ollama", "ModelName": "m-a" },
                          { "Id": "skip", "ModelProvider": "deterministic" }
                        ]
                      }
                    }
                    """);

                var rows = RuntimeStudioAgentSetReader.TryListOllamaAgents(path);
                /// <summary>Assert equal.</summary>
                AssertEqual(2, rows.Count);
                /// <summary>Assert equal.</summary>
                AssertEqual("a", rows[0].Id);
                /// <summary>Assert equal.</summary>
                AssertEqual("m-a", rows[0].ModelName);
                /// <summary>Assert equal.</summary>
                AssertEqual("z", rows[1].Id);
            }
            finally
            {
                try
                {
                    Directory.Delete(temp, recursive: true);
                }
                catch
                {
                }
            }

            return Task.FromResult(new TestResult
            {
                Name = nameof(RuntimeStudioAgentSetReaderTests),
                Category = "CLI",
                Passed = true,
                Message = "Runtime Studio agent-set reader tests passed"
            });
        }
        catch (AssertionException ex)
        {
            return Task.FromResult(new TestResult
            {
                Name = nameof(RuntimeStudioAgentSetReaderTests),
                Category = "CLI",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new TestResult
            {
                Name = nameof(RuntimeStudioAgentSetReaderTests),
                Category = "CLI",
                Passed = false,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace
            });
        }
    }
}
