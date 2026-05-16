# Sample Step Functions + GitHub approval path

`sample-approval.asl.json` is a **minimal** Amazon States Language definition showing:

1. A **Pass** state normalizing input (replace `$.detail` with your EventBridge or Step Functions input shape).
2. A **Lambda** task using the **callback pattern** (`waitForTaskToken`) so a worker can call `SendTaskSuccess` after a GitHub check completes.

## Wiring GitHub

Typical production pieces:

- **Lambda** with a **GitHub App** installation token or **fine-scoped PAT** stored in **Secrets Manager**.
- Calls such as [`POST /repos/{owner}/{repo}/actions/workflows/{workflow_id}/dispatches`](https://docs.github.com/en/rest/actions/workflows?apiVersion=2022-11-28#create-a-workflow-dispatch-event) or environment approval APIs, depending on your gate.
- **GitHub Environments** with required reviewers often replace custom Step Functions for human approval; use this sample when SMS or external systems must join the same state machine.

## Security

- Never commit PATs; use OIDC where possible for AWS ↔ GitHub federation.
- Restrict Lambda security groups and Step Functions IAM to least privilege.
