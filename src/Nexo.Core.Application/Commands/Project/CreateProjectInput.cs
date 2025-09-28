using System;
using System.Collections.Generic;
using Nexo.Core.Domain.ValueObjects;

namespace Nexo.Core.Application.Commands.Project
{
    /// <summary>
    /// Input for creating a project
    /// </summary>
    public class CreateProjectInput
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public ContainerRuntime? Runtime { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
        public string? Description { get; set; }
    }
}
