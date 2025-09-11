using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Entities.FeatureFactory
{
    public class ApplicationService
    {
        public string Name { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public List<string> Methods { get; set; } = new();
        public List<string> Dependencies { get; set; } = new();
        public string Interface { get; set; } = string.Empty;
        public string InterfaceName { get; set; } = string.Empty;
        public Dictionary<string, object> Attributes { get; set; } = new();
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public List<ServiceMethod> ServiceMethods { get; set; } = new();
    }

    public class ServiceMethod
    {
        public string Name { get; set; } = string.Empty;
        public string ReturnType { get; set; } = string.Empty;
        public List<string> Parameters { get; set; } = new();
        public string Description { get; set; } = string.Empty;
    }
}
