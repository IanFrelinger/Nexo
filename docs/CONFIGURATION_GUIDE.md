# Nexo Configuration Guide

Nexo reads configuration from `~/.nexo/config.json`. If the file is missing, a default configuration is created when you run `nexo config`.

## Configuration Model

```json
{
  "analysis": {
    "enabledRules": ["SecurityScan", "CodeQuality"],
    "ruleSettings": {},
    "maxComplexityThreshold": 20,
    "enableSecurityScan": true,
    "enableCodeQuality": true
  },
  "validation": {
    "defaultFilter": null,
    "timeoutSeconds": 300,
    "failOnNoTests": false,
    "testProjectPatterns": ["*Test*.csproj", "*Tests.csproj"]
  },
  "logging": {
    "level": "Information",
    "enableStructuredLogging": true,
    "enableProgressIndicators": true
  }
}
```

## Managing Configuration

### View Current Settings

```bash
nexo config          # human readable
nexo config --json   # raw JSON
```

### Edit Configuration

1. Run `nexo config --json > ~/.nexo/config.json` to export.
2. Edit the JSON file (ensure valid syntax).
3. Next command run automatically reloads settings.

### Key Settings

| Setting | Description |
|---------|-------------|
| `analysis.enabledRules` | Controls which analysis rules run. Add custom rule names once registered in DI. |
| `analysis.maxComplexityThreshold` | Used by `CodeQualityRule` to flag high complexity. |
| `validation.timeoutSeconds` | Aborts validation runs after the specified time. |
| `validation.failOnNoTests` | If `true`, validation fails when no test projects are found. |
| `logging.level` | Standard `Microsoft.Extensions.Logging` levels (`Trace`, `Debug`, `Information`, etc.). |
| `logging.enableStructuredLogging` | Reserved for future structured logging integration. |
| `logging.enableProgressIndicators` | Enables future UX improvements (progress bars, verbose output). |

## Custom Rule Configuration

`analysis.ruleSettings` can store per-rule configuration objects. Example:

```json
"ruleSettings": {
  "CodeQuality": {
    "maxComplexityThreshold": 15,
    "maxFileLength": 500
  }
}
```

Inside your rule implementation, read from the configuration service to apply custom behavior.

## Advanced Scenarios

- **Environment Overrides:** You can symlink or copy different `config.json` files per environment (dev/staging/prod).
- **Team Settings:** Commit a baseline config under `docs/examples/config.json` and instruct developers to copy it to `~/.nexo/config.json`.
- **CI Pipelines:** Provide `~/.nexo/config.json` via build steps for reproducible analysis/validation.

