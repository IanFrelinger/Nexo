# Nexo CLI

AI-Enhanced Development Environment Orchestration Platform CLI

## Installation

### Global Installation
```bash
dotnet tool install --global Nexo.CLI --add-source ./nupkgs
```

### Local Installation (for CI/Development)
```bash
dotnet pack src/Nexo.CLI -c Release -o ./nupkgs
dotnet tool install --tool-path ./tools Nexo.CLI --add-source ./nupkgs
```

## Usage

### Pipeline Run Command

Execute a pipeline with a request file:

```bash
nexo pipeline run --request ./examples/HelloWorld.yaml
```

Execute with dry-run mode:

```bash
nexo pipeline run --request ./examples/HelloWorld.yaml --dry-run
```

Execute with custom output directory:

```bash
nexo pipeline run --request ./examples/HelloWorld.yaml --out ./output
```

Execute with maximum repair attempts:

```bash
nexo pipeline run --request ./examples/HelloWorld.yaml --max-repairs 3
```

Execute with stdin input:

```bash
cat ./examples/HelloWorld.yaml | nexo pipeline run --stdin
```

## Commands

### Orchestration

#### `nexo orchestrate <request>`
Orchestrate agent execution for a request.

```bash
nexo orchestrate "build extraction shooter"
```

### Escalation Management

#### `nexo escalate list`
List all pending escalations.

```bash
nexo escalate list
nexo escalate list --format-json
nexo escalate list --verbose
```

#### `nexo escalate show <id>`
Show details for a specific escalation.

```bash
nexo escalate show <escalation-id>
nexo escalate show <escalation-id> --verbose
```

#### `nexo escalate resolve <id> [--resolution <text>]`
Resolve an escalation with an optional resolution description.

```bash
nexo escalate resolve <escalation-id>
nexo escalate resolve <escalation-id> --resolution "Conflict resolved by merging schemas"
```

#### `nexo escalate dismiss <id> [--reason <text>]`
Dismiss an escalation without resolution.

```bash
nexo escalate dismiss <escalation-id>
nexo escalate dismiss <escalation-id> --reason "False positive"
```

#### `nexo escalate list-by-severity <severity>`
List escalations filtered by severity (Low, Medium, High, Critical).

```bash
nexo escalate list-by-severity Critical
nexo escalate list-by-severity High --format-json
```

## Features

- AI-enhanced development environment orchestration
- Pipeline execution with repair loops
- Dry-run mode for validation
- Human-friendly and machine-readable reporting
- Support for JSON and YAML request formats
- Comprehensive error handling and recovery
- Escalation management for conflict resolution
- Real-time conflict detection and reporting

## License

MIT