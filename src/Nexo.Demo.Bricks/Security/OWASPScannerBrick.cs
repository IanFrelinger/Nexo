using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Execution;

namespace Nexo.Demo.Bricks.Security;

/// <summary>
/// OWASP vulnerability scanner brick with both deterministic and agentic implementations.
/// </summary>
public class OWASPScannerBrick : Brick
{
    private readonly IProviderFactory _providerFactory;
    private readonly ILogger<OWASPScannerBrick> _logger;
    
    public OWASPScannerBrick(IProviderFactory providerFactory, ILogger<OWASPScannerBrick> logger)
    {
        _providerFactory = providerFactory;
        _logger = logger;
        
        Id = "owasp-scanner";
        Name = "OWASP Vulnerability Scanner";
        Version = "3.0.0";
        Icon = "🔬";
        Category = BrickCategory.Security;
        Description = "Scans code for OWASP Top 10 and CWE vulnerabilities.";
        
        DomainKnowledge = new DomainKnowledge
        {
            Standards = ["OWASP Top 10 2023", "CWE Top 25 2023", "SANS Top 25"],
            Rules = LoadOWASPRules(),
            ReferenceData = new Dictionary<string, string>
            {
                ["semgrepRules"] = "./rules/semgrep/",
                ["cweDatabase"] = "./data/cwe-database.json",
                ["falsePositives"] = "./data/false-positive-patterns.json"
            },
            LearnedPatterns = LoadLearnedPatterns()
        };
        
        Interface = new BrickInterface
        {
            Inputs = [
                new BrickInputDefinition("code", "string", "Source code to analyze"),
                new BrickInputDefinition("language", "string", "Programming language", required: false),
                new BrickInputDefinition("ruleSet", "string", "Rule set to apply", required: false, defaultValue: "owasp-top-10")
            ],
            Outputs = [
                new BrickOutputDefinition("findings", "SecurityFinding[]", "Detected vulnerabilities"),
                new BrickOutputDefinition("metadata", "ScanMetadata", "Scan statistics")
            ]
        };
        
        Implementations = new BrickImplementations
        {
            Deterministic = new DeterministicImplementation
            {
                Id = "semgrep-engine",
                Name = "Semgrep Rule Engine",
                Description = "Pattern-based scanning using Semgrep rules",
                Executor = "SemgrepExecutor",
                Config = new Dictionary<string, object>
                {
                    ["rulesets"] = new[] { "p/owasp-top-ten", "p/cwe-top-25", "custom" },
                    ["timeout"] = "60s",
                    ["maxMemory"] = "1GB"
                },
                Characteristics = new ImplementationCharacteristics
                {
                    Latency = "< 500ms per 1000 LOC",
                    Deterministic = true,
                    RequiresNetwork = false,
                    ResourceUsage = ResourceUsage.Medium
                }
            },
            Agentic = new AgenticImplementation
            {
                Id = "llm-security-analyzer",
                Name = "LLM Security Analyzer",
                Description = "AI-powered vulnerability detection with context understanding",
                LLMConfig = new LLMConfig
                {
                    Model = "gpt-4",
                    SystemPrompt = """
                        You are a security expert analyzing code for vulnerabilities.
                        Focus on: {standards}
                        Apply these detection patterns: {rules}
                        Consider these known safe patterns: {learnedPatterns}
                        
                        For each finding, report:
                        - Location (file, line)
                        - Vulnerability type
                        - Severity (Critical/High/Medium/Low)
                        - CWE ID if applicable
                        - Brief explanation
                        - Suggested fix
                        
                        Be thorough but avoid false positives.
                        """,
                    Temperature = 0.1,
                    MaxTokens = 4000
                },
                ProviderMappings = new Dictionary<string, ProviderConfig>
                {
                    ["openai"] = new("gpt-4"),
                    ["azure"] = new("gpt-4-turbo"),
                    ["ollama"] = new("codellama:34b"),
                    ["anthropic"] = new("claude-3-opus")
                },
                Characteristics = new ImplementationCharacteristics
                {
                    Latency = "5-15s",
                    Deterministic = false,
                    RequiresNetwork = true,
                    ResourceUsage = ResourceUsage.High
                }
            }
        };
        
        DefaultImplementation = ImplementationType.Deterministic;
        FallbackChain = [ImplementationType.Deterministic, ImplementationType.Agentic];
        
        Selector = new ImplementationSelector
        {
            PreferDeterministic = [
                "environment.airGapped",
                "context.auditMode",
                "input.language in ['csharp', 'java', 'javascript']"
            ],
            PreferAgentic = [
                "input.language in ['solidity', 'move', 'cairo']",
                "context.includeLogicFlaws",
                "context.depth == 'deep'"
            ],
            Default = ImplementationType.Deterministic
        };
        
        Metadata = new BrickMetadata
        {
            Author = "nexo-security-team",
            License = "MIT",
            Repository = "https://github.com/nexo/security-bricks",
            UsageCount = 89234,
            LastUpdated = DateTime.Parse("2024-01-20")
        };
    }
    
    public override async Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        return implementation switch
        {
            ImplementationType.Deterministic => await ExecuteDeterministicAsync(input, context, cancellationToken),
            ImplementationType.Agentic => await ExecuteAgenticAsync(input, context, cancellationToken),
            _ => throw new ArgumentException($"Unsupported implementation: {implementation}")
        };
    }
    
    private async Task<BrickOutput> ExecuteDeterministicAsync(
        BrickInput input,
        IExecutionContext context,
        CancellationToken cancellationToken)
    {
        var code = input.Get<string>("code");
        var language = input.Get<string?>("language");
        var ruleSet = input.Get<string>("ruleSet", "owasp-top-10");
        
        _logger.LogInformation("Executing deterministic OWASP scan for {Language} code", language ?? "unknown");
        
        // Simulate Semgrep execution
        await Task.Delay(100, cancellationToken);
        
        var findings = new List<SecurityFinding>
        {
            new("sqli-1", "SQL Injection", "Critical", "CWE-89", "Line 42: String concatenation in SQL query"),
            new("xss-1", "Cross-Site Scripting", "High", "CWE-79", "Line 15: Unescaped user input in HTML")
        };
        
        // Filter false positives using learned patterns
        findings = FilterFalsePositives(findings, DomainKnowledge.LearnedPatterns);
        
        return new BrickOutput
        {
            ["findings"] = findings,
            ["metadata"] = new ScanMetadata(
                DomainKnowledge.Rules.Count,
                code.Split('\n').Length,
                language ?? "auto-detected"
            ),
            Summary = $"Found {findings.Count} vulnerabilities ({findings.Count(f => f.Severity == "Critical")} critical)"
        };
    }
    
    private async Task<BrickOutput> ExecuteAgenticAsync(
        BrickInput input,
        IExecutionContext context,
        CancellationToken cancellationToken)
    {
        var code = input.Get<string>("code");
        
        // Build prompt with domain knowledge
        var prompt = Implementations.Agentic!.LLMConfig.SystemPrompt
            .Replace("{standards}", string.Join(", ", DomainKnowledge.Standards))
            .Replace("{rules}", FormatRulesForPrompt(DomainKnowledge.Rules.Take(20)))
            .Replace("{learnedPatterns}", FormatPatternsForPrompt(DomainKnowledge.LearnedPatterns.Take(10)));
        
        _logger.LogInformation("Executing agentic OWASP scan with provider {Provider}", context.Provider);
        
        var response = await _providerFactory.ExecuteLLMAsync(
            context.Provider,
            prompt,
            $"Analyze this code:\n\n{code}",
            Implementations.Agentic.LLMConfig,
            cancellationToken);
        
        var findings = ParseLLMFindings(response);
        
        return new BrickOutput
        {
            ["findings"] = findings,
            ["metadata"] = new ScanMetadata(
                DomainKnowledge.Rules.Count,
                code.Split('\n').Length,
                "analyzed-by-llm"
            ),
            Summary = $"Found {findings.Count} vulnerabilities ({findings.Count(f => f.Severity == "Critical")} critical)"
        };
    }
    
    private static IReadOnlyList<DomainRule> LoadOWASPRules() => [
        new DomainRule("sqli-string-concat", "String concatenation in SQL query", pattern: null, severity: "critical", cweId: "CWE-89"),
        new DomainRule("sqli-format-string", "Format string in SQL query", pattern: null, severity: "critical", cweId: "CWE-89"),
        new DomainRule("xss-unescaped-output", "Unescaped user input in HTML", pattern: null, severity: "high", cweId: "CWE-79"),
        new DomainRule("xss-innerhtml", "innerHTML with user data", pattern: null, severity: "high", cweId: "CWE-79"),
        new DomainRule("csrf-no-token", "Form without CSRF token", pattern: null, severity: "medium", cweId: "CWE-352"),
    ];
    
    private static IReadOnlyList<LearnedPattern> LoadLearnedPatterns() => [
        new("Entity Framework parameterized queries", "safe", 0.99, 15234),
        new("React dangerouslySetInnerHTML with sanitize", "safe", 0.95, 8721),
    ];
    
    private static List<SecurityFinding> FilterFalsePositives(
        List<SecurityFinding> findings,
        IReadOnlyList<LearnedPattern> patterns)
    {
        // Simple filtering - in production, use semantic matching
        return findings;
    }
    
    private static string FormatRulesForPrompt(IEnumerable<DomainRule> rules)
    {
        return string.Join("\n", rules.Select(r => $"- {r.Id}: {r.Description}"));
    }
    
    private static string FormatPatternsForPrompt(IEnumerable<LearnedPattern> patterns)
    {
        return string.Join("\n", patterns.Select(p => $"- {p.Pattern} ({p.Confidence:P0} confidence)"));
    }
    
    private static List<SecurityFinding> ParseLLMFindings(string response)
    {
        // Simple parsing - in production, use structured output
        var findings = new List<SecurityFinding>();
        
        if (response.Contains("SQL Injection", StringComparison.OrdinalIgnoreCase))
        {
            findings.Add(new("sqli-llm", "SQL Injection", "Critical", "CWE-89", "Detected by LLM analysis"));
        }
        
        if (response.Contains("XSS", StringComparison.OrdinalIgnoreCase) || 
            response.Contains("Cross-Site Scripting", StringComparison.OrdinalIgnoreCase))
        {
            findings.Add(new("xss-llm", "Cross-Site Scripting", "High", "CWE-79", "Detected by LLM analysis"));
        }
        
        return findings;
    }
}

public record SecurityFinding(
    string Id,
    string Type,
    string Severity,
    string? CweId,
    string Description
);

public record ScanMetadata(
    int RulesApplied,
    int LinesScanned,
    string Language
);

