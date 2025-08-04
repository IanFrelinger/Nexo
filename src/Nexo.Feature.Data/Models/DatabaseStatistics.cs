using Nexo.Feature.Data.Enums;

namespace Nexo.Feature.Data.Models
{
    /// <summary>
    /// Database statistics and performance metrics
    /// </summary>
    public class DatabaseStatistics
    {
        /// <summary>
        /// Database name
        /// </summary>
        public string DatabaseName { get; set; } = string.Empty;

        /// <summary>
        /// Database provider
        /// </summary>
        public DatabaseProvider Provider { get; set; }

        /// <summary>
        /// Connection string (masked for security)
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Whether the database is online
        /// </summary>
        public bool IsOnline { get; set; }

        /// <summary>
        /// Database size in bytes
        /// </summary>
        public long DatabaseSize { get; set; }

        /// <summary>
        /// Available space in bytes
        /// </summary>
        public long AvailableSpace { get; set; }

        /// <summary>
        /// Number of active connections
        /// </summary>
        public int ActiveConnections { get; set; }

        /// <summary>
        /// Maximum allowed connections
        /// </summary>
        public int MaxConnections { get; set; }

        /// <summary>
        /// Database uptime
        /// </summary>
        public TimeSpan Uptime { get; set; }

        /// <summary>
        /// Last backup time
        /// </summary>
        public DateTime? LastBackupTime { get; set; }

        /// <summary>
        /// Database version
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Performance metrics
        /// </summary>
        public PerformanceMetrics Performance { get; set; } = new();

        /// <summary>
        /// Table statistics
        /// </summary>
        public List<TableStatistics> Tables { get; set; } = new();

        /// <summary>
        /// Index statistics
        /// </summary>
        public List<IndexStatistics> Indexes { get; set; } = new();

        /// <summary>
        /// Last updated timestamp
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Database performance metrics
    /// </summary>
    public class PerformanceMetrics
    {
        /// <summary>
        /// Average query execution time in milliseconds
        /// </summary>
        public double AverageQueryTime { get; set; }

        /// <summary>
        /// Slowest query execution time in milliseconds
        /// </summary>
        public double SlowestQueryTime { get; set; }

        /// <summary>
        /// Number of queries executed
        /// </summary>
        public long QueryCount { get; set; }

        /// <summary>
        /// Number of slow queries (above threshold)
        /// </summary>
        public long SlowQueryCount { get; set; }

        /// <summary>
        /// Cache hit ratio (0-1)
        /// </summary>
        public double CacheHitRatio { get; set; }

        /// <summary>
        /// Buffer pool hit ratio (0-1)
        /// </summary>
        public double BufferPoolHitRatio { get; set; }

        /// <summary>
        /// Lock wait time in milliseconds
        /// </summary>
        public double LockWaitTime { get; set; }

        /// <summary>
        /// Deadlock count
        /// </summary>
        public long DeadlockCount { get; set; }
    }

    /// <summary>
    /// Table statistics
    /// </summary>
    public class TableStatistics
    {
        /// <summary>
        /// Table name
        /// </summary>
        public string TableName { get; set; } = string.Empty;

        /// <summary>
        /// Schema name
        /// </summary>
        public string SchemaName { get; set; } = string.Empty;

        /// <summary>
        /// Number of rows
        /// </summary>
        public long RowCount { get; set; }

        /// <summary>
        /// Table size in bytes
        /// </summary>
        public long TableSize { get; set; }

        /// <summary>
        /// Index size in bytes
        /// </summary>
        public long IndexSize { get; set; }

        /// <summary>
        /// Last modified date
        /// </summary>
        public DateTime LastModified { get; set; }
    }

    /// <summary>
    /// Index statistics
    /// </summary>
    public class IndexStatistics
    {
        /// <summary>
        /// Index name
        /// </summary>
        public string IndexName { get; set; } = string.Empty;

        /// <summary>
        /// Table name
        /// </summary>
        public string TableName { get; set; } = string.Empty;

        /// <summary>
        /// Index type
        /// </summary>
        public string IndexType { get; set; } = string.Empty;

        /// <summary>
        /// Number of index scans
        /// </summary>
        public long ScanCount { get; set; }

        /// <summary>
        /// Number of index seeks
        /// </summary>
        public long SeekCount { get; set; }

        /// <summary>
        /// Index fragmentation percentage
        /// </summary>
        public double Fragmentation { get; set; }

        /// <summary>
        /// Index size in bytes
        /// </summary>
        public long IndexSize { get; set; }

        /// <summary>
        /// Whether the index is unique
        /// </summary>
        public bool IsUnique { get; set; }
    }
} 