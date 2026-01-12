# Nexo Quick Start Guide

Get Nexo running and see the dual-implementation system in action in under 5 minutes.

---

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Git
- (Optional) [Ollama](https://ollama.ai) for local LLM support

---

## Step 1: Clone and Build

```bash
git clone https://github.com/IanFrelinger/Nexo.git
cd Nexo
dotnet restore
dotnet build
```

**Expected output:**
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

---

## Step 2: Run the CLI Demo

```bash
# Test an application with Universal Testing Agent
dotnet run --project src/Nexo.CLI -- demo test "https://example.com" "Test the application"

# Or build a feature with Autonomous Development Agent
dotnet run --project src/Nexo.CLI -- demo dev "Add a feature" "./MyProject"
```

The CLI demos showcase:
1. Universal Testing Agent - AI-powered testing for any application
2. Autonomous Development Agent - Build features with mock user testing
3. Beautiful CLI output with progress indicators and color-coded results

---

## Step 3: Try the CLI

```bash
# Install globally
dotnet tool install --global Nexo.CLI

# Verify installation
nexo --version

# Run interactive demo via CLI
nexo demo --interactive

# Analyze the current directory
nexo analyze --path .

# Run with JSON output (for CI/CD)
nexo analyze --format-json
```

---

## Step 4: See the Toggle in Action

### Using the Demo UI

1. Launch the demo: `dotnet run --project src/Nexo.CLI -- demo test "https://example.com" "Test the application"`
2. Find the "OWASP Scanner" brick
3. Click the implementation toggle: ⚙️ ↔ 🤖
4. Run the behavior
5. Observe:
   - ⚙️ Mode: <5ms, uses Semgrep rules
   - 🤖 Mode: ~2s, uses LLM analysis

### Using Code

```csharp
// Get a brick from the registry
var scanner = brickRegistry.Get("owasp-scanner");

// Execute with deterministic implementation
var deterministicResult = await scanner.ExecuteAsync(
    input,
    ImplementationType.Deterministic,
    context);
// Result in <5ms using pattern matching

// Execute with agentic implementation
var agenticResult = await scanner.ExecuteAsync(
    input,
    ImplementationType.Agentic,
    context);
// Result in ~2s using LLM reasoning

// Both produce the same output interface!
Assert.Equal(deterministicResult.Schema, agenticResult.Schema);
```

---

## Step 5: Test Offline Mode

### With Ollama (Recommended)

```bash
# Install Ollama
curl -fsSL https://ollama.ai/install.sh | sh

# Pull a model
ollama pull llama2

# Run Nexo in offline mode
nexo demo --offline
```

### Without Ollama

All 🤖 bricks fall back to ⚙️ deterministic implementations automatically:

```bash
# Force offline mode (no network calls)
nexo demo --offline --no-ollama

# All AI features use deterministic fallbacks
```

---

## Step 6: Create Your First Brick

```bash
# Generate brick scaffold
nexo scaffold brick --name MyAnalyzer --category Analysis

# This creates:
# - src/Nexo.Bricks.Custom/MyAnalyzerBrick.cs
# - src/Nexo.Tests.Bricks/MyAnalyzerBrickTests.cs
```

**Generated brick structure:**

```csharp
public class MyAnalyzerBrick : Brick
{
    public MyAnalyzerBrick()
    {
        Id = "my-analyzer";
        Name = "My Analyzer";
        Category = BrickCategory.Analysis;
        
        DomainKnowledge = new DomainKnowledge
        {
            Standards = ["Your Standard Here"],
            Rules = [
                new DomainRule("rule-1", "Description of rule")
            ]
        };
        
        Implementations = new BrickImplementations
        {
            Deterministic = new DeterministicImplementation
            {
                Id = "rule-based",
                Executor = "PatternMatchExecutor"
            },
            Agentic = new AgenticImplementation
            {
                Id = "llm-powered",
                LLMConfig = new() { Model = "gpt-4" }
            }
        };
    }
    
    public override async Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken ct = default)
    {
        return implementation switch
        {
            ImplementationType.Deterministic => await ExecuteDeterministic(input, ct),
            ImplementationType.Agentic => await ExecuteAgentic(input, context, ct),
            _ => throw new ArgumentException($"Unknown implementation: {implementation}")
        };
    }
    
    private async Task<BrickOutput> ExecuteDeterministic(BrickInput input, CancellationToken ct)
    {
        // Your fast, rule-based logic here
        // No network calls, fully auditable
    }
    
    private async Task<BrickOutput> ExecuteAgentic(
        BrickInput input, 
        IExecutionContext context, 
        CancellationToken ct)
    {
        // Your LLM-powered logic here
        // Uses context.Provider for LLM calls
    }
}
```

---

## Next Steps

| Goal | Guide |
|------|-------|
| Understand the architecture | [Architecture Overview](ARCHITECTURE.md) |
| Deploy to air-gapped environment | [Defense Deployment](DEFENSE_DEPLOYMENT.md) |
| Integrate with Unity | [Unity Integration](UNITY_INTEGRATION.md) |
| Set up CI/CD | [CI/CD Guide](CI_CD_GUIDE.md) |

---

## Troubleshooting

### Build fails with SDK not found

```bash
# Verify .NET 8 is installed
dotnet --list-sdks

# Should show:
# 8.0.xxx [path]
```

### Ollama connection refused

```bash
# Verify Ollama is running
curl http://localhost:11434/api/tags

# If not, start it:
ollama serve
```

### Tests fail

```bash
# Run with verbose output
dotnet test --verbosity detailed

# Run specific test category
dotnet test --filter "Category=Unit"
```

---

## Getting Help

- [GitHub Issues](https://github.com/IanFrelinger/Nexo/issues)
- [Discussions](https://github.com/IanFrelinger/Nexo/discussions)
- [API Reference](API_REFERENCE.md)

