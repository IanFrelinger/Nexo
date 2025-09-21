# Nexo CLI

AI-Enhanced Development Environment Orchestration Platform CLI

## Installation

```bash
dotnet tool install --global Nexo.CLI --add-source ./nupkgs
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

## Features

- AI-enhanced development environment orchestration
- Pipeline execution with repair loops
- Dry-run mode for validation
- Human-friendly and machine-readable reporting
- Support for JSON and YAML request formats
- Comprehensive error handling and recovery

## License

MIT