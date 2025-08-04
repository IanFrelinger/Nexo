using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Data.Enums;

namespace Nexo.Feature.Data.Contexts
{
    /// <summary>
    /// Main Nexo database context
    /// </summary>
    public class NexoDbContext : DbContext
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<NexoDbContext> _logger;
        private readonly DatabaseProvider _provider;

        public NexoDbContext(
            DbContextOptions<NexoDbContext> options,
            IConfiguration configuration,
            ILogger<NexoDbContext> logger) : base(options)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Determine the database provider from the connection string
            _provider = DetermineDatabaseProvider();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidOperationException("DefaultConnection connection string is not configured");
                }

                ConfigureDatabaseProvider(optionsBuilder, connectionString);
            }

            // Enable sensitive data logging in development
            var enableSensitiveDataLogging = _configuration.GetSection("Logging:EnableSensitiveDataLogging").Value;
            if (bool.TryParse(enableSensitiveDataLogging, out var sensitiveLogging) && sensitiveLogging)
            {
                optionsBuilder.EnableSensitiveDataLogging();
            }

            // Enable detailed errors in development
            var enableDetailedErrors = _configuration.GetSection("Logging:EnableDetailedErrors").Value;
            if (bool.TryParse(enableDetailedErrors, out var detailedErrors) && detailedErrors)
            {
                optionsBuilder.EnableDetailedErrors();
            }

            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply all entity configurations
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(NexoDbContext).Assembly);

            // Configure global query filters if needed
            ConfigureGlobalQueryFilters(modelBuilder);

            _logger.LogInformation("Database model created successfully for provider {Provider}", _provider);
        }

        /// <summary>
        /// Determines the database provider from the connection string
        /// </summary>
        /// <returns>The database provider</returns>
        private DatabaseProvider DetermineDatabaseProvider()
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            
            if (string.IsNullOrEmpty(connectionString))
            {
                return DatabaseProvider.InMemory; // Default fallback
            }

            if (connectionString.Contains("Server=") || connectionString.Contains("Data Source="))
            {
                if (connectionString.Contains("Initial Catalog=") || connectionString.Contains("Database="))
                {
                    return DatabaseProvider.SqlServer;
                }
            }

            if (connectionString.Contains("Host=") || connectionString.Contains("Server="))
            {
                if (connectionString.Contains("Database=") || connectionString.Contains("Initial Catalog="))
                {
                    return DatabaseProvider.PostgreSQL;
                }
            }

            if (connectionString.Contains("Data Source=") && connectionString.Contains(".db"))
            {
                return DatabaseProvider.Sqlite;
            }

            if (connectionString.Contains("UseInMemoryDatabase"))
            {
                return DatabaseProvider.InMemory;
            }

            // Default to SQL Server if we can't determine
            return DatabaseProvider.SqlServer;
        }

        /// <summary>
        /// Configures the database provider
        /// </summary>
        /// <param name="optionsBuilder">The options builder</param>
        /// <param name="connectionString">The connection string</param>
        private void ConfigureDatabaseProvider(DbContextOptionsBuilder optionsBuilder, string connectionString)
        {
            switch (_provider)
            {
                case DatabaseProvider.SqlServer:
                    optionsBuilder.UseSqlServer(connectionString, options =>
                    {
                        options.EnableRetryOnFailure(
                            maxRetryCount: 3,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null);
                    });
                    break;

                case DatabaseProvider.PostgreSQL:
                    optionsBuilder.UseNpgsql(connectionString, options =>
                    {
                        options.EnableRetryOnFailure(
                            maxRetryCount: 3,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorCodesToAdd: null);
                    });
                    break;

                case DatabaseProvider.Sqlite:
                    optionsBuilder.UseSqlite(connectionString);
                    break;

                case DatabaseProvider.InMemory:
                    optionsBuilder.UseInMemoryDatabase(connectionString);
                    break;

                default:
                    throw new NotSupportedException($"Database provider {_provider} is not supported");
            }

            _logger.LogInformation("Configured database provider: {Provider}", _provider);
        }

        /// <summary>
        /// Configures global query filters
        /// </summary>
        /// <param name="modelBuilder">The model builder</param>
        private void ConfigureGlobalQueryFilters(ModelBuilder modelBuilder)
        {
            // Example: Add soft delete filter for entities that implement ISoftDelete
            // modelBuilder.Entity<YourEntity>().HasQueryFilter(e => !e.IsDeleted);

            // Example: Add tenant filter for multi-tenant applications
            // modelBuilder.Entity<YourEntity>().HasQueryFilter(e => e.TenantId == _currentTenantId);
        }

        /// <summary>
        /// Gets the database provider
        /// </summary>
        public DatabaseProvider Provider => _provider;

        /// <summary>
        /// Ensures the database is created and migrations are applied
        /// </summary>
        public async Task EnsureDatabaseCreatedAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Ensuring database is created for provider {Provider}", _provider);

                if (_provider == DatabaseProvider.InMemory)
                {
                    await Database.EnsureCreatedAsync(cancellationToken);
                }
                else
                {
                    await Database.MigrateAsync(cancellationToken);
                }

                _logger.LogInformation("Database setup completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring database is created");
                throw;
            }
        }

        /// <summary>
        /// Tests the database connection
        /// </summary>
        /// <returns>True if connection is successful</returns>
        public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Testing database connection for provider {Provider}", _provider);
                return await Database.CanConnectAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing database connection");
                return false;
            }
        }

        /// <summary>
        /// Gets database statistics
        /// </summary>
        /// <returns>Database statistics</returns>
        public async Task<Models.DatabaseStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var statistics = new Models.DatabaseStatistics
                {
                    Provider = _provider,
                    IsOnline = await Database.CanConnectAsync(cancellationToken),
                    LastUpdated = DateTime.UtcNow
                };

                // Set database name and connection string based on provider
                if (_provider == DatabaseProvider.InMemory)
                {
                    statistics.DatabaseName = "InMemoryDatabase";
                    statistics.ConnectionString = MaskConnectionString("UseInMemoryDatabase");
                }
                else
                {
                    try
                    {
                        statistics.DatabaseName = Database.GetDbConnection().Database;
                        statistics.ConnectionString = MaskConnectionString(Database.GetConnectionString());
                    }
                    catch (InvalidOperationException)
                    {
                        // Fallback for cases where relational methods are not available
                        statistics.DatabaseName = "Unknown";
                        statistics.ConnectionString = MaskConnectionString("Unknown");
                    }
                }

                // Add provider-specific statistics
                await AddProviderSpecificStatistics(statistics, cancellationToken);

                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting database statistics");
                throw;
            }
        }

        /// <summary>
        /// Adds provider-specific statistics
        /// </summary>
        /// <param name="statistics">The statistics object to populate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        private async Task AddProviderSpecificStatistics(Models.DatabaseStatistics statistics, CancellationToken cancellationToken)
        {
            try
            {
                switch (_provider)
                {
                    case DatabaseProvider.SqlServer:
                        await AddSqlServerStatistics(statistics, cancellationToken);
                        break;
                    case DatabaseProvider.PostgreSQL:
                        await AddPostgreSQLStatistics(statistics, cancellationToken);
                        break;
                    case DatabaseProvider.Sqlite:
                        await AddSqliteStatistics(statistics, cancellationToken);
                        break;
                    case DatabaseProvider.InMemory:
                        await AddInMemoryStatistics(statistics, cancellationToken);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error adding provider-specific statistics for {Provider}", _provider);
            }
        }

        private async Task AddSqlServerStatistics(Models.DatabaseStatistics statistics, CancellationToken cancellationToken)
        {
            // SQL Server specific statistics
            // This would include queries to get actual database statistics
            await Task.CompletedTask;
        }

        private async Task AddPostgreSQLStatistics(Models.DatabaseStatistics statistics, CancellationToken cancellationToken)
        {
            // PostgreSQL specific statistics
            await Task.CompletedTask;
        }

        private async Task AddSqliteStatistics(Models.DatabaseStatistics statistics, CancellationToken cancellationToken)
        {
            // SQLite specific statistics
            await Task.CompletedTask;
        }

        private async Task AddInMemoryStatistics(Models.DatabaseStatistics statistics, CancellationToken cancellationToken)
        {
            // In-memory database statistics
            statistics.DatabaseSize = 0;
            statistics.AvailableSpace = long.MaxValue;
            await Task.CompletedTask;
        }

        /// <summary>
        /// Masks sensitive information in connection string
        /// </summary>
        /// <param name="connectionString">The connection string to mask</param>
        /// <returns>Masked connection string</returns>
        private static string MaskConnectionString(string? connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                return string.Empty;

            // Simple masking - in production, use a more sophisticated approach
            return connectionString.Replace("Password=", "Password=***")
                                 .Replace("Pwd=", "Pwd=***")
                                 .Replace("User ID=", "User ID=***")
                                 .Replace("UID=", "UID=***");
        }
    }
} 