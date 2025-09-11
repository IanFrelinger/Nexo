using System;
using System.Collections.Generic;
using Nexo.Core.Domain.Enums;

namespace Nexo.Core.Domain.Entities.Domain
{
    /// <summary>
    /// Represents a domain service
    /// </summary>
    public class DomainService
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public List<ServiceMethod> Methods { get; set; } = new List<ServiceMethod>();
        public List<string> Dependencies { get; set; } = new List<string>();
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a method in a domain service
    /// </summary>
    public class ServiceMethod
    {
        public string Name { get; set; } = string.Empty;
        public string ReturnType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<MethodParameter> Parameters { get; set; } = new List<MethodParameter>();
        public string Implementation { get; set; } = string.Empty;
        public MethodVisibility Visibility { get; set; } = MethodVisibility.Public;
        public bool IsAsync { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }
}
