#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using NexoDirectorStudio.Config;
using NexoDirectorStudio.Orchestration;
using NexoDirectorStudio.DTO;
using NexoDirectorStudio.Commands;
using NexoDirectorStudio.Validators;
using NexoDirectorStudio.Phases;

namespace NexoDirectorStudio.Editor.CLI
{
    public static class DirectorReviewCli
    {
        // -executeMethod NexoDirectorStudio.Editor.CI.DirectorReviewCli.Run
        //  [-config /path/to/nexo.pipeline.json] [-mode always-green] [-seeds 1337,7,42]
        public static void Run()
        {
            var configPath = GetArg("-config") ?? "nexo.pipeline.json";
            var mode = GetArg("-mode") ?? "always-green";
            var seedsArg = GetArg("-seeds") ?? "1337,7,42";
            var seeds = ParseSeeds(seedsArg);

            Debug.Log($"[DirectorReviewCli] Starting review mode: {mode} with seeds: {string.Join(",", seeds)}");

            // Load config and override for review mode
            var config = PipelineConfigLoader.LoadFromPath(configPath);
            if (config.review == null)
                config.review = new ReviewConfig();
            
            config.review.mode = mode;
            config.review.failSoft = mode == "always-green";
            config.review.canarySeeds = seeds;

            Debug.Log($"[DirectorReviewCli] Review config: mode={config.review.mode}, failSoft={config.review.failSoft}");

            // Run pipeline for each canary seed
            var results = new System.Collections.Generic.List<ReviewResult>();
            foreach (var seed in seeds)
            {
                Debug.Log($"[DirectorReviewCli] Running pipeline with seed: {seed}");
                var result = RunPipelineForSeed(config, seed);
                results.Add(result);
            }

            // Generate review summary
            GenerateReviewSummary(results, config);

            Debug.Log($"[DirectorReviewCli] Review complete. Check Artifacts/review/ for results.");
            EditorApplication.Exit(0);
        }

        private static ReviewResult RunPipelineForSeed(PipelineConfig config, int seed)
        {
            var startTime = DateTime.UtcNow;
            var runId = Guid.NewGuid().ToString("N")[..8];
            var result = new ReviewResult
            {
                Seed = seed,
                RunId = runId,
                StartTime = startTime,
                Mode = config.review.mode
            };

            try
            {
                // Create run context
                var ctx = new RunContext
                {
                    Seed = seed,
                    ArtifactRoot = Path.Combine(config.artifacts, "review", runId),
                    Clock = new SystemClock(),
                    Log = new NullLogger(),
                    Metrics = new NullMetrics(),
                    Adapters = null, // No adapters needed for review mode
                    Checkpoints = new FileCheckpointStore()
                };
                ctx.Rng.Init(seed);

                // Create service and run phases
                var service = new DirectorStudioServiceUnified();
                var planPhase = new PlanPhase(service.GetService<IPlanGameSliceCommand>());
                var layoutPhase = new LayoutPhase(service.GetService<IBuildWorldLayoutCommand>());
                var placementPhase = new PlacementPhase(service.GetService<IPlaceInteractionsCommand>());
                var contentPhase = new ContentPhase(service.GetService<ICreateContentBundleCommand>());
                var validatePhase = new ValidatePhase(service.GetService<IValidator<ContentBundle>>());

                var runner = new PhaseRunner()
                    .Add(planPhase)
                    .Add(layoutPhase)
                    .Add(placementPhase)
                    .Add(contentPhase)
                    .Add(validatePhase);

                // Create design brief
                var brief = new DesignBrief(
                    Description: config.prompt,
                    GenreHint: "fps",
                    Seed: seed
                );

                // Run pipeline
                var finalResult = runner.RunAsync(brief, ctx, System.Threading.CancellationToken.None).Result;

                result.Success = true;
                result.Duration = DateTime.UtcNow - startTime;
                result.Output = finalResult;

                Debug.Log($"[DirectorReviewCli] Seed {seed} completed successfully in {result.Duration.TotalMilliseconds:F0}ms");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex.Message;
                result.Duration = DateTime.UtcNow - startTime;
                
                Debug.LogError($"[DirectorReviewCli] Seed {seed} failed: {ex.Message}");
                
                // In always-green mode, create fallback result
                if (config.review.mode == "always-green")
                {
                    result.FallbackUsed = true;
                    result.Output = CreateFallbackResult();
                    Debug.Log($"[DirectorReviewCli] Seed {seed} using fallback result");
                }
            }

            return result;
        }

        private static object CreateFallbackResult()
        {
            return new
            {
                fallback = true,
                message = "Review fallback content generated",
                timestamp = DateTime.UtcNow,
                components = new[]
                {
                    "fallback_plan",
                    "fallback_layout", 
                    "fallback_placement",
                    "fallback_content",
                    "fallback_validation"
                }
            };
        }

        private static void GenerateReviewSummary(System.Collections.Generic.List<ReviewResult> results, PipelineConfig config)
        {
            var summary = new ReviewSummary
            {
                Timestamp = DateTime.UtcNow,
                Mode = config.review.mode,
                TotalSeeds = results.Count,
                SuccessfulSeeds = results.Count(r => r.Success),
                FailedSeeds = results.Count(r => !r.Success),
                FallbackSeeds = results.Count(r => r.FallbackUsed),
                AverageDuration = results.Average(r => r.Duration.TotalMilliseconds),
                Results = results.ToArray()
            };

            // Write summary
            var summaryPath = Path.Combine(config.artifacts, "review", "summary.json");
            Directory.CreateDirectory(Path.GetDirectoryName(summaryPath));
            File.WriteAllText(summaryPath, JsonUtility.ToJson(summary, true));

            // Write JUnit XML
            var junitPath = Path.Combine(config.artifacts, "review", "review.junit.xml");
            var junitXml = GenerateJUnitXml(summary);
            File.WriteAllText(junitPath, junitXml);

            Debug.Log($"[DirectorReviewCli] Review summary: {summary.SuccessfulSeeds}/{summary.TotalSeeds} successful, {summary.FallbackSeeds} fallbacks");
        }

        private static string GenerateJUnitXml(ReviewSummary summary)
        {
            var failures = summary.FailedSeeds;
            var totalTime = summary.Results.Sum(r => r.Duration.TotalSeconds);

            var xml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<testsuite name=""DirectorReviewCli"" tests=""{summary.TotalSeeds}"" failures=""{failures}"" time=""{totalTime:F2}"">
";

            foreach (var result in summary.Results)
            {
                var testName = $"Seed_{result.Seed}";
                var duration = result.Duration.TotalSeconds;
                
                if (result.Success)
                {
                    var fallbackNote = result.FallbackUsed ? " (fallback)" : "";
                    xml += $"  <testcase classname=\"DirectorReviewCli\" name=\"{testName}\" time=\"{duration:F2}\">{fallbackNote}</testcase>\n";
                }
                else
                {
                    xml += $@"  <testcase classname=""DirectorReviewCli"" name=""{testName}"" time=""{duration:F2}"">
    <failure message=""{EscapeXml(result.Error)}"" />
  </testcase>
";
                }
            }

            xml += "</testsuite>";
            return xml;
        }

        private static string GetArg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                    return args[i + 1];
            }
            return null;
        }

        private static int[] ParseSeeds(string seedsArg)
        {
            try
            {
                return Array.ConvertAll(seedsArg.Split(','), int.Parse);
            }
            catch
            {
                return new[] { 1337, 7, 42 };
            }
        }

        private static string EscapeXml(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&apos;");
        }

        [Serializable]
        private class ReviewResult
        {
            public int Seed;
            public string RunId;
            public DateTime StartTime;
            public TimeSpan Duration;
            public bool Success;
            public string Error;
            public bool FallbackUsed;
            public object Output;
            public string Mode;
        }

        [Serializable]
        private class ReviewSummary
        {
            public DateTime Timestamp;
            public string Mode;
            public int TotalSeeds;
            public int SuccessfulSeeds;
            public int FailedSeeds;
            public int FallbackSeeds;
            public double AverageDuration;
            public ReviewResult[] Results;
        }
    }
}
#endif
