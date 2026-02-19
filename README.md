# Nexo

Nexo is a .NET platform for AI-assisted development, testing, and geospatial workflows. It provides agents, orchestrators, and a CLI for code analysis, universal application testing (web, desktop, games, APIs), geospatial data processing, and autonomous development tasks.

## Features

- **Universal Testing Agent**: AI-powered testing of any application (web apps, desktop apps, games, APIs, CLIs) using vision models and structured adapters
- **Game Testing**: Support for game testing via Nexo plugin (TCP protocol) or pixel-only screen capture
- **YouTube/Video Analysis**: Docker-based workflow for summarizing YouTube videos and live gameplay using vision/video models
- **Geospatial**: Terrain, vector, and world-bundle generation with AI-assisted workflows
- **Code Analysis & Validation**: Architecture policies, assembly analysis, and validation
- **Autonomous Dev Agent**: AI agent for development tasks with optional human-in-the-loop
- **Background Agents**: Scheduled agents for RAG, web search, code analysis, and self-extend pipelines
- **Orchestration**: Multi-agent workflows with MediatR, FluentValidation, and workflow definitions

## Prerequisites

- .NET 8 SDK
- Docker (for YouTube/video and game testing in containers)
- Ollama (optional, for local LLM/vision; see [Providers](#providers))

## Quick Start

```bash
# Build the solution
dotnet build Nexo.sln

# Run the CLI
dotnet run --project src/Nexo.CLI -- --help

# Run a quick offline test (no external services)
dotnet run --project src/Nexo.CLI -- test --target "cli://dotnet --version" --goal "Verify the command runs" --provider offline
```

## CLI Commands

| Command | Description |
|---------|-------------|
| `nexo test` | Run Universal Testing Agent (web, desktop, game, API, CLI) |
| `nexo game-watch` | Live gameplay watch mode: capture and summarize video game screens (vision AI) |
| `nexo demo youtube-docker` | YouTube video summary test in Docker (headless) |
| `nexo demo youtube-transcribe` | Transcribe YouTube video audio (Whisper) |
| `nexo analyze` | Run code/assembly analyzers and policies |
| `nexo validate` | Run architecture tests and contract checks |
| `nexo agent` | Execute a named agent action |
| `nexo orchestrate` | Run orchestration workflows |
| `nexo dev` | Autonomous development agent |
| `nexo config` | Show or update configuration |
| `nexo geo-vector` | Geovector operations |
| `nexo world` | World bundle generation |
| `nexo background-agent` | Manage background agents (start, stop, logs, metrics) |
| `nexo docker` | Docker subcommands for video/model services |

## Universal Testing Agent

The Universal Tester can test web apps, desktop apps, games, APIs, and CLI tools. It uses a pipeline of bricks: **Perception** → **Understanding** → **Exploration** → **Action** → **Validation** → **Reporting**.

### Targets

- **Web**: `https://example.com`
- **Desktop**: `process://AppName` or executable path
- **Game**: `game://localhost:9999` or `tcp://host:port` (requires Nexo plugin in-game)
- **API**: `api://base-url`
- **CLI**: `cli://command args`

### Example

```bash
nexo test --target "https://example.com" --goal "Verify the homepage loads and has a search box" --provider ollama
nexo test --target "process://Chrome" --goal "Find and click the settings menu" --provider ollama
nexo game-watch --process GameName --duration 5m --summary-interval 15
```

### Agent Spec (Runtime Config)

Use `--agent-spec path/to/spec.json` or `--agent-spec-json '{...}'` to configure per-brick behavior, model overrides, pipeline order, and adapters. See `docs/universal-tester-agent-spec.example.json` and `docs/UniversalTesterComponentArchitecture.md`.

## Providers

LLM and vision models are routed by the `provider` setting:

| Provider | Description |
|----------|-------------|
| `offline` / `mock` / `mock-json` | No external services; deterministic responses for CI/demo |
| `ollama` | Local Ollama (set `OLLAMA_BASE_URL`, `OLLAMA_MODEL`, `OLLAMA_VISION_MODEL`) |
| `openai` | OpenAI API (set `OPENAI_API_KEY`) |
| `azure` | Azure OpenAI (set `AZURE_OPENAI_*` env vars) |
| `video` | Video model container (set `VIDEO_SERVICE_URL` for SmolVLM2-Video) |

## Project Structure

```
Nexo/
├── src/
│   ├── Nexo.CLI/                 # Command-line interface
│   ├── Nexo.Agents.UniversalTester/   # Universal testing agent, bricks, adapters
│   ├── Nexo.Agents.UniversalTester.Windows/   # Windows-specific desktop adapter
│   ├── Nexo.Agents.AutonomousDev/     # Autonomous dev agent
│   ├── Nexo.API/                  # REST API for geospatial, jobs, agents
│   ├── Nexo.Infrastructure/       # ProviderFactory, execution, IO
│   ├── Nexo.Orchestration/        # Workflows, agents, playtest
│   ├── Nexo.Core.Domain/         # Domain models, bricks, execution
│   ├── Nexo.Core.Application/    # Use cases, ports, services
│   ├── Nexo.GeoTerrain/          # Terrain generation
│   ├── Nexo.GeoVector/           # Vector tile generation
│   ├── Nexo.GeoWorld/            # World bundle assembly
│   ├── Nexo.Guide/               # Nexo Guide desktop app
│   ├── Nexo.BackgroundAgents/    # Background agent scheduling
│   └── Nexo.Tests.*/             # Unit and integration tests
├── docs/                         # Architecture, specs, examples
├── .docker/                      # Dockerfiles for video model, youtube-test
├── docker-compose.youtube-test.yml
└── Nexo.sln
```

## Docker

### YouTube Video Test (Headless)

```bash
nexo docker youtube --url "https://www.youtube.com/watch?v=VIDEO_ID"
# Or with watch mode (live summaries):
nexo docker youtube --url "URL" --watch --duration 5m
# With video model (SmolVLM2-Video):
nexo docker youtube --url "URL" --watch --use-video-model
```

Uses `docker-compose.youtube-test.yml` with Chrome, xdotool (for virtual desktop interaction), and optional video/audio services.

### SmolVLM2 Video Service

For temporal video analysis, the SmolVLM2-Video service encodes frames to video and runs inference. Start it with `--use-video-model` when using watch mode, or manually:

```bash
docker compose -f docker-compose.youtube-test.yml up -d video-service
```

## Game Testing

For games with a Nexo plugin (e.g., Unity):

1. Integrate a TCP server in your game that implements the Nexo protocol (HELLO, SCREENSHOT, GAMESTATE, PLAYERSTATE, INTERACTABLES, CLICK, MOVE, etc.).
2. Run the game with `--nexo-test-mode --nexo-port=9999` (or equivalent).
3. Run `nexo test --target "game://localhost:9999" --goal "..."`.

For games **without** a plugin, use pixel-only mode:

- `nexo game-watch --process GameName` for live summaries.
- `nexo test --target "process://GameName"` for full testing via screen capture (DesktopAdapter).

See `docs/UniversalTesterComponentArchitecture.md` for integration details.

## Testing

```bash
dotnet test Nexo.sln
# Filter to specific test project:
dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj
```

## Documentation

- `docs/UniversalTesterComponentArchitecture.md` – Universal Tester design, phases, config
- `docs/universal-tester-agent-spec.example.json` – Example agent spec
- `docs/` – Additional architecture and usage guides

## License

See repository for license information.
