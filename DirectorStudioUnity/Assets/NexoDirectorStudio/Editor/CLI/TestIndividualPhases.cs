#if UNITY_EDITOR
using System;
using System.Threading;
using System.Threading.Tasks;
using NexoDirectorStudio.Orchestration;
using NexoDirectorStudio.Phases;
using NexoDirectorStudio.DTO;
using NexoDirectorStudio.Commands;
using NexoDirectorStudio.Validators;
using UnityEditor;
using UnityEngine;

namespace NexoDirectorStudio.EditorCLI
{
    public static class TestIndividualPhases
    {
        // -executeMethod NexoDirectorStudio.EditorCLI.TestIndividualPhases.Run
        public static async void Run()
        {
            try
            {
                Debug.Log("[TestIndividualPhases] Starting individual phase tests...");
                
                var svc = new DirectorStudioServiceUnified();
                var ctx = new RunContext { Seed = 1337, ArtifactRoot = "Artifacts", Checkpoints = null };
                ctx.Rng.Init(1337);
                
                var brief = new DesignBrief(Description: "test micro room", GenreHint: "fps", Seed: 1337);
                Debug.Log($"[TestIndividualPhases] Created DesignBrief: {brief.Description}");
                
                object planResult = null;
                object layoutResult = null;
                object placementResult = null;
                object contentResult = null;
                
                // Test PlanPhase
                try
                {
                    Debug.Log("[TestIndividualPhases] Testing PlanPhase...");
                    var planPhase = new PlanPhase(svc.GetService<IPlanGameSliceCommand>());
                    planResult = await planPhase.RunAsync(brief, ctx, CancellationToken.None);
                    Debug.Log($"[TestIndividualPhases] PlanPhase completed, result type: {planResult?.GetType().Name}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[TestIndividualPhases] PlanPhase failed: {ex}");
                    throw;
                }
                
                // Test LayoutPhase
                try
                {
                    Debug.Log("[TestIndividualPhases] Testing LayoutPhase...");
                    var layoutPhase = new LayoutPhase(svc.GetService<IBuildWorldLayoutCommand>());
                    layoutResult = await layoutPhase.RunAsync((NexoDirectorStudio.DTO.GamePlan)planResult, ctx, CancellationToken.None);
                    Debug.Log($"[TestIndividualPhases] LayoutPhase completed, result type: {layoutResult?.GetType().Name}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[TestIndividualPhases] LayoutPhase failed: {ex}");
                    throw;
                }
                
                // Test PlacementPhase
                try
                {
                    Debug.Log("[TestIndividualPhases] Testing PlacementPhase...");
                    var placementPhase = new PlacementPhase(svc.GetService<IPlaceInteractionsCommand>());
                    placementResult = await placementPhase.RunAsync((NexoDirectorStudio.DTO.WorldLayout)layoutResult, ctx, CancellationToken.None);
                    Debug.Log($"[TestIndividualPhases] PlacementPhase completed, result type: {placementResult?.GetType().Name}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[TestIndividualPhases] PlacementPhase failed: {ex}");
                    throw;
                }
                
                // Test ContentPhase
                try
                {
                    Debug.Log("[TestIndividualPhases] Testing ContentPhase...");
                    var contentPhase = new ContentPhase(svc.GetService<ICreateContentBundleCommand>());
                    contentResult = await contentPhase.RunAsync((NexoDirectorStudio.DTO.InteractionGraph)placementResult, ctx, CancellationToken.None);
                    Debug.Log($"[TestIndividualPhases] ContentPhase completed, result type: {contentResult?.GetType().Name}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[TestIndividualPhases] ContentPhase failed: {ex}");
                    throw;
                }
                
                // Test ValidatePhase
                try
                {
                    Debug.Log("[TestIndividualPhases] Testing ValidatePhase...");
                    var validatePhase = new ValidatePhase(svc.GetService<IValidator<ContentBundle>>());
                    var validateResult = await validatePhase.RunAsync((NexoDirectorStudio.DTO.ContentBundle)contentResult, ctx, CancellationToken.None);
                    Debug.Log($"[TestIndividualPhases] ValidatePhase completed, result type: {validateResult?.GetType().Name}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[TestIndividualPhases] ValidatePhase failed: {ex}");
                    throw;
                }
                
                Debug.Log("[TestIndividualPhases] All individual phase tests completed successfully!");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TestIndividualPhases] Test failed: {ex}");
                EditorApplication.Exit(2);
            }
        }
    }
}
#endif
