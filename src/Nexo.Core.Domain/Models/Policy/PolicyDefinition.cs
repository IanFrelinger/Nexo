using System.Collections.Generic;

namespace Nexo.Core.Domain.Models.Policy
{
    /// <summary>
    /// Represents a complete policy definition loaded from YAML
    /// </summary>
    public class PolicyDefinition
    {
        /// <summary>
        /// Policy metadata
        /// </summary>
        public PolicyMetadata Meta { get; set; } = new PolicyMetadata();

        /// <summary>
        /// Safety policy configuration
        /// </summary>
        public SafetyPolicy? Safety { get; set; }

        /// <summary>
        /// Quality policy configuration
        /// </summary>
        public QualityPolicy? Quality { get; set; }

        /// <summary>
        /// Policy rules
        /// </summary>
        public List<PolicyRule> Rules { get; set; } = new List<PolicyRule>();

        /// <summary>
        /// Output configuration
        /// </summary>
        public PolicyOutputs Outputs { get; set; } = new PolicyOutputs();
    }

    /// <summary>
    /// Policy metadata
    /// </summary>
    public class PolicyMetadata
    {
        public string Id { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> SeverityScale { get; set; } = new List<string>();
        public bool RequiresApproval { get; set; } = false;
    }

    /// <summary>
    /// Safety policy configuration
    /// </summary>
    public class SafetyPolicy
    {
        public SandboxConfiguration Sandbox { get; set; } = new SandboxConfiguration();
        public List<SafetyRule> Rules { get; set; } = new List<SafetyRule>();
    }

    /// <summary>
    /// Quality policy configuration
    /// </summary>
    public class QualityPolicy
    {
        public QualityGates Gates { get; set; } = new QualityGates();
        public QualityScoring Scoring { get; set; } = new QualityScoring();
        public Dictionary<string, LanguageOverride> LanguageOverrides { get; set; } = new Dictionary<string, LanguageOverride>();
    }

    /// <summary>
    /// Sandbox configuration for safety policies
    /// </summary>
    public class SandboxConfiguration
    {
        public FilesystemSandbox Filesystem { get; set; } = new FilesystemSandbox();
        public ProcessSandbox Process { get; set; } = new ProcessSandbox();
        public NetworkSandbox Network { get; set; } = new NetworkSandbox();
        public EnvironmentSandbox Env { get; set; } = new EnvironmentSandbox();
    }

    /// <summary>
    /// Filesystem sandbox configuration
    /// </summary>
    public class FilesystemSandbox
    {
        public List<string> WriteRoots { get; set; } = new List<string>();
        public List<string> ReadOnlyGlobs { get; set; } = new List<string>();
        public List<string> DenyGlobs { get; set; } = new List<string>();
    }

    /// <summary>
    /// Process sandbox configuration
    /// </summary>
    public class ProcessSandbox
    {
        public List<string> AllowExec { get; set; } = new List<string>();
        public List<string> DenyExec { get; set; } = new List<string>();
    }

    /// <summary>
    /// Network sandbox configuration
    /// </summary>
    public class NetworkSandbox
    {
        public string Default { get; set; } = "deny";
        public string? AllowlistRef { get; set; }
        public string? DenylistRef { get; set; }
        public bool AllowLoopback { get; set; } = true;
        public int MaxEgressKbps { get; set; } = 1024;
    }

    /// <summary>
    /// Environment sandbox configuration
    /// </summary>
    public class EnvironmentSandbox
    {
        public List<string> AllowedPrefixes { get; set; } = new List<string>();
        public List<string> Deny { get; set; } = new List<string>();
    }

    /// <summary>
    /// Quality gates configuration
    /// </summary>
    public class QualityGates
    {
        public CompileGate Compile { get; set; } = new CompileGate();
        public TestGate Tests { get; set; } = new TestGate();
        public StyleGate Style { get; set; } = new StyleGate();
        public ComplexityGate Complexity { get; set; } = new ComplexityGate();
        public DependencyGate Dependencies { get; set; } = new DependencyGate();
        public CommitGate Commits { get; set; } = new CommitGate();
    }

    /// <summary>
    /// Compile gate configuration
    /// </summary>
    public class CompileGate
    {
        public bool MustSucceed { get; set; } = true;
    }

    /// <summary>
    /// Test gate configuration
    /// </summary>
    public class TestGate
    {
        public bool MustPass { get; set; } = true;
        public double MinCoverage { get; set; } = 0.75;
    }

    /// <summary>
    /// Style gate configuration
    /// </summary>
    public class StyleGate
    {
        public int MaxWarnings { get; set; } = 50;
        public bool TreatAnalyzersAsErrors { get; set; } = true;
    }

    /// <summary>
    /// Complexity gate configuration
    /// </summary>
    public class ComplexityGate
    {
        public int MaxCyclomaticAverage { get; set; } = 12;
        public int MaxCyclomaticPerMethod { get; set; } = 20;
    }

    /// <summary>
    /// Dependency gate configuration
    /// </summary>
    public class DependencyGate
    {
        public bool DisallowPrerelease { get; set; } = true;
        public DependencyAudit Audit { get; set; } = new DependencyAudit();
    }

    /// <summary>
    /// Dependency audit configuration
    /// </summary>
    public class DependencyAudit
    {
        public string SeverityFailAt { get; set; } = "high";
    }

    /// <summary>
    /// Commit gate configuration
    /// </summary>
    public class CommitGate
    {
        public bool ConventionalCommits { get; set; } = true;
    }

    /// <summary>
    /// Quality scoring configuration
    /// </summary>
    public class QualityScoring
    {
        public Dictionary<string, double> Weights { get; set; } = new Dictionary<string, double>();
        public double PassThreshold { get; set; } = 0.80;
    }

    /// <summary>
    /// Language-specific overrides
    /// </summary>
    public class LanguageOverride
    {
        public string? AnalyzerRuleset { get; set; }
        public RoslynConfiguration? Roslyn { get; set; }
    }

    /// <summary>
    /// Roslyn configuration
    /// </summary>
    public class RoslynConfiguration
    {
        public bool TreatWarningsAsErrors { get; set; } = true;
        public List<string> RequiredDiagnostics { get; set; } = new List<string>();
        public List<string> DisabledDiagnostics { get; set; } = new List<string>();
    }

    /// <summary>
    /// Policy rule
    /// </summary>
    public class PolicyRule
    {
        public string Id { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Kind { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public List<string>? Patterns { get; set; }
        public List<string>? Allow { get; set; }
        public List<string>? Deny { get; set; }
    }

    /// <summary>
    /// Safety rule
    /// </summary>
    public class SafetyRule : PolicyRule
    {
        // Inherits from PolicyRule
    }

    /// <summary>
    /// Policy output configuration
    /// </summary>
    public class PolicyOutputs
    {
        public string ReportDir { get; set; } = "./test-reports";
        public List<string> Formats { get; set; } = new List<string> { "json", "md" };
        public bool Sarif { get; set; } = true;
    }
}
