using System.Collections.Generic;

namespace Nexo.Feature.Agent.Models
{
    public class AgentRequest
    {
        public AgentRequestType Type { get; set; }
        public Dictionary<string, object> Context { get; set; } = new Dictionary<string, object>();
        public string Content { get; set; } = string.Empty;
    }
}