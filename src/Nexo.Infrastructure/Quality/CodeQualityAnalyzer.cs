using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Models.CodeQuality;
using Nexo.Infrastructure.Quality.Analyzers;

namespace Nexo.Infrastructure.Quality
{
    /// <summary>
    /// Orchestrator for code quality analysis that delegates to specialized analyzers.
    /// </summary>
    public partial class CodeQualityAnalyzer : ICodeQualityAnalyzer
    {
        private readonly ILogger<CodeQualityAnalyzer> _logger;
        private readonly SecurityAnalyzer _securityAnalyzer;
        private readonly PerformanceAnalyzer _performanceAnalyzer;
        private readonly MaintainabilityAnalyzer _maintainabilityAnalyzer;
        private readonly TestabilityAnalyzer _testabilityAnalyzer;
        private readonly ReadabilityAnalyzer _readabilityAnalyzer;
        private readonly DocumentationAnalyzer _documentationAnalyzer;
        private readonly AIAnalysisOrchestrator _aiAnalysisOrchestrator;

        public CodeQualityAnalyzer(ILogger<CodeQualityAnalyzer> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _securityAnalyzer = new SecurityAnalyzer(logger);
            _performanceAnalyzer = new PerformanceAnalyzer(logger);
            _maintainabilityAnalyzer = new MaintainabilityAnalyzer(logger);
            _testabilityAnalyzer = new TestabilityAnalyzer(logger);
            _readabilityAnalyzer = new ReadabilityAnalyzer(logger);
            _documentationAnalyzer = new DocumentationAnalyzer(logger);
            _aiAnalysisOrchestrator = new AIAnalysisOrchestrator(logger);
        }

        public async Task<QualityScore> AnalyzeCodeAsync(string code, string toolName, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Starting comprehensive code quality analysis for tool: {ToolName}", toolName);

            var score = new QualityScore
            {
                ToolName = toolName,
                AnalyzedAt = DateTime.UtcNow
            };

            // Basic metrics
            score.LinesOfCode = CountLinesOfCode(code);
            score.CyclomaticComplexity = CalculateCyclomaticComplexity(code);
            score.NumberOfMethods = CountMethods(code);
            score.NumberOfClasses = CountClasses(code);

            // Individual component analysis
            var securityTask = _securityAnalyzer.AnalyzeAsync(code, cancellationToken);
            var performanceTask = _performanceAnalyzer.AnalyzeAsync(code, cancellationToken);
            var maintainabilityTask = _maintainabilityAnalyzer.AnalyzeAsync(code, cancellationToken);
            var testabilityTask = _testabilityAnalyzer.AnalyzeAsync(code, cancellationToken);

            await Task.WhenAll(securityTask, performanceTask, maintainabilityTask, testabilityTask);

            score.SecurityScore = securityTask.Result.OverallScore;
            score.PerformanceScore = performanceTask.Result.OverallScore;
            score.MaintainabilityScore = maintainabilityTask.Result.OverallScore;
            score.TestabilityScore = testabilityTask.Result.OverallScore;

            // Additional analysis
            score.ReadabilityScore = await _readabilityAnalyzer.AnalyzeAsync(code, cancellationToken);
            score.DocumentationScore = await _documentationAnalyzer.AnalyzeAsync(code, cancellationToken);

            // Calculate overall score (weighted average)
            score.OverallScore = CalculateOverallScore(score);

            // Determine quality level
            score.QualityLevel = DetermineQualityLevel(score.OverallScore);

            // Quality gates
            score.PassesQualityGate = PassesQualityGate(score);
            score.RequiresHumanReview = RequiresHumanReview(score);
            score.IsProductionReady = IsProductionReady(score);

            // AI-powered analysis for additional insights
            await _aiAnalysisOrchestrator.PerformAIAnalysisAsync(code, score, cancellationToken);

            _logger.LogInformation("Code quality analysis completed for {ToolName}. Overall Score: {Score}, Quality Level: {Level}",
                toolName, score.OverallScore, score.QualityLevel);

            return score;
        }

        private int CountLinesOfCode(string code) => code.Split('\n').Length;
        private int CalculateCyclomaticComplexity(string code) => code.Split(new[] { "if", "while", "for", "switch", "case", "catch", "&&", "||" }, StringSplitOptions.None).Length - 1;
        private int CountMethods(string code) => code.Split(new[] { "public ", "private ", "protected ", "internal " }, StringSplitOptions.None).Count(s => s.Contains("("));
        private int CountClasses(string code) => code.Split(new[] { "class " }, StringSplitOptions.None).Length - 1;

        private double CalculateOverallScore(QualityScore score)
        {
            return (score.SecurityScore * 0.25 + score.PerformanceScore * 0.20 + 
                   score.MaintainabilityScore * 0.20 + score.TestabilityScore * 0.15 + 
                   score.ReadabilityScore * 0.10 + score.DocumentationScore * 0.10);
        }

        private string DetermineQualityLevel(double overallScore)
        {
            return overallScore switch
            {
                >= 90 => "Excellent",
                >= 80 => "Good",
                >= 70 => "Fair",
                >= 60 => "Poor",
                _ => "Critical"
            };
        }

        private bool PassesQualityGate(QualityScore score) => score.OverallScore >= 70;
        private bool RequiresHumanReview(QualityScore score) => score.OverallScore < 80;
        private bool IsProductionReady(QualityScore score) => score.OverallScore >= 85;
    }
}
