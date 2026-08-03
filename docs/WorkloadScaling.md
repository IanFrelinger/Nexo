# Workload scaling (Kubernetes-first, swappable)

Nexo can **dynamically change container replica counts** through a first-class port:

| Port | Role |
|------|------|
| `IWorkloadScaler` | Scale / inspect replicas (provider-specific) |
| `IWorkloadScalePolicy` | Pure queue→replicas decision |
| `IWorkloadDemandSignal` | Demand metrics feed (default: zeros; replace with mesh elastic status) |

Providers are selected by config/env — **swap without changing call sites**.

## Providers

| `Nexo:WorkloadScaling:Provider` / `NEXO_WORKLOAD_SCALER` | Implementation |
|----------------------------------------------------------|----------------|
| `null` (default) | `NullWorkloadScaler` — safe no-op |
| `kubernetes` / `k8s` | `KubernetesWorkloadScaler` — `kubectl scale deployment/...` |
| `compose` / `docker-compose` | `ComposeWorkloadScaler` — `docker compose up --scale` |

Add another adapter (ECS, Nomad, …) by implementing `IWorkloadScaler` and extending the switch in `AddNexoWorkloadScaling`.

## Configuration

```json
{
  "Nexo": {
    "WorkloadScaling": {
      "Provider": "kubernetes",
      "Enabled": true,
      "Kubernetes": {
        "KubectlPath": "kubectl",
        "Namespace": "nexo",
        "Workloads": {
          "mesh-worker": {
            "Deployment": "nexo-mesh-worker",
            "MinReplicas": 1,
            "MaxReplicas": 20,
            "DisplayName": "Mesh Worker"
          }
        }
      },
      "Autoscale": {
        "Enabled": false,
        "IntervalSeconds": 30,
        "ScaleUpQueueDepth": 5,
        "ScaleDownQueueDepth": 0,
        "WorkloadIds": [ "mesh-worker" ]
      }
    }
  }
}
```

Env shortcuts:

- `NEXO_WORKLOAD_SCALER=kubernetes|compose|null`
- `NEXO_WORKLOAD_SCALING_ENABLED=true|false`
- `NEXO_WORKLOAD_AUTOSCALE=true` — starts `ElasticWorkloadAutoscaleService`

Sample Deployment: [`deploy/k8s/nexo-mesh-worker-deployment.yaml`](../deploy/k8s/nexo-mesh-worker-deployment.yaml).

## HTTP API

| Method | Path | Purpose |
|--------|------|---------|
| `GET` | `/api/workloads/provider` | Active provider + availability |
| `GET` | `/api/workloads` | Configured workloads |
| `GET` | `/api/workloads/{id}/replicas` | Desired / current / ready |
| `PUT` | `/api/workloads/{id}/replicas` | `{ "desiredReplicas": 3, "reason": "..." }` |

## How this relates to mesh “elastic” scheduling

[`MeshPhase5ElasticScheduling.md`](MeshPhase5ElasticScheduling.md) places work onto **existing** workers and exposes `GET /api/mesh/elastic/status` as an **operator signal**.

This module **creates/destroys capacity** (replicas). Wire them together by implementing `IWorkloadDemandSignal` that reads mesh elastic queue depth and registering it instead of `NullWorkloadDemandSignal`.

## Manual scale (kubectl)

```bash
kubectl -n nexo scale deployment/nexo-mesh-worker --replicas=3
```

Or via Nexo:

```bash
curl -X PUT http://localhost:8088/api/workloads/mesh-worker/replicas \
  -H 'content-type: application/json' \
  -d '{"desiredReplicas":3,"reason":"load spike"}'
```
