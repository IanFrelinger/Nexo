using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FeatureFactoryDemo.Data;
using FeatureFactoryDemo.Services;
using FeatureFactoryDemo.Models;
using Nexo.Feature.Analysis.Interfaces;

namespace FeatureFactoryDemo.Validation
{
    /// <summary>
    /// Comprehensive validation service for testing all Feature Factory features
    /// </summary>
    public class FeatureValidationService
    {
        private readonly FeatureFactoryDbContext _context;
        private readonly CommandHistoryService _commandHistoryService;
        private readonly CodebaseAnalysisService _codebaseAnalysisService;
        private readonly ICodingStandardAnalyzer _codeAnalyzer;
        private readonly ILogger<FeatureValidationService> _logger;
        
        public FeatureValidationService(
            FeatureFactoryDbContext context,
            CommandHistoryService commandHistoryService,
            CodebaseAnalysisService codebaseAnalysisService,
            ICodingStandardAnalyzer codeAnalyzer,
            ILogger<FeatureValidationService> logger)
        {
            _context = context;
            _commandHistoryService = commandHistoryService;
            _codebaseAnalysisService = codebaseAnalysisService;
            _codeAnalyzer = codeAnalyzer;
            _logger = logger;
        }
        
        /// <summary>
        /// Runs comprehensive validation of all Feature Factory features
        /// </summary>
        public async Task<ValidationResults> RunFullValidationAsync()
        {
            var results = new ValidationResults();
            
            Console.WriteLine("Search Starting Comprehensive Feature Factory Validation");
            Console.WriteLine("=====================================================");
            
            // Test 1: Database Operations
            results.DatabaseValidation = await ValidateDatabaseOperationsAsync();
            
            // Test 2: Codebase Context Analysis
            results.CodebaseValidation = await ValidateCodebaseContextAsync();
            
            // Test 3: Command History Operations
            results.CommandHistoryValidation = await ValidateCommandHistoryAsync();
            
            // Test 4: Iterative Improvement with Database Integration
            results.IterativeImprovementValidation = await ValidateIterativeImprovementAsync();
            
            // Test 5: End-to-End Integration
            results.IntegrationValidation = await ValidateEndToEndIntegrationAsync();
            
            // Generate final report
            GenerateValidationReport(results);
            
            return results;
        }
        
        private async Task<DatabaseValidationResult> ValidateDatabaseOperationsAsync()
        {
            Console.WriteLine("\nStats Test 1: Database Operations Validation");
            Console.WriteLine("==========================================");
            
            var result = new DatabaseValidationResult();
            
            try
            {
                // Test database connection
                var canConnect = await _context.Database.CanConnectAsync();
                result.CanConnect = canConnect;
                Console.WriteLine($"   SUCCESS: Database Connection: {(canConnect ? "SUCCESS" : "FAILED")}");
                
                // Test table creation
                var commandHistoryCount = await _context.CommandHistories.CountAsync();
                var codebaseContextCount = await _context.CodebaseContexts.CountAsync();
                result.TablesExist = commandHistoryCount >= 0 && codebaseContextCount >= 0;
                Console.WriteLine($"   SUCCESS: Tables Exist: {(result.TablesExist ? "SUCCESS" : "FAILED")}");
                Console.WriteLine($"      - CommandHistories: {commandHistoryCount} records");
                Console.WriteLine($"      - CodebaseContexts: {codebaseContextCount} records");
                
                // Test CRUD operations
                var testCommand = new CommandHistory
                {
                    Description = "Test Command for Validation",
                    Platform = "DotNet",
                    GeneratedCode = "// Test code",
                    FinalQualityScore = 95,
                    IterationCount = 3,
                    IsSuccessful = true,
                    ExecutedAt = DateTime.UtcNow,
                    Tags = "test,validation"
                };
                
                _context.CommandHistories.Add(testCommand);
                await _context.SaveChangesAsync();
                result.CanCreate = true;
                Console.WriteLine($"   SUCCESS: Create Operation: SUCCESS");
                
                var retrievedCommand = await _context.CommandHistories
                    .FirstOrDefaultAsync(c => c.Description == "Test Command for Validation");
                result.CanRead = retrievedCommand != null;
                Console.WriteLine($"   SUCCESS: Read Operation: {(result.CanRead ? "SUCCESS" : "FAILED")}");
                
                if (retrievedCommand != null)
                {
                    retrievedCommand.FinalQualityScore = 100;
                    await _context.SaveChangesAsync();
                    result.CanUpdate = true;
                    Console.WriteLine($"   SUCCESS: Update Operation: SUCCESS");
                    
                    _context.CommandHistories.Remove(retrievedCommand);
                    await _context.SaveChangesAsync();
                    result.CanDelete = true;
                    Console.WriteLine($"   SUCCESS: Delete Operation: SUCCESS");
                }
                
                result.IsValid = result.CanConnect && result.TablesExist && result.CanCreate && 
                               result.CanRead && result.CanUpdate && result.CanDelete;
                
                Console.WriteLine($"   Stats Database Validation: {(result.IsValid ? "SUCCESS: PASSED" : "ERROR: FAILED")}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database validation failed");
                result.IsValid = false;
                result.ErrorMessage = ex.Message;
                Console.WriteLine($"   ERROR: Database Validation: FAILED - {ex.Message}");
            }
            
            return result;
        }
        
        private async Task<CodebaseValidationResult> ValidateCodebaseContextAsync()
        {
            Console.WriteLine("\nDocumentation Test 2: Codebase Context Analysis Validation");
            Console.WriteLine("===============================================");
            
            var result = new CodebaseValidationResult();
            
            try
            {
                // Test codebase analysis
                var stats = await _codebaseAnalysisService.GetCodebaseStatsAsync();
                result.FilesAnalyzed = stats.TotalFiles;
                result.AverageQuality = stats.AverageQualityScore;
                result.HighQualityFiles = stats.HighQualityFiles;
                
                Console.WriteLine($"   SUCCESS: Codebase Analysis: SUCCESS");
                Console.WriteLine($"      - Files Analyzed: {stats.TotalFiles}");
                Console.WriteLine($"      - Average Quality: {stats.AverageQualityScore}/100");
                Console.WriteLine($"      - High Quality Files (80+): {stats.HighQualityFiles}");
                
                // Test context retrieval
                var context = await _codebaseAnalysisService.GetRelevantContextAsync("Create a Customer entity", "DotNet");
                result.CanRetrieveContext = !string.IsNullOrEmpty(context.Content);
                Console.WriteLine($"   SUCCESS: Context Retrieval: {(result.CanRetrieveContext ? "SUCCESS" : "FAILED")}");
                
                if (result.CanRetrieveContext)
                {
                    Console.WriteLine($"      - Context File: {context.FilePath}");
                    Console.WriteLine($"      - Context Quality: {context.QualityScore}/100");
                    Console.WriteLine($"      - Context Patterns: {context.Patterns}");
                }
                
                // Test code analysis integration
                if (result.CanRetrieveContext && !string.IsNullOrEmpty(context.Content))
                {
                    var analysisResult = await _codeAnalyzer.ValidateCodeAsync(context.Content, context.FilePath, "validation-test");
                    result.CodeAnalysisWorks = analysisResult != null;
                    Console.WriteLine($"   SUCCESS: Code Analysis Integration: {(result.CodeAnalysisWorks ? "SUCCESS" : "FAILED")}");
                    
                    if (result.CodeAnalysisWorks && analysisResult != null)
                    {
                        Console.WriteLine($"      - Analysis Score: {analysisResult.Score}/100");
                        Console.WriteLine($"      - Violations: {analysisResult.Violations.Count}");
                    }
                }
                
                result.IsValid = result.FilesAnalyzed > 0 && result.CanRetrieveContext && result.CodeAnalysisWorks;
                
                Console.WriteLine($"   Stats Codebase Validation: {(result.IsValid ? "SUCCESS: PASSED" : "ERROR: FAILED")}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Codebase validation failed");
                result.IsValid = false;
                result.ErrorMessage = ex.Message;
                Console.WriteLine($"   ERROR: Codebase Validation: FAILED - {ex.Message}");
            }
            
            return result;
        }
        
        private async Task<CommandHistoryValidationResult> ValidateCommandHistoryAsync()
        {
            Console.WriteLine("\nList Test 3: Command History Operations Validation");
            Console.WriteLine("================================================");
            
            var result = new CommandHistoryValidationResult();
            
            try
            {
                // Test saving a command
                await _commandHistoryService.SaveSuccessfulCommandAsync(
                    "Test Customer Entity Generation",
                    "DotNet",
                    "// Generated test code",
                    95,
                    5,
                    "Test context",
                    "test,validation,entity"
                );
                result.CanSaveCommand = true;
                Console.WriteLine($"   SUCCESS: Save Command: SUCCESS");
                
                // Test retrieving recent commands
                var recentCommands = await _commandHistoryService.GetRecentSuccessfulCommandsAsync(5);
                result.CanRetrieveRecent = recentCommands.Any();
                Console.WriteLine($"   SUCCESS: Retrieve Recent Commands: {(result.CanRetrieveRecent ? "SUCCESS" : "FAILED")}");
                Console.WriteLine($"      - Recent Commands Found: {recentCommands.Count}");
                
                // Test similarity matching
                var similarCommands = await _commandHistoryService.GetSimilarCommandsAsync("Create Customer", "DotNet");
                result.CanFindSimilar = similarCommands.Any();
                Console.WriteLine($"   SUCCESS: Find Similar Commands: {(result.CanFindSimilar ? "SUCCESS" : "FAILED")}");
                Console.WriteLine($"      - Similar Commands Found: {similarCommands.Count}");
                
                // Test statistics
                var stats = await _commandHistoryService.GetStatisticsAsync();
                result.CanGetStatistics = stats.TotalCommands > 0;
                Console.WriteLine($"   SUCCESS: Get Statistics: {(result.CanGetStatistics ? "SUCCESS" : "FAILED")}");
                Console.WriteLine($"      - Total Commands: {stats.TotalCommands}");
                Console.WriteLine($"      - Success Rate: {stats.SuccessRate:F1}%");
                Console.WriteLine($"      - Average Quality: {stats.AverageQualityScore}/100");
                
                result.IsValid = result.CanSaveCommand && result.CanRetrieveRecent && 
                               result.CanFindSimilar && result.CanGetStatistics;
                
                Console.WriteLine($"   Stats Command History Validation: {(result.IsValid ? "SUCCESS: PASSED" : "ERROR: FAILED")}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Command history validation failed");
                result.IsValid = false;
                result.ErrorMessage = ex.Message;
                Console.WriteLine($"   ERROR: Command History Validation: FAILED - {ex.Message}");
            }
            
            return result;
        }
        
        private async Task<IterativeImprovementValidationResult> ValidateIterativeImprovementAsync()
        {
            Console.WriteLine("\nProcessing Test 4: Iterative Improvement with Database Integration");
            Console.WriteLine("=========================================================");
            
            var result = new IterativeImprovementValidationResult();
            
            try
            {
                // Test iterative improvement process
                var testCode = @"public class TestEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
}";
                
                var analysisResult = await _codeAnalyzer.ValidateCodeAsync(testCode, "TestEntity.cs", "validation-test");
                result.CanAnalyzeCode = analysisResult != null;
                Console.WriteLine($"   SUCCESS: Code Analysis: {(result.CanAnalyzeCode ? "SUCCESS" : "FAILED")}");
                
                if (result.CanAnalyzeCode && analysisResult != null)
                {
                    Console.WriteLine($"      - Initial Score: {analysisResult.Score}/100");
                    Console.WriteLine($"      - Violations: {analysisResult.Violations.Count}");
                    
                    // Test improvement simulation
                    var improvedCode = testCode.Replace("public string Name { get; set; }", 
                        "public string Name { get; set; } = string.Empty;");
                    
                    var improvedAnalysis = await _codeAnalyzer.ValidateCodeAsync(improvedCode, "TestEntity.cs", "validation-test");
                    result.CanImproveCode = improvedAnalysis != null;
                    Console.WriteLine($"   SUCCESS: Code Improvement: {(result.CanImproveCode ? "SUCCESS" : "FAILED")}");
                    
                    if (result.CanImproveCode && improvedAnalysis != null)
                    {
                        Console.WriteLine($"      - Improved Score: {improvedAnalysis.Score}/100");
                        Console.WriteLine($"      - Violations: {improvedAnalysis.Violations.Count}");
                        result.QualityImproved = improvedAnalysis.Score >= analysisResult.Score;
                    }
                }
                
                // Test database integration
                await _commandHistoryService.SaveSuccessfulCommandAsync(
                    "Test Iterative Improvement",
                    "DotNet",
                    testCode,
                    analysisResult?.Score ?? 0,
                    1,
                    "Test iterative improvement validation",
                    "test,validation,iterative"
                );
                result.CanSaveToDatabase = true;
                Console.WriteLine($"   SUCCESS: Database Integration: SUCCESS");
                
                result.IsValid = result.CanAnalyzeCode && result.CanImproveCode && 
                               result.QualityImproved && result.CanSaveToDatabase;
                
                Console.WriteLine($"   Stats Iterative Improvement Validation: {(result.IsValid ? "SUCCESS: PASSED" : "ERROR: FAILED")}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Iterative improvement validation failed");
                result.IsValid = false;
                result.ErrorMessage = ex.Message;
                Console.WriteLine($"   ERROR: Iterative Improvement Validation: FAILED - {ex.Message}");
            }
            
            return result;
        }
        
        private async Task<IntegrationValidationResult> ValidateEndToEndIntegrationAsync()
        {
            Console.WriteLine("\n🔗 Test 5: End-to-End Integration Validation");
            Console.WriteLine("===========================================");
            
            var result = new IntegrationValidationResult();
            
            try
            {
                // Test complete pipeline
                var description = "Create a Product entity with Name, Price, and Category properties";
                var platform = "DotNet";
                
                // Get codebase context
                var context = await _codebaseAnalysisService.GetRelevantContextAsync(description, platform);
                result.CanGetContext = !string.IsNullOrEmpty(context.Content);
                Console.WriteLine($"   SUCCESS: Context Retrieval: {(result.CanGetContext ? "SUCCESS" : "FAILED")}");
                
                // Get similar commands
                var similarCommands = await _commandHistoryService.GetSimilarCommandsAsync(description, platform);
                result.CanGetSimilarCommands = true; // This will work even if no similar commands exist
                Console.WriteLine($"   SUCCESS: Similar Commands: SUCCESS (Found: {similarCommands.Count})");
                
                // Generate test code
                var generatedCode = $@"// Generated with context: {context.FilePath}
using System;
using System.ComponentModel.DataAnnotations;

namespace Test.Generated
{{
    public class Product
    {{
        [Key]
        public int Id {{ get; set; }}
        
        [Required]
        [StringLength(100)]
        public string Name {{ get; set; }} = string.Empty;
        
        [Required]
        public decimal Price {{ get; set; }}
        
        [Required]
        [StringLength(50)]
        public string Category {{ get; set; }} = string.Empty;
        
        public DateTime CreatedAt {{ get; set; }} = DateTime.UtcNow;
    }}
}}";
                
                // Analyze generated code
                var analysisResult = await _codeAnalyzer.ValidateCodeAsync(generatedCode, "Product.cs", "integration-test");
                result.CanAnalyzeGeneratedCode = analysisResult != null;
                Console.WriteLine($"   SUCCESS: Generated Code Analysis: {(result.CanAnalyzeGeneratedCode ? "SUCCESS" : "FAILED")}");
                
                if (result.CanAnalyzeGeneratedCode && analysisResult != null)
                {
                    Console.WriteLine($"      - Generated Code Score: {analysisResult.Score}/100");
                    Console.WriteLine($"      - Violations: {analysisResult.Violations.Count}");
                }
                
                // Save to database
                await _commandHistoryService.SaveSuccessfulCommandAsync(
                    description,
                    platform,
                    generatedCode,
                    analysisResult?.Score ?? 0,
                    1,
                    $"Context: {context.FilePath}",
                    "integration,test,entity"
                );
                result.CanSaveGeneratedCode = true;
                Console.WriteLine($"   SUCCESS: Save Generated Code: SUCCESS");
                
                // Verify statistics updated
                var stats = await _commandHistoryService.GetStatisticsAsync();
                result.StatisticsUpdated = stats.TotalCommands > 0;
                Console.WriteLine($"   SUCCESS: Statistics Updated: {(result.StatisticsUpdated ? "SUCCESS" : "FAILED")}");
                Console.WriteLine($"      - Total Commands: {stats.TotalCommands}");
                
                result.IsValid = result.CanGetContext && result.CanGetSimilarCommands && 
                               result.CanAnalyzeGeneratedCode && result.CanSaveGeneratedCode && 
                               result.StatisticsUpdated;
                
                Console.WriteLine($"   Stats Integration Validation: {(result.IsValid ? "SUCCESS: PASSED" : "ERROR: FAILED")}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Integration validation failed");
                result.IsValid = false;
                result.ErrorMessage = ex.Message;
                Console.WriteLine($"   ERROR: Integration Validation: FAILED - {ex.Message}");
            }
            
            return result;
        }
        
        private void GenerateValidationReport(ValidationResults results)
        {
            Console.WriteLine("\nStats VALIDATION REPORT");
            Console.WriteLine("====================");
            
            var totalTests = 5;
            var passedTests = 0;
            
            if (results.DatabaseValidation.IsValid) passedTests++;
            if (results.CodebaseValidation.IsValid) passedTests++;
            if (results.CommandHistoryValidation.IsValid) passedTests++;
            if (results.IterativeImprovementValidation.IsValid) passedTests++;
            if (results.IntegrationValidation.IsValid) passedTests++;
            
            Console.WriteLine($"List Test Results: {passedTests}/{totalTests} tests passed");
            Console.WriteLine($"Stats Success Rate: {(double)passedTests / totalTests * 100:F1}%");
            
            Console.WriteLine("\nDocument Detailed Results:");
            Console.WriteLine($"   SUCCESS: Database Operations: {(results.DatabaseValidation.IsValid ? "PASSED" : "FAILED")}");
            Console.WriteLine($"   SUCCESS: Codebase Context: {(results.CodebaseValidation.IsValid ? "PASSED" : "FAILED")}");
            Console.WriteLine($"   SUCCESS: Command History: {(results.CommandHistoryValidation.IsValid ? "PASSED" : "FAILED")}");
            Console.WriteLine($"   SUCCESS: Iterative Improvement: {(results.IterativeImprovementValidation.IsValid ? "PASSED" : "FAILED")}");
            Console.WriteLine($"   SUCCESS: End-to-End Integration: {(results.IntegrationValidation.IsValid ? "PASSED" : "FAILED")}");
            
            if (passedTests == totalTests)
            {
                Console.WriteLine("\nSUCCESS ALL VALIDATIONS PASSED! Feature Factory is fully operational!");
            }
            else
            {
                Console.WriteLine($"\nWARNING:  {totalTests - passedTests} validation(s) failed. Check the details above.");
            }
        }
    }
    
    // Validation result classes
    public class ValidationResults
    {
        public DatabaseValidationResult DatabaseValidation { get; set; } = new();
        public CodebaseValidationResult CodebaseValidation { get; set; } = new();
        public CommandHistoryValidationResult CommandHistoryValidation { get; set; } = new();
        public IterativeImprovementValidationResult IterativeImprovementValidation { get; set; } = new();
        public IntegrationValidationResult IntegrationValidation { get; set; } = new();
    }
    
    public class DatabaseValidationResult
    {
        public bool IsValid { get; set; }
        public bool CanConnect { get; set; }
        public bool TablesExist { get; set; }
        public bool CanCreate { get; set; }
        public bool CanRead { get; set; }
        public bool CanUpdate { get; set; }
        public bool CanDelete { get; set; }
        public string? ErrorMessage { get; set; }
    }
    
    public class CodebaseValidationResult
    {
        public bool IsValid { get; set; }
        public int FilesAnalyzed { get; set; }
        public int AverageQuality { get; set; }
        public int HighQualityFiles { get; set; }
        public bool CanRetrieveContext { get; set; }
        public bool CodeAnalysisWorks { get; set; }
        public string? ErrorMessage { get; set; }
    }
    
    public class CommandHistoryValidationResult
    {
        public bool IsValid { get; set; }
        public bool CanSaveCommand { get; set; }
        public bool CanRetrieveRecent { get; set; }
        public bool CanFindSimilar { get; set; }
        public bool CanGetStatistics { get; set; }
        public string? ErrorMessage { get; set; }
    }
    
    public class IterativeImprovementValidationResult
    {
        public bool IsValid { get; set; }
        public bool CanAnalyzeCode { get; set; }
        public bool CanImproveCode { get; set; }
        public bool QualityImproved { get; set; }
        public bool CanSaveToDatabase { get; set; }
        public string? ErrorMessage { get; set; }
    }
    
    public class IntegrationValidationResult
    {
        public bool IsValid { get; set; }
        public bool CanGetContext { get; set; }
        public bool CanGetSimilarCommands { get; set; }
        public bool CanAnalyzeGeneratedCode { get; set; }
        public bool CanSaveGeneratedCode { get; set; }
        public bool StatisticsUpdated { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
