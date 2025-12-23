using System.CommandLine;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Orchestration.Agents.Playtest;

namespace Nexo.CLI.Commands;

/// <summary>
/// Command handler for demo operations (iterative game creation, feedback synthesis, etc.)
/// </summary>
public class DemoCommand
{
    private readonly ILogger<DemoCommand> _logger;

    public DemoCommand(ILogger<DemoCommand> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Creates the demo command group with all subcommands.
    /// </summary>
    public static Command CreateCommand(IServiceProvider serviceProvider, Option<bool> jsonOpt, Option<bool> verboseOpt)
    {
        var demoCmd = new Command("demo", "Demo operations for iterative game development");

        var logger = serviceProvider.GetRequiredService<ILogger<DemoCommand>>();
        var command = new DemoCommand(logger);

        // nexo demo synthesize-feedback
        var synthesizeCmd = new Command("synthesize-feedback", "Synthesize feedback from playtest results");
        synthesizeCmd.AddArgument(new Argument<FileInfo>("playtest-results", "Path to playtest results JSON"));
        synthesizeCmd.AddArgument(new Argument<FileInfo>("output", "Path to output feedback synthesis JSON"));
        synthesizeCmd.SetHandler(async (FileInfo playtestResults, FileInfo output, bool json, bool verbose) =>
        {
            var exitCode = await command.SynthesizeFeedbackAsync(playtestResults, output, json, verbose);
            Environment.Exit(exitCode);
        }, synthesizeCmd.Arguments[0] as Argument<FileInfo> ?? throw new InvalidOperationException(),
           synthesizeCmd.Arguments[1] as Argument<FileInfo> ?? throw new InvalidOperationException(),
           jsonOpt,
           verboseOpt);

        // nexo demo apply-feedback
        var applyCmd = new Command("apply-feedback", "Apply feedback changes to game specification");
        applyCmd.AddArgument(new Argument<FileInfo>("feedback", "Path to feedback synthesis JSON"));
        applyCmd.AddArgument(new Argument<FileInfo>("spec", "Path to game specification JSON"));
        applyCmd.AddOption(new Option<FileInfo?>("--output", "Output path (defaults to overwriting spec file)"));
        var applyOutputOpt = applyCmd.Options[0] as Option<FileInfo?> ?? throw new InvalidOperationException();
        applyCmd.SetHandler(async (FileInfo feedback, FileInfo spec, FileInfo? output, bool json, bool verbose) =>
        {
            var exitCode = await command.ApplyFeedbackAsync(feedback, spec, output ?? spec, json, verbose);
            Environment.Exit(exitCode);
        }, applyCmd.Arguments[0] as Argument<FileInfo> ?? throw new InvalidOperationException(),
           applyCmd.Arguments[1] as Argument<FileInfo> ?? throw new InvalidOperationException(),
           applyOutputOpt,
           jsonOpt,
           verboseOpt);

        // nexo demo create-assets
        var assetsCmd = new Command("create-assets", "Create Unity demo assets for an iteration");
        assetsCmd.AddArgument(new Argument<DirectoryInfo>("project", "Path to Unity project"));
        assetsCmd.AddArgument(new Argument<int>("iteration", "Iteration number"));
        assetsCmd.SetHandler(async (DirectoryInfo project, int iteration, bool json, bool verbose) =>
        {
            var exitCode = await command.CreateUnityAssetsAsync(project, iteration, json, verbose);
            Environment.Exit(exitCode);
        }, assetsCmd.Arguments[0] as Argument<DirectoryInfo> ?? throw new InvalidOperationException(),
           assetsCmd.Arguments[1] as Argument<int> ?? throw new InvalidOperationException(),
           jsonOpt,
           verboseOpt);

        demoCmd.AddCommand(synthesizeCmd);
        demoCmd.AddCommand(applyCmd);
        demoCmd.AddCommand(assetsCmd);

        return demoCmd;
    }

    public async Task<int> SynthesizeFeedbackAsync(FileInfo playtestResults, FileInfo output, bool json, bool verbose)
    {
        try
        {
            if (!playtestResults.Exists)
            {
                _logger.LogError("Playtest results file not found: {Path}", playtestResults.FullName);
                if (json)
                {
                    Console.WriteLine(JsonSerializer.Serialize(new { error = "Playtest results file not found", path = playtestResults.FullName }));
                }
                return 1;
            }

            _logger.LogInformation("Synthesizing feedback from playtest results: {Path}", playtestResults.FullName);

            var playtestJson = await File.ReadAllTextAsync(playtestResults.FullName);
            var playtestData = JsonNode.Parse(playtestJson);

            if (playtestData == null)
            {
                _logger.LogError("Invalid JSON in playtest results file");
                if (json)
                {
                    Console.WriteLine(JsonSerializer.Serialize(new { error = "Invalid JSON in playtest results" }));
                }
                return 1;
            }

            // Extract issues from playtest results
            var issues = new List<JsonObject>();
            if (playtestData["issues"] is JsonArray issuesArray)
            {
                foreach (var issue in issuesArray.OfType<JsonObject>())
                {
                    // Handle both string and int severity values
                    int severity = 0;
                    var severityNode = issue["severity"];
                    if (severityNode != null)
                    {
                        if (severityNode.GetValueKind() == JsonValueKind.String)
                        {
                            var severityStr = severityNode.GetValue<string>()?.ToLowerInvariant() ?? "";
                            severity = severityStr switch
                            {
                                "low" or "minor" => 1,
                                "medium" or "moderate" => 2,
                                "high" or "major" => 3,
                                "critical" => 4,
                                _ => 0
                            };
                        }
                        else if (severityNode.GetValueKind() == JsonValueKind.Number)
                        {
                            severity = severityNode.GetValue<int>();
                        }
                    }
                    
                    if (severity >= 2) // Only include medium+ severity issues
                    {
                        issues.Add(issue);
                    }
                }
            }

            // Create change requests from issues
            var changeRequests = new List<JsonObject>();
            foreach (var issue in issues)
            {
                var changeRequest = new JsonObject
                {
                    ["requestId"] = issue["issueId"]?.GetValue<string>() ?? Guid.NewGuid().ToString(),
                    ["sourceIssueId"] = issue["issueId"]?.GetValue<string>() ?? "unknown",
                    ["domain"] = (issue["affectedDomains"] as JsonArray)?.FirstOrDefault()?.GetValue<string>() ?? "Gameplay",
                    ["changeType"] = "Modify",
                    ["targetElement"] = "Placeholder Issue",
                    ["currentValue"] = "N/A",
                    ["proposedValue"] = issue["title"]?.GetValue<string>() ?? "Improve based on feedback",
                    ["rationale"] = issue["description"]?.GetValue<string>() ?? "Based on playtest feedback"
                };
                changeRequests.Add(changeRequest);
            }

            var synthesis = new JsonObject
            {
                ["synthesisId"] = $"synth-{DateTime.UtcNow:yyyy-MM-dd}",
                ["sourceReportId"] = playtestData["reportId"]?.GetValue<string>() ?? "unknown",
                ["generatedAt"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                ["changeRequests"] = new JsonArray(changeRequests.ToArray()),
                ["iterationRecommendation"] = new JsonObject
                {
                    ["shouldIterate"] = changeRequests.Count > 0,
                    ["priority"] = changeRequests.Count > 2 ? "High" : "Normal",
                    ["estimatedEffort"] = "PT1H",
                    ["summary"] = $"Synthesized {changeRequests.Count} changes from playtest results"
                }
            };

            var outputJson = synthesis.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(output.FullName, outputJson);

            if (json)
            {
                Console.WriteLine(outputJson);
            }
            else
            {
                _logger.LogInformation("Feedback synthesis created: {Count} change requests", changeRequests.Count);
                _logger.LogInformation("Output saved to: {Path}", output.FullName);
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error synthesizing feedback");
            if (json)
            {
                Console.WriteLine(JsonSerializer.Serialize(new { error = ex.Message }));
            }
            return 1;
        }
    }

    public async Task<int> ApplyFeedbackAsync(FileInfo feedback, FileInfo spec, FileInfo output, bool json, bool verbose)
    {
        try
        {
            if (!feedback.Exists)
            {
                _logger.LogError("Feedback file not found: {Path}", feedback.FullName);
                if (json)
                {
                    Console.WriteLine(JsonSerializer.Serialize(new { error = "Feedback file not found", path = feedback.FullName }));
                }
                return 1;
            }

            if (!spec.Exists)
            {
                _logger.LogError("Specification file not found: {Path}", spec.FullName);
                if (json)
                {
                    Console.WriteLine(JsonSerializer.Serialize(new { error = "Specification file not found", path = spec.FullName }));
                }
                return 1;
            }

            _logger.LogInformation("Applying feedback changes to specification: {Path}", spec.FullName);

            var feedbackJson = await File.ReadAllTextAsync(feedback.FullName);
            var feedbackData = JsonNode.Parse(feedbackJson);

            if (feedbackData == null)
            {
                _logger.LogError("Invalid JSON in feedback file");
                if (json)
                {
                    Console.WriteLine(JsonSerializer.Serialize(new { error = "Invalid JSON in feedback file" }));
                }
                return 1;
            }

            var specJson = await File.ReadAllTextAsync(spec.FullName);
            var specNode = JsonNode.Parse(specJson)?.AsObject();

            if (specNode == null)
            {
                _logger.LogError("Invalid JSON in specification file");
                if (json)
                {
                    Console.WriteLine(JsonSerializer.Serialize(new { error = "Invalid JSON in specification file" }));
                }
                return 1;
            }

            var changeRequests = feedbackData["changeRequests"] as JsonArray;
            if (changeRequests == null || changeRequests.Count == 0)
            {
                _logger.LogInformation("No change requests found in feedback");
                await File.WriteAllTextAsync(output.FullName, specJson);
                if (json)
                {
                    Console.WriteLine(JsonSerializer.Serialize(new { success = true, changesApplied = 0 }));
                }
                return 0;
            }

            int applied = 0;
            int skipped = 0;

            foreach (var changeNode in changeRequests.OfType<JsonObject>())
            {
                var domain = changeNode["domain"]?.GetValue<string>() ?? "";
                var changeType = changeNode["changeType"]?.GetValue<string>() ?? "Modify";
                var targetElement = changeNode["targetElement"]?.GetValue<string>() ?? "";
                var proposedValue = changeNode["proposedValue"]?.GetValue<string>() ?? "";

                if (string.IsNullOrEmpty(domain) || string.IsNullOrEmpty(targetElement))
                {
                    skipped++;
                    continue;
                }

                // Get or create domain object
                if (!specNode.TryGetPropertyValue(domain, out var domainNode) || domainNode is not JsonObject domainObj)
                {
                    if (changeType == "Add")
                    {
                        domainObj = new JsonObject();
                        specNode[domain] = domainObj;
                    }
                    else
                    {
                        skipped++;
                        continue;
                    }
                }

                // Apply change
                switch (changeType)
                {
                    case "Modify":
                    case "Add":
                        domainObj[targetElement] = JsonValue.Create(proposedValue);
                        applied++;
                        if (!json)
                        {
                            _logger.LogInformation("✅ Applied: {ChangeType} {Domain}.{Target} = {Value}", changeType, domain, targetElement, proposedValue);
                        }
                        break;
                    case "Remove":
                        if (domainObj.Remove(targetElement))
                        {
                            applied++;
                            if (!json)
                            {
                                _logger.LogInformation("✅ Removed: {Domain}.{Target}", domain, targetElement);
                            }
                        }
                        else
                        {
                            skipped++;
                        }
                        break;
                }
            }

            var updatedJson = specNode.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(output.FullName, updatedJson);

            if (json)
            {
                Console.WriteLine(JsonSerializer.Serialize(new { success = true, changesApplied = applied, changesSkipped = skipped }));
            }
            else
            {
                _logger.LogInformation("📊 Summary: {Applied} changes applied, {Skipped} skipped", applied, skipped);
                _logger.LogInformation("💾 Updated specification saved to: {Path}", output.FullName);
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying feedback changes");
            if (json)
            {
                Console.WriteLine(JsonSerializer.Serialize(new { error = ex.Message }));
            }
            return 1;
        }
    }

    public Task<int> CreateUnityAssetsAsync(DirectoryInfo project, int iteration, bool json, bool verbose)
    {
        try
        {
            if (!project.Exists)
            {
                _logger.LogError("Unity project not found: {Path}", project.FullName);
                if (json)
                {
                    Console.WriteLine(JsonSerializer.Serialize(new { error = "Project not found", path = project.FullName }));
                }
                return Task.FromResult(1);
            }

            var assetsDir = Path.Combine(project.FullName, "Assets");
            var iterationDir = Path.Combine(assetsDir, $"Iteration{iteration}");
            var scriptsDir = Path.Combine(assetsDir, "Scripts");

            Directory.CreateDirectory(iterationDir);
            Directory.CreateDirectory(scriptsDir);

            var sceneName = $"Iteration{iteration}Scene";
            var scenePath = Path.Combine(iterationDir, $"{sceneName}.unity");
            var scriptName = $"Iteration{iteration}Demo";
            var scriptPath = Path.Combine(scriptsDir, $"{scriptName}.cs");
            var readmePath = Path.Combine(iterationDir, "README.txt");

            // Create minimal Unity scene file
            var sceneContent = @"%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!29 &1
SceneSettings:
  m_ObjectHideFlags: 0
  m_PVSData: 
  m_PVSObjectsArray: []
  m_PVSCellCount: 0
";

            // Create simple C# script
            var scriptContent = $@"using UnityEngine;

public class {scriptName} : MonoBehaviour
{{
    void Start()
    {{
        Debug.Log(""Iteration {iteration} Demo - Created by Nexo"");
    }}
}}
";

            // Create README
            var readmeContent = $@"Iteration {iteration} Assets
Created: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC

This iteration was created by the Nexo iterative game development demo.
";

            File.WriteAllText(scenePath, sceneContent);
            File.WriteAllText(scriptPath, scriptContent);
            File.WriteAllText(readmePath, readmeContent);

            if (json)
            {
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    success = true,
                    iteration = iteration,
                    assets = new
                    {
                        scene = scenePath,
                        script = scriptPath,
                        readme = readmePath
                    }
                }));
            }
            else
            {
                _logger.LogInformation("✅ Created Unity assets for iteration {Iteration}:", iteration);
                _logger.LogInformation("   Scene: {Scene}", scenePath);
                _logger.LogInformation("   Script: {Script}", scriptPath);
                _logger.LogInformation("   README: {Readme}", readmePath);
            }

            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Unity assets");
            if (json)
            {
                Console.WriteLine(JsonSerializer.Serialize(new { error = ex.Message }));
            }
            return Task.FromResult(1);
        }
    }
}

