# Agent Foundry Demo

The Agent Foundry demo showcases Nexo's embeddable AI agent system that can plan tasks, use tools, and dynamically grow its toolbelt at runtime.

## Overview

The Agent Foundry system consists of:

- **Atlas Agent**: The core AI agent that plans and executes tasks
- **Tool System**: Pluggable tools that the agent can use
- **Dynamic Tool Creation**: Ability to create new tools at runtime using the Feature Factory pipeline
- **Policy Enforcement**: SAST/SCA/license/quality gates for tool validation
- **Hot Loading**: Tools can be loaded/unloaded without restarting the application
- **Observability**: OpenTelemetry traces and metrics for monitoring

## Running the Demo

### Prerequisites

- .NET 8.0 SDK
- Nexo solution built successfully

### Quick Start

1. Build the solution:
   ```bash
   dotnet build Nexo.sln -c Release -warnaserror
   ```

2. Run the demo:
   ```bash
   dotnet run --project src/Nexo.Agent.Demo
   ```

3. Follow the interactive prompts to explore the system

### Demo Features

#### 1. Task Execution
- Enter a natural language goal (e.g., "Redact PII in customers.csv, write report, and zip outputs")
- The agent will create a plan and execute it step by step
- Watch as tools are invoked and outputs are generated

#### 2. Mode Toggle
- Switch between OFF, HYBRID, and EMBEDDED modes
- See how different modes affect planning and execution
- OFF mode uses rule-based planning (no LLM required)
- HYBRID and EMBEDDED modes can use LLM when available

#### 3. Tool Management
- View all available tools and their capabilities
- See tool manifests with permissions and requirements
- Understand the tool ecosystem

#### 4. Dynamic Tool Creation
- Generate new tools using the Feature Factory pipeline
- Watch as tools are created, validated, and hot-loaded
- See policy enforcement in action

#### 5. Policy Break Demo
- Intentionally break policies to see self-healing in action
- Watch as the system detects violations, repairs code, and re-validates
- Understand the canary deployment process

## Architecture

### Core Components

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Atlas Agent   │────│  Tool Broker    │────│  Tool Registry  │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                       │                       │
         │                       │                       │
         ▼                       ▼                       ▼
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│ Agent Planner   │    │  Built-in Tools │    │ Pipeline Tool   │
│                 │    │                 │    │ Factory         │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

### Tool Lifecycle

1. **Tool Request**: Agent identifies need for new tool
2. **Generation**: Feature Factory pipeline generates tool code
3. **Validation**: Policy gates validate security, quality, licenses
4. **Repair**: If validation fails, repair strategies fix issues
5. **Hot Load**: Tool is loaded into collectible AssemblyLoadContext
6. **Registration**: Tool is registered with the tool registry
7. **Execution**: Agent can now use the new tool

### Built-in Tools

- **File.Read**: Read text or binary files
- **CSV.Query**: Query CSV files with simple operations
- **Report.Write**: Write reports in Markdown format
- **Summarize**: Summarize text content using heuristics or AI

### Generated Tools

- **Archive.Zip**: Create ZIP archives (generated during demo)

## Configuration

### Environment Variables

- `NEXO_AI_MODE`: Set to `off`, `hybrid`, or `embedded`
- `OTEL_EXPORTER_OTLP_ENDPOINT`: OpenTelemetry endpoint for traces
- `OTEL_CONSOLE_DISABLED`: Set to `true` to disable console exporter

### Agent Modes

- **OFF**: Rule-based planning, no LLM calls
- **HYBRID**: Can use LLM when available, falls back to rules
- **EMBEDDED**: Optimized for embedded scenarios

## Observability

The system emits comprehensive telemetry:

### Activity Sources
- `Nexo.Agent`: Agent operations
- `Nexo.Tool`: Tool execution
- `Nexo.Pipeline`: Tool generation pipeline
- `Nexo.PluginHost`: Plugin loading/unloading

### Metrics
- `tool_invocations`: Counter of tool executions
- `tool_failures`: Counter of failed tool executions
- `pipeline_success`: Success rate of tool generation
- `policy_score`: Histogram of policy compliance scores
- `plan_steps`: Histogram of plan step counts

### Traces
- Agent task execution spans
- Tool invocation spans with timing
- Pipeline generation spans
- Policy validation spans

## Example Workflows

### 1. Data Processing Pipeline

```
Goal: "Process customers.csv, redact PII, generate report, and create ZIP"

Plan:
1. Read customers.csv (File.Read)
2. Query and filter data (CSV.Query)
3. Redact PII (custom tool - would be generated)
4. Write report (Report.Write)
5. Create ZIP archive (Archive.Zip - generated)
```

### 2. Tool Generation Workflow

```
1. Agent identifies missing tool (Archive.Zip)
2. Creates ToolRequest with specifications
3. PipelineToolFactory generates C# code
4. Policy gates validate (SAST, SCA, license)
5. If validation fails, repair strategies fix issues
6. Tool is compiled and hot-loaded
7. Tool is registered and available for use
```

## Troubleshooting

### Common Issues

1. **Tool not found**: Check if tool is registered in the registry
2. **Permission denied**: Verify tool has required permissions
3. **Policy validation failed**: Check policy compliance and repair attempts
4. **Assembly load failed**: Ensure tool is properly compiled and signed

### Debugging

Enable detailed logging by setting log level to `Debug`:

```json
{
  "Logging": {
    "LogLevel": {
      "Nexo.Agent": "Debug",
      "Nexo.Tool": "Debug",
      "Nexo.Pipeline": "Debug"
    }
  }
}
```

## Next Steps

- Explore the source code in `src/Nexo.Agent/`
- Add custom tools by implementing `ITool<TInput, TOutput>`
- Extend the planner with custom planning strategies
- Integrate with your own LLM providers
- Add custom policy gates for domain-specific validation
