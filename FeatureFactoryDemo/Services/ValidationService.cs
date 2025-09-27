using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Analysis.Interfaces;
using Nexo.Feature.Analysis.Models;

namespace FeatureFactoryDemo.Services
{
    public partial class ValidationService
    {
        private readonly ILogger<ValidationService> _logger;
        private readonly ICodingStandardAnalyzer _codeAnalyzer;
        private readonly IIterationStrategy _iterationStrategy;

        public ValidationService(
            ILogger<ValidationService> logger,
            ICodingStandardAnalyzer codeAnalyzer,
            IIterationStrategy iterationStrategy)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _codeAnalyzer = codeAnalyzer ?? throw new ArgumentNullException(nameof(codeAnalyzer));
            _iterationStrategy = iterationStrategy ?? throw new ArgumentNullException(nameof(iterationStrategy));
        }

        public async Task DemoCodingStandardsValidation()
        {
            Console.WriteLine("\n--- Demo 1: Coding Standards Validation ---");
            
            var code = @"public partial class UserService
{
    private readonly ILogger<UserService> _logger;
    
    public UserService(ILogger<UserService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task<User> GetUserAsync(int id)
    {
        _logger.LogInformation(""Getting user with ID: {Id}"", id);
        
        // Implementation here
        return new User { Id = id, Name = ""John Doe"" };
    }
}";
            
            Console.WriteLine("Validating code sample...");
            var result = await _codeAnalyzer.AnalyzeCodeAsync(code);
            
            Console.WriteLine($"Quality Score: {result.QualityScore}/100");
            Console.WriteLine($"Issues Found: {result.Issues.Count}");
            
            foreach (var issue in result.Issues.Take(3))
            {
                Console.WriteLine($"  - {issue.Type}: {issue.Description}");
            }
        }

        public async Task DemoQualityScoreCalculation()
        {
            Console.WriteLine("\n--- Demo 2: Quality Score Calculation ---");
            
            var codeSamples = new[]
            {
                ("Poor Quality Code", @"public partial class BadCode
{
    public string GetData()
    {
        return ""data"";
    }
}"),
                ("Medium Quality Code", @"public partial class MediumCode
{
    private readonly ILogger<MediumCode> _logger;
    
    public MediumCode(ILogger<MediumCode> logger)
    {
        _logger = logger;
    }
    
    public async Task<string> GetDataAsync()
    {
        _logger.LogInformation(""Getting data"");
        return ""data"";
    }
}"),
                ("High Quality Code", @"public partial class GoodCode
{
    private readonly ILogger<GoodCode> _logger;
    private readonly IDataRepository _repository;
    
    public GoodCode(ILogger<GoodCode> logger, IDataRepository repository)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }
    
    public async Task<Result<string>> GetDataAsync(int id)
    {
        try
        {
            _logger.LogInformation(""Getting data for ID: {Id}"", id);
            
            if (id <= 0)
            {
                return Result<string>.Failure(""Invalid ID"");
            }
            
            var data = await _repository.GetByIdAsync(id);
            return Result<string>.Success(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ""Error getting data for ID: {Id}"", id);
            return Result<string>.Failure(""An error occurred"");
        }
    }
}")
            };

            foreach (var (description, code) in codeSamples)
            {
                Console.WriteLine($"\nAnalyzing: {description}");
                var result = await _codeAnalyzer.AnalyzeCodeAsync(code);
                Console.WriteLine($"Quality Score: {result.QualityScore}/100");
                Console.WriteLine($"Issues: {result.Issues.Count}");
            }
        }

        public async Task DemoIterationImprovement()
        {
            Console.WriteLine("\n--- Demo 3: Iteration Improvement ---");
            
            var initialCode = @"public partial class Service
{
    public string GetData()
    {
        return ""data"";
    }
}";
            
            Console.WriteLine("Initial Code Quality:");
            var initialResult = await _codeAnalyzer.AnalyzeCodeAsync(initialCode);
            Console.WriteLine($"Score: {initialResult.QualityScore}/100");
            
            var improvedCode = await _iterationStrategy.ImproveCodeAsync(initialCode, initialResult);
            Console.WriteLine("\nAfter Iteration Improvement:");
            var improvedResult = await _codeAnalyzer.AnalyzeCodeAsync(improvedCode);
            Console.WriteLine($"Score: {improvedResult.QualityScore}/100");
            
            Console.WriteLine($"Improvement: +{improvedResult.QualityScore - initialResult.QualityScore} points");
        }

        public async Task<CodingStandardValidationResult> ValidateCodeAsync(string code)
        {
            try
            {
                _logger.LogInformation("Validating code quality");

                return await _codeAnalyzer.AnalyzeCodeAsync(code);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating code");
                return new CodingStandardValidationResult
                {
                    QualityScore = 0,
                    Issues = new List<CodingStandardIssue>
                    {
                        new CodingStandardIssue
                        {
                            Type = "Error",
                            Description = $"Validation failed: {ex.Message}"
                        }
                    }
                };
            }
        }

        public async Task<List<CodingStandardValidationResult>> ValidateCodeIterativelyAsync(string code, int maxIterations = 5)
        {
            var results = new List<CodingStandardValidationResult>();
            var currentCode = code;

            for (int iteration = 1; iteration <= maxIterations; iteration++)
            {
                _logger.LogInformation("Validation iteration {Iteration}", iteration);

                var result = await _codeAnalyzer.AnalyzeCodeAsync(currentCode);
                results.Add(result);

                if (result.QualityScore >= 90)
                {
                    _logger.LogInformation("Target quality reached after {Iterations} iterations", iteration);
                    break;
                }

                currentCode = await _iterationStrategy.ImproveCodeAsync(currentCode, result);
            }

            return results;
        }
    }
}
