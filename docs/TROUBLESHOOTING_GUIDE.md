# Troubleshooting Guide

This document lists common errors, their error codes, and recommended remediation steps.

## Analysis Errors

| Error Code | Message | Suggested Fix |
|------------|---------|---------------|
| `ANALYSIS_1001` | Path not found | Verify the `--path` exists and is accessible. |
| `ANALYSIS_1002` | Unauthorized access | Run with appropriate permissions or adjust directory ACLs. |
| `ANALYSIS_1004` | Rule execution failed | Check logs for rule-specific messages; validate rule configuration in `config.json`. |

## Validation Errors

| Error Code | Message | Suggested Fix |
|------------|---------|---------------|
| `VALIDATION_2001` | No test projects found | Ensure project names include `Test` or update `validation.testProjectPatterns`. |
| `VALIDATION_2002` | Test execution failed | Review stderr output from `dotnet test` for compilation or runtime errors. |
| `VALIDATION_2003` | TRX parse failed | Delete corrupted TRX files or rerun `nexo validate`. |
| `VALIDATION_2004` | Validation timeout | Increase `validation.timeoutSeconds` or investigate long-running tests. |

## Agent Errors

| Error Code | Message | Suggested Fix |
|------------|---------|---------------|
| `AGENT_3001` | Agent not found | Run `nexo agent list` to confirm the correct name. |
| `AGENT_3002` | Agent execution failed | Inspect CLI logs; ensure prerequisites for the agent are satisfied. |
| `AGENT_3003` | Agent timeout | Retry with smaller workloads or increase timeout in agent configuration. |
| `AGENT_3004` | Invalid input | Provide a valid `--input` file path; some agents require JSON payloads. |

## Configuration Errors

| Error Code | Message | Suggested Fix |
|------------|---------|---------------|
| `CONFIG_4001` | Config file not found | Run `nexo config` to generate defaults. |
| `CONFIG_4002` | Invalid format | Fix JSON syntax or delete `~/.nexo/config.json` to regenerate. |
| `CONFIG_4003` | Invalid value | Verify property names and data types. |

## General Errors

| Error Code | Message | Suggested Fix |
|------------|---------|---------------|
| `GENERAL_5001` | Unexpected error | Rerun with `--json` to capture details and file an issue. |
| `GENERAL_5002` | Invalid argument | Check command syntax in `docs/CLI_REFERENCE.md`. |

## Collecting Diagnostic Data

1. Re-run the failing command with `--json` to capture machine-readable output.
2. Check `logs/nexo.log` (if structured logging enabled).
3. Provide the error code, command invocation, and relevant config entries when filing issues.

