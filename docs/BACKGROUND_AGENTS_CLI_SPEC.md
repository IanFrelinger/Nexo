# Background Agents CLI Specification

## Overview

CLI commands for configuring and managing background agents, data sensitivity levels, RAG knowledge bases, and web search providers. All commands follow the framework's dog-fooding principle - they use the framework's own orchestration and agent infrastructure.

## Command Structure

```
nexo background-agent <subcommand> [options]
```

## Commands

### Agent Management

#### `nexo background-agent list`

List all configured background agents.

**Options:**
- `--format-json` - Output as JSON
- `--status` - Filter by status (running, stopped, error)
- `--role` - Filter by role
- `--sensitivity` - Filter by max sensitivity level

**Examples:**
```bash
nexo background-agent list
nexo background-agent list --format-json
nexo background-agent list --status running --role monitor
```

**Output:**
```
Background Agents:
  health-monitor (monitor) - Running - Max Sensitivity: Internal
  code-analyzer (analyzer) - Running - Max Sensitivity: Confidential
  security-auditor (auditor) - Stopped - Max Sensitivity: Secret
```

#### `nexo background-agent show <id>`

Show detailed information about a specific agent.

**Options:**
- `--format-json` - Output as JSON

**Examples:**
```bash
nexo background-agent show health-monitor
nexo background-agent show code-analyzer --format-json
```

**Output:**
```
Agent: health-monitor
  Name: Health Monitor Agent
  Role: monitor
  Status: Running
  Model Provider: deterministic
  Max Data Sensitivity: Internal
  Schedule: Interval (00:05:00)
  Commands: check-health, report-metrics
  RAG: Disabled
  Web Search: Disabled
  Exfiltration Policy:
    - Block External LLMs: false
    - Block Web Search: true
    - Block Network Exports: false
    - Max Allowed Level: Internal
```

#### `nexo background-agent add`

Add a new background agent interactively or via JSON file.

**Options:**
- `--config <file>` - Load agent config from JSON file
- `--interactive` - Interactive mode (prompts for all fields)
- `--id <id>` - Agent ID (non-interactive)
- `--name <name>` - Agent name (non-interactive)
- `--role <role>` - Agent role (non-interactive)
- `--model-provider <provider>` - Model provider (non-interactive)
- `--model-name <name>` - Model name (non-interactive)
- `--sensitivity <level>` - Max data sensitivity level (non-interactive)
- `--schedule-type <type>` - Schedule type: continuous, interval, cron (non-interactive)
- `--schedule-interval <timespan>` - Interval for interval schedule (e.g., "00:05:00")
- `--schedule-cron <expression>` - Cron expression for cron schedule
- `--enable-rag` - Enable RAG for this agent
- `--enable-web-search` - Enable web search for this agent

**Examples:**
```bash
# Interactive mode
nexo background-agent add --interactive

# From config file
nexo background-agent add --config ./my-agent.json

# Command-line arguments
nexo background-agent add \
  --id my-agent \
  --name "My Agent" \
  --role analyzer \
  --model-provider openai \
  --model-name gpt-4 \
  --sensitivity Confidential \
  --schedule-type interval \
  --schedule-interval "01:00:00" \
  --enable-rag \
  --enable-web-search
```

#### `nexo background-agent update <id>`

Update an existing background agent.

**Options:**
- `--config <file>` - Load updates from JSON file
- `--name <name>` - Update agent name
- `--role <role>` - Update agent role
- `--model-provider <provider>` - Update model provider
- `--model-name <name>` - Update model name
- `--sensitivity <level>` - Update max sensitivity level
- `--enable` - Enable the agent
- `--disable` - Disable the agent
- `--add-command <command>` - Add a command
- `--remove-command <command>` - Remove a command

**Examples:**
```bash
nexo background-agent update health-monitor --sensitivity Confidential
nexo background-agent update code-analyzer --disable
nexo background-agent update my-agent --add-command new-command
```

#### `nexo background-agent remove <id>`

Remove a background agent.

**Options:**
- `--force` - Force removal without confirmation

**Examples:**
```bash
nexo background-agent remove old-agent
nexo background-agent remove old-agent --force
```

#### `nexo background-agent start <id>`

Start a stopped background agent.

**Examples:**
```bash
nexo background-agent start health-monitor
```

#### `nexo background-agent stop <id>`

Stop a running background agent.

**Options:**
- `--force` - Force stop immediately

**Examples:**
```bash
nexo background-agent stop health-monitor
nexo background-agent stop health-monitor --force
```

#### `nexo background-agent restart <id>`

Restart a background agent.

**Examples:**
```bash
nexo background-agent restart health-monitor
```

### Data Sensitivity Level Management

#### `nexo background-agent sensitivity list`

List all data sensitivity levels (primitives + custom).

**Options:**
- `--format-json` - Output as JSON
- `--custom-only` - Show only custom levels
- `--primitives-only` - Show only primitive levels

**Examples:**
```bash
nexo background-agent sensitivity list
nexo background-agent sensitivity list --format-json
nexo background-agent sensitivity list --custom-only
```

**Output:**
```
Data Sensitivity Levels:

Primitives:
  Public (0) - Allows External LLM, Web Search, Network Exports
  Internal (1) - Allows External LLM, Web Search, Network Exports
  Confidential (2) - Blocks External LLM, Allows Web Search
  Secret (3) - Blocks External LLM, Web Search
  TopSecret (4) - Blocks All, Requires Local Only

Custom:
  CustomerData (2) - Blocks External LLM, Web Search
  PII (3) - Blocks External LLM, Web Search, Network Exports
```

#### `nexo background-agent sensitivity show <name>`

Show details about a specific sensitivity level.

**Options:**
- `--format-json` - Output as JSON

**Examples:**
```bash
nexo background-agent sensitivity show Confidential
nexo background-agent sensitivity show CustomerData --format-json
```

#### `nexo background-agent sensitivity add`

Add a custom sensitivity level.

**Options:**
- `--config <file>` - Load from JSON file
- `--name <name>` - Level name (non-interactive)
- `--value <int>` - Sensitivity value (non-interactive)
- `--allows-external-llm` - Allow external LLM calls
- `--blocks-external-llm` - Block external LLM calls
- `--allows-web-search` - Allow web search
- `--blocks-web-search` - Block web search
- `--requires-local-only` - Require local-only processing
- `--allows-network-exports` - Allow network exports
- `--blocks-network-exports` - Block network exports
- `--description <text>` - Description

**Examples:**
```bash
# Interactive mode
nexo background-agent sensitivity add --interactive

# Command-line arguments
nexo background-agent sensitivity add \
  --name CustomerData \
  --value 2 \
  --blocks-external-llm \
  --blocks-web-search \
  --allows-network-exports \
  --description "Customer-specific data, GDPR protected"

# From config file
nexo background-agent sensitivity add --config ./custom-levels.json
```

#### `nexo background-agent sensitivity update <name>`

Update a custom sensitivity level.

**Options:**
- `--value <int>` - Update sensitivity value
- `--allows-external-llm` - Allow external LLM calls
- `--blocks-external-llm` - Block external LLM calls
- `--allows-web-search` - Allow web search
- `--blocks-web-search` - Block web search
- `--requires-local-only` - Require local-only processing
- `--allows-network-exports` - Allow network exports
- `--blocks-network-exports` - Block network exports
- `--description <text>` - Update description

**Examples:**
```bash
nexo background-agent sensitivity update CustomerData --value 3
nexo background-agent sensitivity update CustomerData --blocks-network-exports
```

#### `nexo background-agent sensitivity remove <name>`

Remove a custom sensitivity level.

**Options:**
- `--force` - Force removal even if in use

**Examples:**
```bash
nexo background-agent sensitivity remove CustomerData
```

### RAG (Knowledge Base) Management

#### `nexo background-agent rag index`

Index a directory or file into the knowledge base.

**Options:**
- `--agent <id>` - Agent ID (uses agent's RAG config)
- `--source <path>` - Source directory or file to index
- `--sensitivity <level>` - Sensitivity level for indexed content
- `--vector-store <provider>` - Vector store provider (overrides agent config)
- `--vector-store-path <path>` - Vector store path (overrides agent config)
- `--chunk-size <int>` - Chunk size for text splitting (default: 1000)
- `--overwrite` - Overwrite existing index

**Examples:**
```bash
# Index using agent's RAG config
nexo background-agent rag index --agent code-analyzer --source ./docs

# Index with custom settings
nexo background-agent rag index \
  --source ./src \
  --sensitivity Internal \
  --vector-store sqlite \
  --vector-store-path ./data/kb.db

# Index multiple sources
nexo background-agent rag index --agent code-analyzer \
  --source ./docs \
  --source ./src \
  --sensitivity Internal
```

#### `nexo background-agent rag search`

Search the knowledge base.

**Options:**
- `--agent <id>` - Agent ID (uses agent's RAG config)
- `--query <text>` - Search query
- `--max-results <int>` - Maximum results (default: 5)
- `--min-score <float>` - Minimum similarity score (0.0-1.0, default: 0.7)
- `--sensitivity <level>` - Max sensitivity level to search
- `--vector-store <provider>` - Vector store provider (overrides agent config)
- `--vector-store-path <path>` - Vector store path (overrides agent config)
- `--format-json` - Output as JSON

**Examples:**
```bash
nexo background-agent rag search --agent code-analyzer --query "how to create agents"
nexo background-agent rag search --query "background agents" --max-results 10 --format-json
```

#### `nexo background-agent rag stats`

Show statistics about the knowledge base.

**Options:**
- `--agent <id>` - Agent ID (uses agent's RAG config)
- `--vector-store <provider>` - Vector store provider (overrides agent config)
- `--vector-store-path <path>` - Vector store path (overrides agent config)
- `--format-json` - Output as JSON

**Examples:**
```bash
nexo background-agent rag stats --agent code-analyzer
nexo background-agent rag stats --vector-store sqlite --vector-store-path ./data/kb.db
```

**Output:**
```
Knowledge Base Statistics:
  Total Documents: 1,234
  Total Chunks: 5,678
  Vector Store: SQLite (./data/rag-store.db)
  Sensitivity Distribution:
    Public: 500 chunks
    Internal: 3,000 chunks
    Confidential: 2,178 chunks
```

#### `nexo background-agent rag clear`

Clear the knowledge base.

**Options:**
- `--agent <id>` - Agent ID (uses agent's RAG config)
- `--vector-store <provider>` - Vector store provider (overrides agent config)
- `--vector-store-path <path>` - Vector store path (overrides agent config)
- `--sensitivity <level>` - Clear only content at or below this sensitivity level
- `--force` - Force clear without confirmation

**Examples:**
```bash
nexo background-agent rag clear --agent code-analyzer --force
nexo background-agent rag clear --sensitivity Internal
```

### Web Search Configuration

#### `nexo background-agent web-search configure`

Configure web search for an agent.

**Options:**
- `--agent <id>` - Agent ID
- `--provider <name>` - Search provider (bing, google, duckduckgo, serpapi)
- `--api-key <key>` - API key for provider
- `--max-results <int>` - Maximum results (default: 10)
- `--filter-sensitive` - Enable sensitive content filtering
- `--allow-domain <domain>` - Add domain to allowlist
- `--block-domain <domain>` - Add domain to blocklist
- `--clear-allowlist` - Clear domain allowlist
- `--clear-blocklist` - Clear domain blocklist

**Examples:**
```bash
nexo background-agent web-search configure \
  --agent code-analyzer \
  --provider bing \
  --api-key "${BING_API_KEY}" \
  --max-results 10 \
  --filter-sensitive \
  --allow-domain github.com \
  --allow-domain stackoverflow.com
```

#### `nexo background-agent web-search test`

Test web search configuration.

**Options:**
- `--agent <id>` - Agent ID
- `--query <text>` - Test query
- `--max-results <int>` - Maximum results

**Examples:**
```bash
nexo background-agent web-search test --agent code-analyzer --query "background agents"
```

### Configuration Management

#### `nexo background-agent config show`

Show current configuration.

**Options:**
- `--format-json` - Output as JSON
- `--file <path>` - Show config from specific file

**Examples:**
```bash
nexo background-agent config show
nexo background-agent config show --format-json
nexo background-agent config show --file ./custom-config.json
```

#### `nexo background-agent config validate`

Validate configuration file.

**Options:**
- `--file <path>` - Config file to validate (default: appsettings.json)

**Examples:**
```bash
nexo background-agent config validate
nexo background-agent config validate --file ./background-agents.json
```

**Output:**
```
Configuration is valid.

Found:
  - 3 agents
  - 2 custom sensitivity levels
  - 1 RAG configuration
  - 1 web search configuration
```

#### `nexo background-agent config export`

Export current configuration to file.

**Options:**
- `--file <path>` - Output file path (default: background-agents-export.json)
- `--include-secrets` - Include API keys and secrets (default: false)
- `--format <format>` - Output format: json, yaml (default: json)

**Examples:**
```bash
nexo background-agent config export
nexo background-agent config export --file ./my-config.json
nexo background-agent config export --format yaml
```

#### `nexo background-agent config import`

Import configuration from file.

**Options:**
- `--file <path>` - Config file to import
- `--merge` - Merge with existing config (default: replace)
- `--dry-run` - Validate without applying

**Examples:**
```bash
nexo background-agent config import --file ./my-config.json
nexo background-agent config import --file ./my-config.json --merge
nexo background-agent config import --file ./my-config.json --dry-run
```

### Agent Execution & Monitoring

#### `nexo background-agent execute <id>`

Manually trigger agent execution (for testing).

**Options:**
- `--async` - Execute asynchronously (don't wait for completion)
- `--format-json` - Output as JSON

**Examples:**
```bash
nexo background-agent execute health-monitor
nexo background-agent execute code-analyzer --async
```

#### `nexo background-agent logs <id>`

Show agent execution logs.

**Options:**
- `--tail <n>` - Show last N lines (default: 100)
- `--follow` - Follow logs in real-time
- `--level <level>` - Filter by log level (debug, info, warning, error)
- `--since <duration>` - Show logs since duration ago (e.g., "1h", "30m")

**Examples:**
```bash
nexo background-agent logs health-monitor
nexo background-agent logs code-analyzer --tail 50 --follow
nexo background-agent logs security-auditor --level error --since 1h
```

#### `nexo background-agent metrics <id>`

Show agent performance metrics.

**Options:**
- `--format-json` - Output as JSON
- `--since <duration>` - Show metrics since duration ago

**Examples:**
```bash
nexo background-agent metrics health-monitor
nexo background-agent metrics code-analyzer --since 24h --format-json
```

**Output:**
```
Agent Metrics: health-monitor
  Execution Count: 1,234
  Average Duration: 2.3s
  Success Rate: 99.2%
  Last Execution: 2026-01-27 10:30:00 UTC
  Data Sensitivity Violations: 0
  Exfiltration Blocked: 0
```

## Configuration File Format

### Agent Configuration

```json
{
  "id": "my-agent",
  "name": "My Agent",
  "role": "analyzer",
  "parentId": "health-monitor",
  "modelProvider": "openai",
  "modelName": "gpt-4",
  "commands": ["analyze-code", "detect-issues"],
  "schedule": {
    "type": "interval",
    "interval": "01:00:00",
    "initialDelay": "00:05:00"
  },
  "enabled": true,
  "maxDataSensitivity": "Confidential",
  "parameters": {
    "analysisDepth": "thorough"
  },
  "rag": {
    "enabled": true,
    "vectorStoreProvider": "sqlite",
    "vectorStorePath": "./data/rag-store.db",
    "maxRetrievalResults": 5,
    "similarityThreshold": 0.7,
    "knowledgeSources": ["./docs", "./src"],
    "maxSourceSensitivity": "Internal"
  },
  "webSearch": {
    "enabled": true,
    "searchProvider": "bing",
    "apiKey": "${BING_API_KEY}",
    "maxResults": 10,
    "filterSensitiveContent": true,
    "allowedDomains": ["github.com", "stackoverflow.com"]
  },
  "exfiltrationPolicy": {
    "blockExternalLLMs": false,
    "blockWebSearch": false,
    "blockNetworkExports": true,
    "maxAllowedLevel": "Confidential"
  }
}
```

### Custom Sensitivity Level Configuration

```json
{
  "name": "CustomerData",
  "sensitivityValue": 2,
  "allowsExternalLLM": false,
  "allowsWebSearch": false,
  "requiresLocalOnly": false,
  "allowsNetworkExports": false,
  "description": "Customer-specific data, GDPR protected"
}
```

## Error Handling

All commands return appropriate exit codes:
- `0` - Success
- `1` - General error
- `2` - Configuration error
- `3` - Agent not found
- `4` - Permission denied
- `5` - Validation error

## Integration with Framework

All CLI commands use the framework's own infrastructure:
- `Orchestrator` for agent management operations
- `AgentFactory` for creating agents
- `LifecycleManager` for agent lifecycle
- `IDataSensitivityRegistry` for sensitivity level management
- `IVectorStore` for RAG operations
- `IWebSearchProvider` for web search

This ensures consistency and demonstrates the framework's capabilities through dog-fooding.
