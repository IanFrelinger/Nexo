using System;

namespace StandaloneTestRunner.Models
{
    public class TestOptions
    {
        public bool Discover { get; set; }
        public bool ForceTimeout { get; set; }
        public bool Progress { get; set; }
        public bool Verbose { get; set; }
        public bool Coverage { get; set; }
        public bool SmokeTests { get; set; }
        public bool Epic54Tests { get; set; }
        public bool Epic54Category { get; set; }
        public bool Epic54Priority { get; set; }
        public bool Epic54Enhanced { get; set; }
        public bool Epic54Phase { get; set; }
        public bool FeatureFactoryDomain { get; set; }
        public bool FeatureFactoryApplication { get; set; }
        public bool FeatureFactoryDeployment { get; set; }
        public bool AIServices { get; set; }
        public bool CoreDomainEntities { get; set; }
        public bool ShowHelp { get; set; }
        public int Timeout { get; set; } = 5;
        public int HeartbeatInterval { get; set; } = 2;
        public int ProcessTimeout { get; set; } = 1;
        public string? Category { get; set; }
        public string? Priority { get; set; }
    }
}
