# Nexo Policy Pack v0.1

This directory contains data-driven safety and quality policies for the Nexo codebase.

## Quick Start

```bash
# Validate policy files
nexo policy validate --schema policies/schemas/policy.safety.schema.json policies/safety/default.yaml
nexo policy validate --schema policies/schemas/policy.quality.schema.json policies/quality/default.yaml

# Run safety scan
nexo safety scan --policy policies/safety/default.yaml --out test-reports/policies

# Run quality checks
nexo quality run --policy policies/quality/default.yaml --out test-reports/policies --format sarif

# Apply complete policy pack
nexo policy apply --manifest policies/policy-pack.manifest.yaml --out test-reports/policies
```

## File Structure

```
policies/
├── policy-pack.manifest.yaml    # Central manifest and version info
├── safety/
│   ├── default.yaml            # Safety rules (filesystem, network, secrets)
│   └── allowlists.yaml         # Network allow/deny lists
├── quality/
│   ├── default.yaml            # Quality gates (compile, tests, style)
│   └── csharp-baseline.yaml    # C#-specific rules
├── schemas/
│   ├── policy.safety.schema.json   # Safety policy validation
│   └── policy.quality.schema.json  # Quality policy validation
└── overrides/
    └── local.example.yaml      # Local customization template
```

## Policy Types

### Safety Policies
- **Filesystem**: Restrict file access to workspace boundaries
- **Process**: Control executable permissions
- **Network**: Allowlist domains and ports (80, 443)
- **Secrets**: Detect API keys, AWS credentials, high-entropy strings
- **Environment**: Control environment variable access

### Quality Policies
- **Compilation**: Must succeed with zero errors
- **Tests**: 75% minimum coverage using coverlet
- **Style**: Max 50 warnings, analyzers as errors
- **Complexity**: Max 12 average cyclomatic complexity
- **Dependencies**: Audit for vulnerabilities, no prerelease packages

## Customization

### Local Overrides
Create `policies/overrides/local.yaml` (gitignored) to customize rules:

```yaml
gates:
  tests:
    min_coverage: 0.70   # Lower locally
style:
  max_warnings: 200
sandbox:
  network:
    default: allow       # More permissive locally
```

### Adding New Rules
1. Add rule to appropriate policy file (`safety/default.yaml` or `quality/default.yaml`)
2. Follow the schema pattern: `id`, `kind`, `action`, `description`
3. Test with `nexo policy validate`
4. Update allowlists if needed

## CI Integration

The policy pack runs automatically on every PR via GitHub Actions:
- Validates policy file structure
- Runs safety and quality checks
- Uploads SARIF to Code Scanning
- Comments PR with results
- Fails job on policy violations

## Schema Validation

All policy files are validated against JSON schemas:
- `policy.safety.schema.json` - Validates safety rules
- `policy.quality.schema.json` - Validates quality gates

Use `nexo policy validate` to check files before committing.

## Troubleshooting

**Policy validation fails:**
- Check JSON schema compliance
- Ensure rule IDs match pattern `^[a-z0-9-]+$`
- Verify required fields are present

**Coverage collection fails:**
- Ensure coverlet is installed: `dotnet add package coverlet.collector`
- Run tests with coverage: `dotnet test --collect:"XPlat Code Coverage"`
- Check coverage file format matches policy setting

**Network rules too restrictive:**
- Add domains to `policies/safety/allowlists.yaml`
- Use local override to disable network restrictions
- Check wildcard patterns are properly anchored

## Version History

- **v0.1.0**: Initial release with safety and quality policies
- Enhanced schemas with strict validation
- Added wildcard domain support
- Integrated CI/CD with SARIF upload
