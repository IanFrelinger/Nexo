# HTTP API versioning policy

This is the versioning and breaking-change policy for the Nexo HTTP API served by `Nexo.API` (`MapNexoEndpoints`), the surface that `Nexo.Client` / `INexoClient`, the CLI, the portal and non-.NET integrators (Unreal, curl, generated clients) talk to. The NuGet package policy is separate: [`../SdkCompatibilityPolicy.md`](../SdkCompatibilityPolicy.md).

## The rule

| Release line | Path prefix | Breaking changes to documented endpoints |
|--------------|-------------|------------------------------------------|
| `v0.x` (from `v0.1.0`) | **unversioned**: `/health` at the root, everything else under `/api/...` | Allowed only in a minor (`0.(x+1).0`), never in a patch. Announced in `CHANGELOG.md` under **Breaking**, with a **deprecation window of one minor**: the old shape keeps working for the whole of the minor in which the replacement appears and is removed no earlier than the minor after that. |
| `1.0.0` and later | **`/api/v1/...`** is introduced at `1.0.0`; the unversioned `/api/...` paths keep answering as aliases of `v1` for the whole `1.x` line | A breaking change means a new prefix (`/api/v2/...`); `/api/v1` keeps working until the `2.x` line's deprecation notice expires (at least one MINOR of `2.x`). |

"Breaking" for an HTTP endpoint means any of: removing or renaming a documented route; changing the HTTP method; removing, renaming or retyping a documented request or response field; making an optional request field required; changing the meaning of a status code the endpoint documents; changing an authentication requirement in the stricter direction on an already-documented route (see the note on the execution surface below). Adding a route, adding an optional request field, adding a response field, or adding a status code for a new failure mode is **additive** and may ship in a patch.

Response bodies are JSON and clients must ignore unknown fields; that is what makes additive change safe.

## Documented surface (what the promise covers)

The endpoints below are the "documented surface" for testers and integrators in `v0.x`. Everything not in this list is internal to the portal/IDE experience and may change in any release (it is still listed in [`index.md`](index.md) for discovery).

| Method | Path | Purpose | Notes |
|--------|------|---------|-------|
| GET | `/health` | Liveness (`{status, timestamp}`) | Root, not under `/api`; unauthenticated; excluded from the OpenAPI description. There is no separate `/ready` route in `v0.1`: readiness is `GET /api/onboarding/status` (below), which reports provider availability. |
| GET | `/api/status` | Background-agent / node status | Also `INexoClient.GetStatusAsync` |
| GET | `/api/onboarding/status` | First-run setup status (provider availability) | The readiness probe for operators and the CLI |
| POST | `/api/copilot/task` | Submit a copilot task; returns the trust-auditable context (decision, recent audit entries) | Body: `CopilotTaskRequest`; audit context is part of the documented response |
| GET | `/api/copilot/tasks` | List copilot tasks | |
| POST | `/api/orchestrate` | Run orchestration | Also `INexoClient.OrchestrateAsync` |
| POST | `/api/agent` | Run an agent | Also `INexoClient.RunAgentAsync` |
| POST | `/api/validate` | Run validation | Also `INexoClient.RunValidationAsync` |
| GET | `/api/trust/dashboard` | Trust boundary status plus recent **audit** events | The audit read surface in `v0.1`; `GET /api/activity/feed` merges audit with background-agent activity and is documented too |
| GET | `/api/trust/status` | Trust boundary status | |
| POST | `/api/execution/build`, `/api/execution/run` | Remote container build / run | **Opt-in**: served only when `Nexo:Execution:ServeRemoteExecution=true` **and** the built-in auth mode resolves to something other than `None` (otherwise 403 "Remote execution requires built-in auth"). Tightening this further is not a breaking change; loosening it would be. Also `INexoClient.BuildImageAsync` / `RunContainerAsync` |
| POST | `/api/bricks/{brickId}/execute` | Execute an authored brick | The round-trip the `external-product-shape` distribution gate proves via `INexoClient.InvokeAsync` |
| GET | `/api/capabilities` | Node capability manifest | |

Everything else in [`index.md`](index.md) (director, chat/plan/edit, orgs, workloads, preferences, knowledge query, changelog generation, runtime-studio metrics, support diagnostics) is **not** on the documented surface in `v0.1`. It stays reachable via `INexoClient.InvokeAsync`, but it may change in a patch.

## Compatibility with the typed client

`Nexo.Client` (`INexoClient`) is a stable-tier NuGet package under [`SdkCompatibilityPolicy.md`](../SdkCompatibilityPolicy.md). Its typed methods wrap endpoints on the documented surface (except `QueryKnowledgeAsync`, a relative-path passthrough to a route that is not on it, treated like `InvokeAsync` below), so a breaking HTTP change to one of them is also a breaking client change and follows both policies at once (one minor deprecation, `[Obsolete]` on the old method, **Breaking** entry). `InvokeAsync` deliberately takes a relative path, so calling an undocumented route through it does not extend the promise to that route.

## Announcing a breaking change

1. Land the replacement shape additively (new route or new field), keeping the old one working.
2. Add a **Breaking** entry to `CHANGELOG.md` `[Unreleased]` naming the route, the old and new shape, and the release in which the old shape stops answering (`0.(x+2).0` at the earliest).
3. Ship the removal in that release, not before; the release notes repeat the entry.
