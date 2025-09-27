using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nexo.Feature.Analysis.Interfaces;
using Nexo.Feature.Analysis.Models;

namespace FeatureFactoryDemo.Commands
{
    /// <summary>
    /// Iterative improvement functionality for E2E generation command.
    /// </summary>
    public partial class GenerateWithE2ECommand
    {
        private async Task<FeatureGenerationResult> RunIterativeImprovementAsync(
            ICodingStandardAnalyzer codeAnalyzer,
            string description,
            string platform,
            int targetScore,
            int maxIterations)
        {
            Console.WriteLine($"\nProcessing Starting Iterative Code Improvement (Target: {targetScore}/100, Max Iterations: {maxIterations})");
            Console.WriteLine(new string('=', 80));

            var result = new FeatureGenerationResult
            {
                GeneratedCode = GenerateInitialCode(description, platform),
                IterationHistory = new List<CodingStandardValidationResult>()
            };

            int bestScore = 0;
            string bestCode = result.GeneratedCode;

            for (int iteration = 1; iteration <= maxIterations; iteration++)
            {
                var analysisResult = await codeAnalyzer.ValidateCodeAsync(result.GeneratedCode, "feature-generation");
                result.IterationHistory.Add(analysisResult);

                Console.WriteLine($"\nStats Iteration {iteration}/{maxIterations}:");
                Console.WriteLine($"   Quality Score: {analysisResult.Score}/100");
                Console.WriteLine($"   Violations: {analysisResult.Violations.Count}");

                if (analysisResult.Score > bestScore)
                {
                    bestScore = analysisResult.Score;
                    bestCode = result.GeneratedCode;
                }

                if (analysisResult.Score >= targetScore)
                {
                    Console.WriteLine($"   SUCCESS TARGET ACHIEVED! Quality score: {analysisResult.Score}/100");
                    Console.WriteLine($"   SUCCESS: Code meets all quality standards!");
                    break;
                }

                if (iteration < maxIterations)
                {
                    var improvement = GetImprovementDescription(iteration, analysisResult.Score);
                    Console.WriteLine($"   Improvement: {improvement}");
                    Console.WriteLine($"   Tool Applying AI-powered improvements...");
                    
                    result.GeneratedCode = ImproveCodeBasedOnViolations(result.GeneratedCode, analysisResult.Violations);
                    Console.WriteLine($"   Starting Code improved based on analysis");
                }
            }

            result.QualityScore = bestScore;
            result.GeneratedCode = bestCode;
            result.IsSuccess = bestScore >= targetScore;
            result.IterationCount = result.IterationHistory.Count;

            return result;
        }
    }
}
