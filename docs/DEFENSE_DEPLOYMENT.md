# Nexo Defense Deployment Guide

This guide covers deploying Nexo in classified, air-gapped, and compliance-heavy environments.

---

## Overview

Nexo is designed from the ground up for defense and regulated industry deployment:

| Requirement | Nexo Solution |
|-------------|---------------|
| Air-gap operation | Ollama/LocalAI for fully offline LLM |
| Audit compliance | Every operation logged with correlation IDs |
| Deterministic behavior | ⚙️ mode for all bricks, no AI required |
| Vendor independence | Swap providers without code changes |
| SCIF deployment | Zero network dependencies possible |

---

## Deployment Modes

### Mode 1: Fully Offline (SCIF/Air-Gap)

```
┌─────────────────────────────────────────────────────────────┐
│                     AIR-GAPPED NETWORK                       │
│                                                             │
│  ┌─────────────┐     ┌─────────────┐     ┌─────────────┐   │
│  │   Nexo      │ ──→ │   Ollama    │ ──→ │  Local LLM  │   │
│  │   Runtime   │     │   Server    │     │  (Llama 2)  │   │
│  └─────────────┘     └─────────────┘     └─────────────┘   │
│         │                                                   │
│         ▼                                                   │
│  ┌─────────────┐                                           │
│  │  ⚙️ Fallback │  ← All AI bricks have deterministic      │
│  │   Mode      │    implementations that work offline      │
│  └─────────────┘                                           │
│                                                             │
│  Network calls: ZERO                                        │
│  External dependencies: ZERO                                │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

**Configuration:**

```json
{
  "nexo": {
    "environment": "air-gapped",
    "providers": {
      "primary": "ollama",
      "ollama": {
        "endpoint": "http://localhost:11434",
        "model": "llama2:13b"
      }
    },
    "fallback": {
      "enabled": true,
      "mode": "deterministic-only"
    },
    "network": {
      "allowExternalCalls": false
    }
  }
}
```

**Deployment steps:**

```bash
# 1. Package Nexo with all dependencies
nexo package --self-contained --runtime linux-x64 --output ./deploy

# 2. Package Ollama and model (on connected system)
ollama pull llama2:13b
tar -czf ollama-package.tar.gz ~/.ollama

# 3. Transfer to air-gapped system via approved media
# 4. Install on air-gapped system
tar -xzf ollama-package.tar.gz -C ~/
./deploy/nexo --verify-offline
```

### Mode 2: Hybrid (Development → Production)

```
┌─────────────────────────────────────────────────────────────┐
│                    DEVELOPMENT (Cloud)                      │
│  ┌─────────────┐     ┌─────────────┐                       │
│  │   Nexo      │ ──→ │  OpenAI /   │  Full AI capabilities │
│  │   Runtime   │     │  Azure      │  for development      │
│  └─────────────┘     └─────────────┘                       │
└─────────────────────────────────────────────────────────────┘
                           │
                           │ Same code, same tests
                           ▼
┌─────────────────────────────────────────────────────────────┐
│                   PRODUCTION (Air-Gap)                      │
│  ┌─────────────┐     ┌─────────────┐                       │
│  │   Nexo      │ ──→ │   Ollama    │  Local AI or          │
│  │   Runtime   │     │   or ⚙️     │  deterministic        │
│  └─────────────┘     └─────────────┘                       │
└─────────────────────────────────────────────────────────────┘
```

**This is Nexo's primary value proposition:**
- Develop with full cloud AI capabilities
- Deploy to classified with zero code changes
- Same interface, same tests, different provider

### Mode 3: Deterministic Only (Maximum Auditability)

For environments where AI is not approved:

```bash
# Force all bricks to deterministic mode
nexo run --implementation-mode deterministic-only

# Or via configuration
{
  "nexo": {
    "implementation": {
      "mode": "deterministic-only",
      "allowAgenticOverride": false
    }
  }
}
```

**Result:** Every brick uses ⚙️ implementation. Zero AI calls. Fully auditable.

---

## Compliance Features

### Audit Logging

Every operation generates audit events:

```json
{
  "correlationId": "550e8400-e29b-41d4-a716-446655440000",
  "timestamp": "2025-01-08T15:30:00Z",
  "operation": "BrickExecution",
  "brick": "owasp-scanner",
  "implementation": "deterministic",
  "input": {
    "hash": "sha256:abc123...",
    "size": 1024
  },
  "output": {
    "hash": "sha256:def456...",
    "size": 512
  },
  "duration": "00:00:00.045",
  "user": "analyst@example.com",
  "classification": "UNCLASSIFIED"
}
```

### Correlation IDs

All operations within a workflow share a correlation ID for tracing:

```csharp
// Start a traced operation
using var scope = tracer.StartSpan("SecurityAnalysis", correlationId);

// All subsequent operations include this ID
await behavior.ExecuteAsync(input, context);

// Query all events for this operation
var events = auditLog.GetByCorrelationId(correlationId);
```

### Data Classification Support

```csharp
public class ClassifiedExecutionContext : IExecutionContext
{
    public Classification DataClassification { get; init; }
    
    // Prevents data from flowing to unauthorized providers
    public bool CanUseProvider(string provider)
    {
        return DataClassification switch
        {
            Classification.TopSecret => provider == "ollama-local",
            Classification.Secret => provider is "ollama-local" or "azure-gov",
            Classification.Unclassified => true,
            _ => false
        };
    }
}
```

---

## Security Hardening

### Network Isolation

```csharp
// Configuration enforces network restrictions
public class NetworkPolicy
{
    public bool AllowExternalCalls { get; init; } = false;
    public IReadOnlyList<string> AllowedEndpoints { get; init; } = [];
    
    public void Validate(string endpoint)
    {
        if (!AllowExternalCalls)
            throw new NetworkPolicyViolation("External calls disabled");
        
        if (!AllowedEndpoints.Contains(endpoint))
            throw new NetworkPolicyViolation($"Endpoint not allowed: {endpoint}");
    }
}
```

### Input Validation

All brick inputs are validated before execution:

```csharp
public class SecurityScannerBrick : Brick
{
    public override async Task<BrickOutput> ExecuteAsync(...)
    {
        // Validate input before processing
        Validator.ValidateInput(input, new InputPolicy
        {
            MaxSize = 10_000_000,  // 10MB max
            AllowedTypes = ["text/plain", "application/json"],
            SanitizeHtml = true,
            BlockExecutables = true
        });
        
        // Process validated input
        return await ProcessAsync(input);
    }
}
```

### Binary Protection

Nexo prevents writing to sensitive file types:

```csharp
// Enforced by policy
public class BinaryProtectionPolicy
{
    public IReadOnlyList<string> ProtectedExtensions { get; } = 
        [".dll", ".exe", ".so", ".dylib", ".sys"];
    
    public void ValidateWrite(string path)
    {
        var ext = Path.GetExtension(path);
        if (ProtectedExtensions.Contains(ext))
            throw new PolicyViolation($"Cannot write to binary: {path}");
    }
}
```

---

## Compliance Pathway

### SOC 2 Readiness

| Control | Nexo Feature |
|---------|--------------|
| CC6.1 - Logical Access | Role-based execution contexts |
| CC6.6 - System Boundaries | Network policy enforcement |
| CC7.1 - Change Management | Audit logging, correlation IDs |
| CC7.2 - System Monitoring | Metrics, health checks, tracing |

### FedRAMP Considerations

| Requirement | Implementation |
|-------------|----------------|
| AC-2 Account Management | Execution context with user identity |
| AU-2 Audit Events | Comprehensive audit logging |
| AU-3 Audit Content | Correlation IDs, timestamps, classifications |
| SC-7 Boundary Protection | Network policy, endpoint allowlists |
| SI-4 System Monitoring | Health checks, metrics, alerting |

---

## Deployment Checklist

### Pre-Deployment

- [ ] All bricks have deterministic implementations
- [ ] Network policy configured for target environment
- [ ] Audit logging enabled and tested
- [ ] Ollama/local LLM tested (if using AI features)
- [ ] Classification handling verified
- [ ] Binary protection policies active

### Deployment

- [ ] Self-contained package created
- [ ] Dependencies bundled (no network required)
- [ ] Configuration for target environment applied
- [ ] Verification tests pass in isolated environment

### Post-Deployment

- [ ] Audit logs flowing to SIEM
- [ ] Health checks returning healthy
- [ ] Metrics collection verified
- [ ] Incident response procedures documented

---

## Getting Help

For defense-specific deployment questions:
- [GitHub Issues](https://github.com/IanFrelinger/Nexo/issues) (use `defense` label)
- [Security Policy](../SECURITY.md)

