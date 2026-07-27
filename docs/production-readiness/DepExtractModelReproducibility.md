# DepExtract model reproducibility

## Two paths

| Path | Reproducible? | Notes |
|------|---------------|--------|
| **Deterministic scaffold** (`preferScaffold` / `PREFER_SCAFFOLD=1`) | **Yes** | No model weights involved. Prefer this for CI and locked demos (`COMPILE_GATE=0` GUI tests use it). |
| **Local model** (`OLLAMA_MODEL`) | Pin by digest | Same tag can move. Record `model_digest` in the installed `adapted_reader.hpp` provenance header. |

## Pinning the model

1. Prefer an explicit digest env when shipping:

   ```bash
   export OLLAMA_MODEL=qwen2.5-coder:7b
   export OLLAMA_MODEL_DIGEST=sha256:…
   ```

2. Or use `name@sha256:…` as the model string.

3. At install time, `OllamaModelPin` queries local `GET {OLLAMA_BASE_URL}/api/tags` and writes `model_tag` / `model_digest` into the greppable provenance block:

   ```text
   /* === NEXO_ADAPTER_PROVENANCE_BEGIN ===
    * strategy: model
    * model_tag: qwen2.5-coder:7b
    * model_digest: sha256:…
   === NEXO_ADAPTER_PROVENANCE_END === */
   ```

## Air-gap

Unless `NEXO_ALLOW_REMOTE_MODEL=1`, `OLLAMA_BASE_URL` must be loopback or on `NEXO_OLLAMA_ALLOWLIST` (default includes compose host `ollama`).
