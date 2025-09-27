using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FeatureFactoryDemo.Data;
using FeatureFactoryDemo.Services;
using FeatureFactoryDemo.Models;
using Nexo.Feature.Analysis.Interfaces;

namespace FeatureFactoryDemo.Validation
{
    /// <summary>
    /// Core validation functionality
    /// </summary>
    public partial class FeatureValidationService
    {
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
    }
}
