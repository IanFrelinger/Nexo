# Nexo Verification System

This document describes the verification system that validates Nexo's core product claims through automated testing and CI/CD integration.

## Product Claims Verified

### 1. Zero Platform Lock-in
- **Claim**: Swap AI providers/models, run on multiple OSes/runtimes/infra
- **Verification**: Cross-provider parity tests ensure semantic equivalence ≥ 0.85 between local and cloud providers
- **Tests**: `tests/Nexo.Parity.Tests/ParityTests.cs`

### 2. Offline ↔ Online Spectrum
- **OFF**: No AI, deterministic outputs, zero network calls
- **ASSIST**: Dev-time only AI, no network calls
- **HYBRID**: Optional AI with local models
- **EMBEDDED**: AI required with cloud models
- **Verification**: Mode-specific tests validate behavior and network isolation

### 3. Deterministic Outputs
- **Claim**: OFF mode produces byte-for-byte identical outputs
- **Verification**: Hash-based assertions ensure consistent outputs across runs
- **Tests**: `tests/Nexo.Verify.Tests/DeterminismTests.cs`

### 4. Self-Healing Behavior
- **Claim**: Retries, circuit breaker, provider failover, rollback on policy violations
- **Verification**: Tests validate retry logic, failover mechanisms, and circuit breaker behavior
- **Tests**: `tests/Nexo.Verify.Tests/SelfHealingTests.cs`

### 5. Compounding Bricks
- **Claim**: Reusable components with typed I/O contracts and discoverability
- **Verification**: Contract tests validate brick interfaces and behavior
- **Tests**: `tests/Nexo.Verify.Tests/BrickContractTests.cs`

## Running Verification Locally

### Prerequisites
- .NET 8.0 SDK
- Docker (for OFF mode network isolation tests)

### Basic Usage

```bash
# Run all verification tests
nexo verify tests/specs

# Run specific mode
nexo verify tests/specs --mode off

# Run single spec file
nexo verify tests/specs/triage_support.verify.yaml
```

### Mode-Specific Testing

#### OFF Mode (No AI, No Network)
```bash
NEXO_AI_MODE=off nexo verify tests/specs --mode off
```

#### ASSIST Mode (Dev-time AI, No Network)
```bash
NEXO_AI_MODE=assist nexo verify tests/specs --mode assist
```

#### HYBRID Mode (Local AI)
```bash
NEXO_AI_MODE=hybrid NEXO_PROVIDER=local NEXO_MODEL=llama3 nexo verify tests/specs
```

#### EMBEDDED Mode (Cloud AI)
```bash
NEXO_AI_MODE=embedded NEXO_PROVIDER=openai NEXO_MODEL=gpt-4o nexo verify tests/specs
```

### Docker-based Network Isolation

For OFF/ASSIST modes, use Docker to ensure complete network isolation:

```bash
# Build test image
docker build -f Dockerfile.test -t nexo-tests .

# Run with no network access
docker run --rm --network none nexo-tests nexo verify tests/specs --mode off
```

## Verification Spec Format

Verification specs are defined in `.verify.yaml` files:

```yaml
scenario: examples/triage_support.yaml
inputs:
  - tests/fixtures/inbox/*.eml

modes:
  - name: off
    env:
      NEXO_AI_MODE: off
    asserts:
      - type: no_network_egress
      - type: hash_equals
        of: outputs/labels.csv
        expected_sha256: "a1b2c3d4e5f6..."

  - name: hybrid_local
    env:
      NEXO_AI_MODE: hybrid
      NEXO_PROVIDER: local
      NEXO_MODEL: llama3
    asserts:
      - type: completes_within_ms
        limit: 8000
      - type: structure_contains
        file: outputs/labels.csv
        must_have_columns: [id, subject, label, confidence]

  - name: embedded_cloud
    env:
      NEXO_AI_MODE: embedded
      NEXO_PROVIDER: openai
      NEXO_MODEL: gpt-4o
    asserts:
      - type: semantic_equivalence
        against: outputs/labels.csv
        reference: tests/baselines/labels_ref.csv
        metric: jaccard_tokens
        threshold: 0.85
```

## Assertion Types

### `hash_equals`
Verifies file SHA256 hash matches expected value (for deterministic outputs).

### `no_network_egress`
Ensures no outbound network calls were made during execution.

### `completes_within_ms`
Validates execution completed within time limit.

### `structure_contains`
Checks file structure (e.g., CSV columns).

### `semantic_equivalence`
Compares outputs using similarity metrics (Jaccard tokens).

## CI/CD Integration

### GitHub Actions Matrix
- **OS**: Ubuntu, Windows, macOS
- **Modes**: OFF, HYBRID, EMBEDDED
- **Network Isolation**: Docker `--network none` for OFF mode on Ubuntu

### Test Results
- Unit tests: xUnit with FluentAssertions
- Integration tests: Cross-provider parity validation
- Performance tests: Execution time validation
- Security tests: Network isolation verification

## Metrics and Reporting

### Parity Score
- **Target**: ≥ 0.85 semantic equivalence between providers
- **Metric**: Jaccard token similarity
- **Coverage**: Local vs Cloud, Cloud vs Cloud

### Determinism Coverage
- **Target**: 100% for OFF mode
- **Metric**: SHA256 hash consistency
- **Validation**: Multiple runs produce identical outputs

### Offline Coverage
- **Target**: 100% for OFF/ASSIST modes
- **Metric**: Zero network egress attempts
- **Validation**: Network guard + Docker isolation

### OS Matrix Coverage
- **Target**: 100% across Ubuntu/Windows/macOS
- **Validation**: CI matrix execution

## Troubleshooting

### Common Issues

1. **Network calls in OFF mode**
   - Check environment variables
   - Verify network guard installation
   - Use Docker for complete isolation

2. **Hash mismatches**
   - Ensure deterministic inputs
   - Check for timestamp/random data
   - Verify file paths are consistent

3. **Semantic equivalence failures**
   - Adjust similarity threshold
   - Check baseline reference files
   - Verify provider configurations

### Debug Mode

```bash
# Enable verbose logging
NEXO_LOG_LEVEL=Debug nexo verify tests/specs

# Run single assertion
nexo verify tests/specs/triage_support.verify.yaml --mode off
```

## Status Badges

- **Determinism**: ![Determinism](https://img.shields.io/badge/Determinism-100%25-brightgreen)
- **Parity**: ![Parity](https://img.shields.io/badge/Parity-≥85%25-brightgreen)
- **Offline Coverage**: ![Offline](https://img.shields.io/badge/Offline-100%25-brightgreen)
- **OS Matrix**: ![OS Matrix](https://img.shields.io/badge/OS%20Matrix-3/3-brightgreen)

## Related Documentation

- [Parity Report](parity-report.md) - Detailed cross-provider analysis
- [API Reference](../src/Nexo.Verify/README.md) - Verification library documentation
- [CI Configuration](../.github/workflows/verify.yml) - GitHub Actions setup
