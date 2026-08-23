# Cloud AWS account structure (Phase 2.1)

Reference layout for **Ashlar Cloud** staging and production. Adapt account IDs and region to your org.

## Account topology

| Account | Purpose | Network |
|---------|---------|---------|
| **management** | AWS Organizations, billing, SSO | No workloads |
| **shared-services** | ECR, CI artifacts, Route53 public zones | Private connectivity to workload accounts |
| **ashlar-cloud-staging** | Multi-tenant staging | VPC per env; no prod data |
| **ashlar-cloud-prod** | Paying customers | Isolated subnets; WAF on ingress |

## IAM boundaries (least privilege)

| Role | Scope | Notes |
|------|-------|-------|
| `ashlar-deploy-staging` | EKS/ECS deploy in staging only | OIDC from GitHub Actions |
| `ashlar-runtime-prod` | Read secrets, write CloudWatch logs | No `*` on `s3:*` |
| `ashlar-support-readonly` | CloudWatch + diagnostics export bucket | Break-glass via SSO |

**Rule:** no long-lived access keys in git. Use **Secrets Manager** or **SSM Parameter Store** for:

- Stripe webhook signing secret
- Per-tenant BYOK vault references (not raw keys in shared config)

## VPC sketch (prod)

```
Internet → ALB (TLS) → Ashlar.API (private subnets)
                      → Ollama gateway / BYOK proxy (optional)
RDS or DynamoDB (tenant metadata) — private subnets only
S3 (artifacts) — bucket policies per tenant prefix (future)
```

## Secrets checklist

- [ ] `Ashlar__Security__ApiKey` in Secrets Manager, not compose files
- [ ] Stripe keys in management account billing integration only
- [ ] Separate KMS keys for staging vs prod
- [ ] CloudTrail enabled on all workload accounts

## Related

- [`cloud-reference-deployment.md`](./cloud-reference-deployment.md) — local multi-tenant compose shape
- [`MonetizationProductDesign.md`](../MonetizationProductDesign.md) — Cloud tier entitlements
