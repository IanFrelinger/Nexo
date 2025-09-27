using System;
using System.Collections.Generic;

namespace Nexo.Shared
{
    /// <summary>
    /// Database system names and identifiers with case-insensitive matching.
    /// </summary>
    public static partial class SystemConstants
    {
        public static class Databases
        {
            public const string SQLServer = "SQL Server";
            public const string PostgreSQL = "PostgreSQL";
            public const string MySQL = "MySQL";
            public const string MariaDB = "MariaDB";
            public const string Oracle = "Oracle";
            public const string SQLite = "SQLite";
            public const string MongoDB = "MongoDB";
            public const string Redis = "Redis";
            public const string Cassandra = "Cassandra";
            public const string Elasticsearch = "Elasticsearch";
            public const string InfluxDB = "InfluxDB";
            public const string CosmosDB = "Cosmos DB";
            public const string DynamoDB = "DynamoDB";

            /// <summary>
            /// Gets all database variations for case-insensitive matching.
            /// </summary>
            public static readonly HashSet<string> AllVariations = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                SQLServer, PostgreSQL, MySQL, MariaDB, Oracle, SQLite, MongoDB, Redis, 
                Cassandra, Elasticsearch, InfluxDB, CosmosDB, DynamoDB,
                "sql server", "postgresql", "mysql", "mariadb", "oracle", "sqlite", "mongodb", "redis",
                "cassandra", "elasticsearch", "influxdb", "cosmos db", "dynamodb"
            };

            /// <summary>
            /// Tries to match a database name case-insensitively.
            /// </summary>
            /// <param name="databaseName">The database name to match.</param>
            /// <returns>The standardized database name or empty string if not found.</returns>
            public static string MatchDatabase(string databaseName)
            {
                if (string.IsNullOrWhiteSpace(databaseName))
                    return string.Empty;

                var normalizedName = databaseName.Trim();
                
                if (AllVariations.Contains(normalizedName))
                    return normalizedName;
                
                return string.Empty;
            }
        }
    }
}
