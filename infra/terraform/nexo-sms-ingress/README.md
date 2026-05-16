# Terraform: Nexo SMS ingress (DynamoDB + optional WAF)

Creates a DynamoDB table compatible with `Nexo.Ingress.DynamoDb` (`pk` / `sk` string keys) and an optional **AWS WAFv2** regional Web ACL with a **rate-based rule** (IP aggregate). WAF association targets an **ALB ARN** you supply.

## Usage

```bash
cd infra/terraform/nexo-sms-ingress
terraform init
terraform apply -var="name_prefix=myorg-nexo" -var="create_waf=true" -var="alb_arn=arn:aws:elasticloadbalancing:..."
```

Set `Nexo:SmsIngressDynamoDb:TableName` to the output `dynamodb_table_name` and `Nexo:MiddlewareIngress:SmsIngressApprovalStore` to `DynamoDb`. Grant the Nexo task role `dynamodb:GetItem` and `dynamodb:PutItem` on the table ARN.

Tune `limit` inside `rate_based_statement` for your SMS volume.
