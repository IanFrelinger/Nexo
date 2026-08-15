# Security Policy

## Supported versions

Nexo is pre-1.0. Security fixes land on `master` and ship in the next tagged release; only the most recent release and `master` are supported.

## Reporting a vulnerability

Please do **not** open a public issue for a security vulnerability.

- Preferred: report privately via [GitHub Security Advisories](https://github.com/IanFrelinger/Nexo/security/advisories/new).
- Alternatively: contact the maintainer, [@IanFrelinger](https://github.com/IanFrelinger).

Include the affected component (project or path), a reproduction or proof of concept, and your assessment of the impact. You should receive an acknowledgement within 7 days.

## Scope

Nexo's security-relevant surface includes the trust path components: policy gates, sanitization and PII/secret filters, audit trails and barrier identity, mesh/federation transport, and execution-target routing (local, cloud, peer). Reports about weaknesses in these boundaries are especially welcome, as are reports about the standard surfaces (`Nexo.API`, `Nexo.CLI`, container images, compose deployments).
