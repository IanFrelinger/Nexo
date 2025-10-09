using UnityEngine;
using UnityEngine.Events;
using System.Threading;
using System.Threading.Tasks;
using NexoDirectorStudio.Orchestration;
using NexoDirectorStudio.Commands;
using NexoDirectorStudio.DTO;
using NexoDirectorStudio.Adapters;
using NexoDirectorStudio.Validation;

namespace NexoDirectorStudio.Agents
{
    /// <summary>
    /// Agent-first Director: turns a natural-language prompt into a generated, launched slice.
    /// </summary>
    public class AgentDirector : MonoBehaviour
    {
        [TextArea(4, 12)] public string prompt = "Doom-style FPS. 15 min. Intense combat, key gates. seed=666.";
        public bool autoLaunch = true;
        public string genreHint = "FPS";
        public int targetMinutes = 15;
        public int difficulty = 4;
        public int seed = 666;

        [Header("Events")] public UnityEvent onPlanned;
        public UnityEvent onWorldBuilt;
        public UnityEvent onLaunched;
        public UnityEvent onValidated;

        [Header("Validation Settings")]
        public bool enableComponentValidation = true;
        public float validationTimeoutSeconds = 30f;
        public int maxValidationRetries = 3;
        public float minValidationScore = 80f;

        private DirectorStudioService _svc;
        private TestRunnerIntegration _testRunner;

        async void Start()
        {
            if (autoLaunch)
            {
                await RunAsync(CancellationToken.None);
            }
        }

        public async Task RunAsync(CancellationToken ct)
        {
            _svc = new DirectorStudioServiceUnified();
            _testRunner = new TestRunnerIntegration(maxValidationRetries, validationTimeoutSeconds);

            // Optional: consult adapters for enrichment (LLM, etc.)
            var ollama = _svc.GetService<IOllamaAdapter>();
            // In a real setup, call ollama to expand prompt → constraints (kept stub/deterministic)

            var brief = new DesignBrief(
                Description: prompt,
                GenreHint: genreHint,
                TargetDurationMinutes: targetMinutes,
                DifficultyLevel: difficulty,
                Constraints: null,
                Seed: seed
            );

            var plan = await _svc.GetService<IPlanGameSliceCommand>()
                .ExecuteAsync(new IPlanGameSliceCommand.Input(brief), ct);
            onPlanned?.Invoke();

            var world = await _svc.GetService<IBuildWorldLayoutCommand>()
                .ExecuteAsync(new IBuildWorldLayoutCommand.Input(plan), ct);
            onWorldBuilt?.Invoke();

            var interactions = await _svc.GetService<IPlaceInteractionsCommand>()
                .ExecuteAsync(new IPlaceInteractionsCommand.Input(world, plan), ct);

            var content = await _svc.GetService<ICreateContentBundleCommand>()
                .ExecuteAsync(new ICreateContentBundleCommand.Input(interactions, plan), ct);

            // Validate generated components using Unity test runner
            if (enableComponentValidation)
            {
                Debug.Log("🧪 Starting component validation...");
                
                var validationResult = await _testRunner.RunValidationTestsWithRetryAsync(
                    content, 
                    plan, 
                    async (failedResult) =>
                    {
                        // Regenerate content based on validation failures
                        Debug.Log($"🔄 Regenerating content due to validation failures (Score: {failedResult.OverallScore:F1}%)");
                        
                        // In a real implementation, this would use the validation results
                        // to guide the regeneration process with specific improvements
                        return await _svc.GetService<ICreateContentBundleCommand>()
                            .ExecuteAsync(new ICreateContentBundleCommand.Input(interactions, plan), ct);
                    },
                    ct);

                if (validationResult.OverallPassed && validationResult.OverallScore >= minValidationScore)
                {
                    Debug.Log($"✅ Component validation PASSED - Score: {validationResult.OverallScore:F1}%");
                    onValidated?.Invoke();
                }
                else
                {
                    Debug.LogWarning($"⚠️ Component validation completed with issues - Score: {validationResult.OverallScore:F1}%");
                    
                    // Log recommendations
                    var recommendations = _testRunner.GetValidationRecommendations(validationResult);
                    foreach (var recommendation in recommendations)
                    {
                        Debug.LogWarning($"💡 Recommendation: {recommendation}");
                    }
                }

                // Export validation report
                var reportPath = $"Assets/Generated/ValidationReports/validation_{content.Id}_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}.txt";
                await _testRunner.ExportValidationReportAsync(validationResult, reportPath, ct);
            }

            // Game components will be generated by Nexo agents as part of the pipeline
            // The content bundle now contains all the generated Unity assets
            Debug.Log($"✅ Pipeline complete - Generated content bundle with {content.Assets.Count} assets");
            Debug.Log($"📋 Game Plan: {plan.Description}");
            Debug.Log($"🏗️ World Layout: {world.Dimensions.x}x{world.Dimensions.y}x{world.Dimensions.z}");
            Debug.Log($"⚔️ Interactions: {interactions.Nodes.Count} nodes, {interactions.Connections.Count} connections");
            Debug.Log($"🎮 Content Bundle: {content.Assets.Count} assets ready for Unity");

            onLaunched?.Invoke();
        }

        void OnDestroy()
        {
            _svc?.Dispose();
        }
    }
}
