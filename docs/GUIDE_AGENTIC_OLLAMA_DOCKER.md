# Agentic Guide Test with Ollama (Docker)

The agentic Guide test runs the Universal Tester agent against the Nexo Guide app with a real LLM (Ollama). When using Ollama in Docker, 500 errors usually mean **the model could not load due to memory**.

## What the logs mean

From `docker logs ollama`:

```text
model request too large for system  requested="2.3 GiB" available="2.1 GiB" ...
Load failed ... error="model requires more system memory (2.3 GiB) than is available (2.1 GiB)"
[GIN] ... | 500 | ... | POST "/api/chat"
```

- **requested** = RAM the model needs to load (e.g. llama3.2:3b ~2.3 GiB, llava:7b ~4.3 GiB).
- **available** = free memory inside the container when the load was attempted.
- **500** = Ollama returns 500 when the model fails to load (e.g. OOM).

So the container doesn’t have enough free RAM for the model.

## Fixes

1. **Give Docker more memory**  
   Docker Desktop → Settings → Resources → Memory → set to **8 GB or more**, then apply and restart the `ollama` container.

2. **Run Ollama on the host (no Docker)**  
   - Install Ollama and run `ollama serve`.
   - `ollama pull llama3.2:3b` and `ollama pull llava:7b`.
   - Run the test with the same env vars; it will use `http://localhost:11434` and full system RAM.

3. **Use a smaller model**  
   If you must stay in Docker with limited memory, use a smaller model (e.g. a 1B or smaller variant) and set `OLLAMA_MODEL` / `OLLAMA_VISION_MODEL` to that model. Vision models are generally larger; llava:7b needs ~4.3 GiB.

## Run the test

**Via CLI (recommended):**

```bash
# From repo root: run test (Ollama must be running)
dotnet run --project src/Nexo.CLI -- test guide-agentic

# Or start Ollama in Docker first, then run the test
dotnet run --project src/Nexo.CLI -- test guide-agentic --docker
```

If you install the CLI as a tool (`dotnet tool install`), you can run `nexo test guide-agentic` or `nexo test guide-agentic --docker`.

**Via script or dotnet test directly:**

```bash
# With Docker (ensure container has enough memory)
./scripts/run-guide-agentic-test-docker.sh

# With native Ollama
export NEXO_RUN_AGENTIC_GUIDE_TEST=1
export OLLAMA_MODEL=llama3.2:3b
export OLLAMA_VISION_MODEL=llava:7b
dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj -f net8.0 \
  --filter "FullyQualifiedName~Guide_Agentic_UniversalTester_ShouldTestAllInteractions_WhenRunWithOllama" \
  -p:TreatWarningsAsErrors=false --logger "console;verbosity=normal"
```
