using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using FeatureFactoryDemo.Data;
using Microsoft.EntityFrameworkCore;

namespace FeatureFactoryDemo.Services
{
    public class CommandHistoryService
    {
        private readonly ILogger<CommandHistoryService> _logger;
        private readonly FeatureFactoryDbContext _context;

        public CommandHistoryService(
            ILogger<CommandHistoryService> logger,
            FeatureFactoryDbContext context)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<List<CommandHistory>> GetSimilarCommandsAsync(string description)
        {
            try
            {
                _logger.LogInformation("Finding similar commands for: {Description}", description);

                var commands = await _context.CommandHistories.ToListAsync();
                
                // Simple similarity calculation based on common words
                var descriptionWords = description.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                
                var similarCommands = commands
                    .Select(cmd => new
                    {
                        Command = cmd,
                        Similarity = CalculateSimilarity(descriptionWords, cmd.Command.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries))
                    })
                    .Where(x => x.Similarity > 0.3)
                    .OrderByDescending(x => x.Similarity)
                    .Select(x => new CommandHistory
                    {
                        Command = x.Command.Command,
                        SimilarityScore = x.Similarity,
                        Timestamp = x.Command.Timestamp
                    })
                    .ToList();

                return similarCommands;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding similar commands");
                return new List<CommandHistory>();
            }
        }

        public async Task SaveCommandAsync(string command, string description, bool success)
        {
            try
            {
                _logger.LogInformation("Saving command: {Command}", command);

                var commandHistory = new CommandHistory
                {
                    Command = command,
                    Description = description,
                    Success = success,
                    Timestamp = DateTime.UtcNow
                };

                _context.CommandHistories.Add(commandHistory);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving command");
            }
        }

        public async Task<List<CommandHistory>> GetRecentCommandsAsync(int count = 10)
        {
            try
            {
                return await _context.CommandHistories
                    .OrderByDescending(c => c.Timestamp)
                    .Take(count)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent commands");
                return new List<CommandHistory>();
            }
        }

        public async Task<Dictionary<string, int>> GetCommandStatisticsAsync()
        {
            try
            {
                var commands = await _context.CommandHistories.ToListAsync();
                
                var statistics = new Dictionary<string, int>();
                
                foreach (var command in commands)
                {
                    var key = command.Success ? "Successful" : "Failed";
                    statistics[key] = statistics.GetValueOrDefault(key, 0) + 1;
                }

                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting command statistics");
                return new Dictionary<string, int>();
            }
        }

        private double CalculateSimilarity(string[] words1, string[] words2)
        {
            if (words1.Length == 0 || words2.Length == 0)
                return 0;

            var commonWords = words1.Intersect(words2).Count();
            var totalWords = words1.Union(words2).Count();
            
            return (double)commonWords / totalWords;
        }
    }
}