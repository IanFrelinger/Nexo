using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NexoDirectorStudio.DTO;
using NexoDirectorStudio.Validation;
using NexoDirectorStudio.Commands;
using NexoDirectorStudio.Orchestration;

namespace NexoDirectorStudio.Tests.EditMode
{
    /// <summary>
    /// Utilities for CI/CD test execution.
    /// </summary>
    public static class CICDTestUtilities
    {
        /// <summary>
        /// Runs a minimal pipeline for CI/CD testing (no validation to keep it fast).
        /// </summary>
        public static async Task<CICDPipelineResult> RunMinimalPipelineAsync(DesignBrief brief)
        {
            try
            {
                using var service = new DirectorStudioService();
                
                var plan = await service.GetService<IPlanGameSliceCommand>()
                    .ExecuteAsync(new IPlanGameSliceCommand.Input(brief), CancellationToken.None);

                var world = await service.GetService<IBuildWorldLayoutCommand>()
                    .ExecuteAsync(new IBuildWorldLayoutCommand.Input(plan), CancellationToken.None);

                var interactions = await service.GetService<IPlaceInteractionsCommand>()
                    .ExecuteAsync(new IPlaceInteractionsCommand.Input(world, plan), CancellationToken.None);

                var content = await service.GetService<ICreateContentBundleCommand>()
                    .ExecuteAsync(new ICreateContentBundleCommand.Input(interactions, plan), CancellationToken.None);

                return new CICDPipelineResult
                {
                    Success = true,
                    GamePlan = plan,
                    WorldLayout = world,
                    InteractionGraph = interactions,
                    ContentBundle = content
                };
            }
            catch (Exception ex)
            {
                return new CICDPipelineResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Creates a test brief for CI/CD testing.
        /// </summary>
        public static DesignBrief CreateTestBrief(string description, string genre, int seed = 123)
        {
            return new DesignBrief(
                Description: description,
                GenreHint: genre,
                TargetDurationMinutes: 1,
                DifficultyLevel: 1,
                Seed: seed
            );
        }

        /// <summary>
        /// Validates service resolution for CI/CD testing.
        /// </summary>
        public static bool ValidateServiceResolution(DirectorStudioService service)
        {
            try
            {
                var services = new object[]
                {
                    service.GetService<IPlanGameSliceCommand>(),
                    service.GetService<IBuildWorldLayoutCommand>(),
                    service.GetService<IPlaceInteractionsCommand>(),
                    service.GetService<ICreateContentBundleCommand>(),
                    service.GetService<IOllamaAdapter>(),
                    service.GetService<ITextureGenAdapter>(),
                    service.GetService<ITtsAdapter>()
                };

                return services.All(s => s != null);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Validates data consistency for CI/CD testing.
        /// </summary>
        public static bool ValidateDataConsistency(CICDPipelineResult result)
        {
            if (!result.Success)
                return false;

            // Check basic data consistency
            return result.GamePlan != null &&
                   result.WorldLayout != null &&
                   result.InteractionGraph != null &&
                   result.ContentBundle != null &&
                   result.GamePlan.Seed == result.WorldLayout.Seed &&
                   result.WorldLayout.Seed == result.InteractionGraph.Seed &&
                   result.InteractionGraph.Seed == result.ContentBundle.Seed;
        }
    }

    /// <summary>
    /// Result of a CI/CD pipeline execution.
    /// </summary>
    public class CICDPipelineResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = "";
        public GamePlan GamePlan { get; set; }
        public WorldLayout WorldLayout { get; set; }
        public InteractionGraph InteractionGraph { get; set; }
        public ContentBundle ContentBundle { get; set; }
    }
}
