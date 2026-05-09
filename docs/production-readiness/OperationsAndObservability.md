# Operations and observability

## Checklist

### Metrics and logs

- [ ] Structured logs from API, agents, and portal; log levels appropriate per environment.
- [ ] Central log aggregation chosen (or file rotation + shipper) for production.
- [ ] Metrics: CPU, memory, disk, request latency/error rate, queue depth (if any), mesh health (if used).

### SLOs and alerting

- [ ] SLO targets defined for critical paths (example: API availability 99.9%, p95 latency).
- [ ] Alerts wired to paging or on-call; alert runbook linked per alert.
- [ ] Non-paging warnings for capacity trends (disk growth).

### Health and readiness

- [ ] Liveness vs readiness probes documented for each container.
- [ ] Dependency checks (DB, Redis, etc.) reflected in readiness where applicable.

### Capacity

- [ ] Resource requests/limits set for production workloads.
- [ ] Growth plan: when to scale horizontally vs vertically.

### Runbooks

- [ ] At least: portal down, API errors spike, agents not processing, disk full, cert expiry.
- [ ] Runbook template: [RunbookTemplate.md](RunbookTemplate.md)

## Fill in (org-specific)

| Item | Your value |
| ---- | ---------- |
| Observability stack (e.g. Prometheus/Grafana, cloud APM) | |
| On-call rotation / tool (PagerDuty, Opsgenie, etc.) | |
