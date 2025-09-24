using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Entities.Infrastructure.DataStore
{
    /// <summary>
    /// Data store configuration
    /// </summary>
    public record DataStoreConfiguration
    {
        /// <summary>
        /// Configuration identifier
        /// </summary>
        public string Id { get; init; } = string.Empty;
        
        /// <summary>
        /// Configuration name
        /// </summary>
        public string Name { get; init; } = string.Empty;
        
        /// <summary>
        /// Data store type
        /// </summary>
        public string StoreType { get; init; } = string.Empty;
        
        /// <summary>
        /// Connection string
        /// </summary>
        public string ConnectionString { get; init; } = string.Empty;
        
        /// <summary>
        /// Configuration settings
        /// </summary>
        public Dictionary<string, object> Settings { get; init; } = new();
        
        /// <summary>
        /// Configuration metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; init; } = new();
    }

    /// <summary>
    /// Data store connection
    /// </summary>
    public record DataStoreConnection
    {
        /// <summary>
        /// Connection identifier
        /// </summary>
        public string Id { get; init; } = string.Empty;
        
        /// <summary>
        /// Connection name
        /// </summary>
        public string Name { get; init; } = string.Empty;
        
        /// <summary>
        /// Connection string
        /// </summary>
        public string ConnectionString { get; init; } = string.Empty;
        
        /// <summary>
        /// Connection timeout
        /// </summary>
        public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);
        
        /// <summary>
        /// Connection status
        /// </summary>
        public string Status { get; init; } = "Unknown";
        
        /// <summary>
        /// Connection metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; init; } = new();
    }

    /// <summary>
    /// Data store schema
    /// </summary>
    public record DataStoreSchema
    {
        /// <summary>
        /// Schema identifier
        /// </summary>
        public string Id { get; init; } = string.Empty;
        
        /// <summary>
        /// Schema name
        /// </summary>
        public string Name { get; init; } = string.Empty;
        
        /// <summary>
        /// Schema version
        /// </summary>
        public string Version { get; init; } = "1.0.0";
        
        /// <summary>
        /// Schema tables
        /// </summary>
        public List<SchemaTable> Tables { get; init; } = new();
        
        /// <summary>
        /// Schema metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; init; } = new();
    }

    /// <summary>
    /// Schema table
    /// </summary>
    public record SchemaTable
    {
        /// <summary>
        /// Table name
        /// </summary>
        public string Name { get; init; } = string.Empty;
        
        /// <summary>
        /// Table columns
        /// </summary>
        public List<SchemaColumn> Columns { get; init; } = new();
        
        /// <summary>
        /// Table indexes
        /// </summary>
        public List<SchemaIndex> Indexes { get; init; } = new();
        
        /// <summary>
        /// Table constraints
        /// </summary>
        public List<SchemaConstraint> Constraints { get; init; } = new();
        
        /// <summary>
        /// Table metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; init; } = new();
    }

    /// <summary>
    /// Schema column
    /// </summary>
    public record SchemaColumn
    {
        /// <summary>
        /// Column name
        /// </summary>
        public string Name { get; init; } = string.Empty;
        
        /// <summary>
        /// Column data type
        /// </summary>
        public string DataType { get; init; } = string.Empty;
        
        /// <summary>
        /// Column length
        /// </summary>
        public int Length { get; init; }
        
        /// <summary>
        /// Is nullable
        /// </summary>
        public bool IsNullable { get; init; } = true;
        
        /// <summary>
        /// Is primary key
        /// </summary>
        public bool IsPrimaryKey { get; init; } = false;
        
        /// <summary>
        /// Default value
        /// </summary>
        public object DefaultValue { get; init; } = null;
        
        /// <summary>
        /// Column metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; init; } = new();
    }

    /// <summary>
    /// Schema index
    /// </summary>
    public record SchemaIndex
    {
        /// <summary>
        /// Index name
        /// </summary>
        public string Name { get; init; } = string.Empty;
        
        /// <summary>
        /// Index columns
        /// </summary>
        public List<string> Columns { get; init; } = new();
        
        /// <summary>
        /// Is unique index
        /// </summary>
        public bool IsUnique { get; init; } = false;
        
        /// <summary>
        /// Index type
        /// </summary>
        public string Type { get; init; } = "btree";
        
        /// <summary>
        /// Index metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; init; } = new();
    }

    /// <summary>
    /// Schema constraint
    /// </summary>
    public record SchemaConstraint
    {
        /// <summary>
        /// Constraint name
        /// </summary>
        public string Name { get; init; } = string.Empty;
        
        /// <summary>
        /// Constraint type
        /// </summary>
        public string Type { get; init; } = string.Empty;
        
        /// <summary>
        /// Constraint columns
        /// </summary>
        public List<string> Columns { get; init; } = new();
        
        /// <summary>
        /// Constraint expression
        /// </summary>
        public string Expression { get; init; } = string.Empty;
        
        /// <summary>
        /// Constraint metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; init; } = new();
    }
}
