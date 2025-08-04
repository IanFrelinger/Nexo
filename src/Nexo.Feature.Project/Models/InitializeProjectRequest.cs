using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Core.Application.Models
{
    /// <summary>
    /// Represents a request to initialize a new project.
    /// </summary>
    public sealed class InitializeProjectRequest
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string Runtime { get; set; }
        public bool Force { get; set; }
        public List<string> AgentIds { get; set; }
        public Dictionary<string, object> Metadata { get; set; }

        public InitializeProjectRequest(
            string name,
            string path,
            string runtime,
            bool force = false,
            IEnumerable<string> agentIds = null,
            IDictionary<string, object> metadata = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Project name cannot be null or whitespace", nameof(name));
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Project path cannot be null or whitespace", nameof(path));
            if (string.IsNullOrWhiteSpace(runtime))
                throw new ArgumentException("Runtime cannot be null or whitespace", nameof(runtime));

            Name = name;
            Path = path;
            Runtime = runtime;
            Force = force;
            AgentIds = agentIds != null ? new List<string>(agentIds) : new List<string>();
            Metadata = metadata != null ? new Dictionary<string, object>(metadata) : new Dictionary<string, object>();
        }

        public static InitializeProjectRequest Create(string name, string path, string runtime)
        {
            return new InitializeProjectRequest(name, path, runtime);
        }

        public static InitializeProjectRequest CreateWithAgents(
            string name,
            string path,
            string runtime,
            params string[] agentIds)
        {
            return new InitializeProjectRequest(name, path, runtime, agentIds: agentIds);
        }
    }
}