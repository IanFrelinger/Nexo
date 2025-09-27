using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FeatureFactoryDemo.Data;
using FeatureFactoryDemo.Models;

namespace FeatureFactoryDemo.Validation
{
    /// <summary>
    /// Database validation functionality
    /// </summary>
    public partial class FeatureValidationService
    {
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
    }
}
