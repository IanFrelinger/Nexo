using System;
using System.Collections.Generic;

namespace Nexo.Feature.Platform.Models
{
    /// <summary>
    /// Core Data entity.
    /// </summary>
    public class CoreDataEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<CoreDataAttribute> Attributes { get; set; } = new List<CoreDataAttribute>();
        public List<CoreDataRelationship> Relationships { get; set; } = new List<CoreDataRelationship>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Core Data attribute.
    /// </summary>
    public class CoreDataAttribute
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool IsOptional { get; set; }
        public object DefaultValue { get; set; } = new object();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Core Data relationship.
    /// </summary>
    public class CoreDataRelationship
    {
        public string Name { get; set; } = string.Empty;
        public string DestinationEntity { get; set; } = string.Empty;
        public string RelationshipType { get; set; } = string.Empty;
        public bool IsOptional { get; set; }
        public bool IsToMany { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}
