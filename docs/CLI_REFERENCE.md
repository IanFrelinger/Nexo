# Nexo CLI Reference

The Nexo CLI (`nexo`) is built on top of `System.CommandLine` and MediatR handlers. All commands support the `--json` option for machine-friendly output.

## Global Options

| Option | Description |
|--------|-------------|
| `--json` | Outputs JSON envelopes (`ok`, `data`, `error`). |
| `--verbose` | Enables correlation IDs + progress indicators for long operations. |

## Commands

### `nexo analyze`

- **Description:** Runs the configured analysis rules against a path (default: current directory).
- **Options:**
  - `--path <Directory>`: Root folder to scan (default `.`).
  - `--json`
  - `--verbose`
- **Exit Codes:**
  - `0` – No violations.
  - `2` – Violations found or validation failure.
  - `3` – Policy violation (e.g., unauthorized access).
  - `99` – Unexpected error.

### `nexo validate`

- **Description:** Runs validation/test projects discovered under the workspace.
- **Options:**
  - `--filter <Trait>`: Optional filter passed to `dotnet test`.
  - `--json`
  - `--verbose`
- **Behavior:** Discovers test projects automatically, runs `dotnet test --logger trx`, parses TRX files, and aggregates results. Caching prevents repeated runs when inputs do not change.

### `nexo agent`

- **Description:** Runs a registered agent (e.g., `director`, `dev-director`).
- **Options:**
  - `--name <AgentName>` (required)
  - `--input <File>`: Optional input payload for the agent.
  - `--json`
  - `--verbose`
- **Subcommands:**
  - `nexo agent list` — Lists available agents with metadata.

### `nexo config`

- **Description:** Displays the current configuration (or outputs JSON).
- **Options:** `--json`, `--verbose`
- **Configuration File:** `~/.nexo/config.json` (created automatically). Provides analysis rule settings, validation defaults, and logging preferences.

## Exit Codes

| Code | Meaning |
|------|---------|
| `0` | Success |
| `2` | Validation failed / violations |
| `3` | Policy violation |
| `99` | Unexpected error |

## JSON Output Envelope

Every command that uses `--json` prints an envelope:

```json
{
  "ok": true,
  "data": { ... command-specific payload ... },
  "error": null
}
```

On error, `ok` is `false`, `data` is `null`, and `error` contains the message and error code.

## Error Codes & Suggestions

| Code | Scenario | Suggested Action |
|------|----------|------------------|
| `ANALYSIS_1002` | Unauthorized access | Ensure the CLI has read permissions; try running as administrator. |
| `VALIDATION_2001` | No test projects found | Verify `*.csproj` naming conventions include "Test" or configure `TestProjectPatterns`. |
| `AGENT_3001` | Agent not found | Run `nexo agent list` to see available agents. |
| `CONFIG_4002` | Invalid config file | Fix JSON syntax or delete `~/.nexo/config.json` to regenerate defaults. |

See `docs/TROUBLESHOOTING_GUIDE.md` for additional details.

