using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FeatureFactoryDemo.Data;
using FeatureFactoryDemo.Models;

namespace FeatureFactoryDemo.Services
{
    /// <summary>
    /// Service for managing command history and successful code generation records
    /// </summary>
    public class CommandHistoryService
    {
        private readonly FeatureFactoryDbContext _context;
        private readonly ILogger<CommandHistoryService> _logger;
        
        public CommandHistoryService(FeatureFactoryDbContext context, ILogger<CommandHistoryService> logger)
        {
            _context = context;
            _logger = logger;
        }
        
        /// <summary>
        /// Saves a successful command execution to the database
        /// </summary>
        public async Task SaveSuccessfulCommandAsync(string description, string platform, string generatedCode, 
            int qualityScore, int iterationCount, string? context = null, string? tags = null)
        {
            try
            {
                var commandHistory = new CommandHistory
                {
                    Description = description,
                    Platform = platform,
                    GeneratedCode = generatedCode,
                    FinalQualityScore = qualityScore,
                    IterationCount = iterationCount,
                    Context = context,
                    Tags = tags,
                    IsSuccessful = true,
                    ExecutedAt = DateTime.UtcNow
                };
                
                _context.CommandHistories.Add(commandHistory);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Successfully saved command history: {Description} (Quality: {QualityScore}/100)", 
                    description, qualityScore);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save command history for: {Description}", description);
            }
        }
        
        /// <summary>
        /// Retrieves recent successful commands for context
        /// </summary>
        public async Task<List<CommandHistory>> GetRecentSuccessfulCommandsAsync(int count = 10)
        {
            try
            {
                return await _context.CommandHistories
                    .Where(c => c.IsSuccessful && c.FinalQualityScore >= 80)
                    .OrderByDescending(c => c.ExecutedAt)
                    .Take(count)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve recent successful commands");
                return new List<CommandHistory>();
            }
        }
        
        /// <summary>
        /// Gets commands similar to the current request for context
        /// </summary>
        public async Task<List<CommandHistory>> GetSimilarCommandsAsync(string description, string platform)
        {
            try
            {
                var firstWord = description.Split(' ')[0];
                var allCommands = await _context.CommandHistories
                    .Where(c => c.IsSuccessful && c.Platform == platform)
                    .ToListAsync();
                
                return allCommands
                    .Where(c => c.Description.Contains(firstWord) || description.Contains(c.Description.Split(' ')[0]))
                    .OrderByDescending(c => c.FinalQualityScore)
                    .Take(5)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve similar commands");
                return new List<CommandHistory>();
            }
        }
        
        /// <summary>
        /// Gets statistics about command history
        /// </summary>
        public async Task<CommandHistoryStats> GetStatisticsAsync()
        {
            try
            {
                var totalCommands = await _context.CommandHistories.CountAsync();
                var successfulCommands = await _context.CommandHistories.CountAsync(c => c.IsSuccessful);
                var averageQuality = await _context.CommandHistories
                    .Where(c => c.IsSuccessful)
                    .AverageAsync(c => (double)c.FinalQualityScore);
                var averageIterations = await _context.CommandHistories
                    .Where(c => c.IsSuccessful)
                    .AverageAsync(c => (double)c.IterationCount);
                
                return new CommandHistoryStats
                {
                    TotalCommands = totalCommands,
                    SuccessfulCommands = successfulCommands,
                    AverageQualityScore = (int)Math.Round(averageQuality),
                    AverageIterations = (int)Math.Round(averageIterations),
                    SuccessRate = totalCommands > 0 ? (double)successfulCommands / totalCommands * 100 : 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve command statistics");
                return new CommandHistoryStats();
            }
        }
    }
    
    /// <summary>
    /// Statistics about command history
    /// </summary>
    public class CommandHistoryStats
    {
        public int TotalCommands { get; set; }
        public int SuccessfulCommands { get; set; }
        public int AverageQualityScore { get; set; }
        public int AverageIterations { get; set; }
        public double SuccessRate { get; set; }
    }
}
