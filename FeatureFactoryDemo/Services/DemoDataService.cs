using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using FeatureFactoryDemo.Data;
using Microsoft.EntityFrameworkCore;

namespace FeatureFactoryDemo.Services
{
    public class DemoDataService
    {
        private readonly ILogger<DemoDataService> _logger;
        private readonly FeatureFactoryDbContext _context;

        public DemoDataService(
            ILogger<DemoDataService> logger,
            FeatureFactoryDbContext context)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task InitializeDemoDataAsync()
        {
            try
            {
                _logger.LogInformation("Initializing demo data");

                // Ensure database is created
                await _context.Database.EnsureCreatedAsync();

                // Seed command history
                await SeedCommandHistoryAsync();

                // Seed coding standards
                await SeedCodingStandardsAsync();

                _logger.LogInformation("Demo data initialization completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing demo data");
                throw;
            }
        }

        private async Task SeedCommandHistoryAsync()
        {
            if (await _context.CommandHistories.AnyAsync())
                return;

            var commands = new List<CommandHistory>
            {
                new CommandHistory
                {
                    Command = "Create user authentication service",
                    Description = "Implement JWT-based authentication with user management",
                    Success = true,
                    Timestamp = DateTime.UtcNow.AddDays(-1)
                },
                new CommandHistory
                {
                    Command = "Create payment processing service",
                    Description = "Implement Stripe payment integration with webhook handling",
                    Success = true,
                    Timestamp = DateTime.UtcNow.AddDays(-2)
                },
                new CommandHistory
                {
                    Command = "Create order management system",
                    Description = "Implement order processing with inventory management",
                    Success = true,
                    Timestamp = DateTime.UtcNow.AddDays(-3)
                },
                new CommandHistory
                {
                    Command = "Create notification service",
                    Description = "Implement email and SMS notification system",
                    Success = false,
                    Timestamp = DateTime.UtcNow.AddDays(-4)
                },
                new CommandHistory
                {
                    Command = "Create data analytics dashboard",
                    Description = "Implement real-time analytics with charts and metrics",
                    Success = true,
                    Timestamp = DateTime.UtcNow.AddDays(-5)
                },
                new CommandHistory
                {
                    Command = "Create API gateway",
                    Description = "Implement API routing and rate limiting",
                    Success = true,
                    Timestamp = DateTime.UtcNow.AddDays(-6)
                },
                new CommandHistory
                {
                    Command = "Create file upload service",
                    Description = "Implement secure file upload with virus scanning",
                    Success = false,
                    Timestamp = DateTime.UtcNow.AddDays(-7)
                },
                new CommandHistory
                {
                    Command = "Create search functionality",
                    Description = "Implement full-text search with Elasticsearch",
                    Success = true,
                    Timestamp = DateTime.UtcNow.AddDays(-8)
                }
            };

            _context.CommandHistories.AddRange(commands);
            await _context.SaveChangesAsync();
        }

        private async Task SeedCodingStandardsAsync()
        {
            if (await _context.CodingStandards.AnyAsync())
                return;

            var standards = new List<CodingStandard>
            {
                new CodingStandard
                {
                    Name = "Naming Conventions",
                    Description = "Use PascalCase for classes and methods, camelCase for variables",
                    Severity = "High",
                    Category = "Naming"
                },
                new CodingStandard
                {
                    Name = "Error Handling",
                    Description = "Always use try-catch blocks and proper exception handling",
                    Severity = "High",
                    Category = "Error Handling"
                },
                new CodingStandard
                {
                    Name = "Logging",
                    Description = "Include structured logging in all public methods",
                    Severity = "Medium",
                    Category = "Logging"
                },
                new CodingStandard
                {
                    Name = "Async/Await",
                    Description = "Use async/await for I/O operations and long-running tasks",
                    Severity = "High",
                    Category = "Performance"
                },
                new CodingStandard
                {
                    Name = "Dependency Injection",
                    Description = "Use constructor injection for dependencies",
                    Severity = "High",
                    Category = "Architecture"
                },
                new CodingStandard
                {
                    Name = "Null Checks",
                    Description = "Always validate input parameters for null values",
                    Severity = "High",
                    Category = "Validation"
                },
                new CodingStandard
                {
                    Name = "Documentation",
                    Description = "Include XML documentation for public APIs",
                    Severity = "Medium",
                    Category = "Documentation"
                },
                new CodingStandard
                {
                    Name = "Unit Tests",
                    Description = "Write unit tests for all business logic",
                    Severity = "Medium",
                    Category = "Testing"
                }
            };

            _context.CodingStandards.AddRange(standards);
            await _context.SaveChangesAsync();
        }

        public async Task<List<CommandHistory>> GetDemoCommandsAsync()
        {
            try
            {
                return await _context.CommandHistories
                    .OrderByDescending(c => c.Timestamp)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting demo commands");
                return new List<CommandHistory>();
            }
        }

        public async Task<List<CodingStandard>> GetCodingStandardsAsync()
        {
            try
            {
                return await _context.CodingStandards.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting coding standards");
                return new List<CodingStandard>();
            }
        }

        public async Task ClearDemoDataAsync()
        {
            try
            {
                _logger.LogInformation("Clearing demo data");

                _context.CommandHistories.RemoveRange(_context.CommandHistories);
                _context.CodingStandards.RemoveRange(_context.CodingStandards);
                
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing demo data");
                throw;
            }
        }
    }
}
