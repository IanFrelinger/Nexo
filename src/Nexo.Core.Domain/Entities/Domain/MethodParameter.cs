using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Entities.Domain
{
    /// <summary>
    /// Represents a method parameter
    /// </summary>
    public class MethodParameter
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsOptional { get; set; }
        public object? DefaultValue { get; set; }
        public List<string> Attributes { get; set; } = new List<string>();
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }
}
