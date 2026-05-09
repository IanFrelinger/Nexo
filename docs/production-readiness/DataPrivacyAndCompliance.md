# Data privacy and compliance

This is a **framework** for supporting audits and customer questionnaires—not legal advice.

## Checklist

### Data classification

- [ ] Inventory of data types: prompts, code, PII, credentials, audit logs, model telemetry.
- [ ] Classification labels (public / internal / confidential / restricted).
- [ ] Where each class may be stored (volumes, DB, logs, third-party LLM if used).

### Retention and deletion

- [ ] Retention period per data class documented.
- [ ] Automated deletion or archival where required.
- [ ] Customer export / deletion process if applicable (GDPR-style).

### Encryption

- [ ] TLS in transit for external interfaces.
- [ ] Encryption at rest for databases and volumes holding confidential data (platform-specific).
- [ ] Document what is **not** encrypted (e.g. local dev default).

### Access control

- [ ] Who can read production logs and audit trails.
- [ ] Break-glass access documented and rare.

### Control mapping (optional but enterprise-friendly)

- [ ] Spreadsheet or doc mapping controls (e.g. access control, logging, backups) to **evidence** (screenshot, CI job name, policy PDF).
- [ ] Annual review date set.

## Fill in (org-specific)

| Item | Your value |
| ---- | ---------- |
| DPO / privacy contact | |
| Retention: audit logs (days/months) | |
| Retention: application logs | |
