namespace Nexo.Feature.Data.Enums
{
    /// <summary>
    /// Supported database providers
    /// </summary>
    public enum DatabaseProvider
    {
        /// <summary>
        /// SQL Server
        /// </summary>
        SqlServer,

        /// <summary>
        /// PostgreSQL
        /// </summary>
        PostgreSQL,

        /// <summary>
        /// SQLite
        /// </summary>
        Sqlite,

        /// <summary>
        /// In-Memory (for testing)
        /// </summary>
        InMemory,

        /// <summary>
        /// MySQL
        /// </summary>
        MySql,

        /// <summary>
        /// Oracle
        /// </summary>
        Oracle,

        /// <summary>
        /// Cosmos DB
        /// </summary>
        CosmosDb,

        /// <summary>
        /// MongoDB
        /// </summary>
        MongoDb
    }
} 