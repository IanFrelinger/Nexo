# Reliability and chaos

## Checklist

### Failure modes

- [ ] Documented: single host loss, dependency timeout, partial mesh partition, disk full, OOM.
- [ ] Graceful degradation where possible (read-only mode, queue backlog messaging).

### Timeouts and retries

- [ ] External calls have timeouts; retries are **bounded** and idempotent where they mutate state.

### Resource safety

- [ ] Production limits on concurrent agents or jobs if unbounded work exists.
- [ ] Backpressure or shedding policy documented under overload.

### Chaos or game days

- [ ] Quarterly (or semiannual) drill: kill one container, block egress, or slow disk; verify recovery.
- [ ] Results recorded; gaps become tickets.

### Disaster recovery

- [ ] RPO/RTO targets written for stateful components.
- [ ] Restore from backup tested on a schedule.

## Fill in (org-specific)

| Item | Your value |
| ---- | ---------- |
| RPO (max acceptable data loss) | |
| RTO (max acceptable downtime) | |
| Last restore drill date | |
