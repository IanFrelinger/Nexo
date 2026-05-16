# AWS SNS → Nexo SMS webhook (sample Lambda)

This folder shows a minimal **Node.js 20** Lambda that converts an SNS-triggered payload into the **same JSON shape** Amazon SNS uses for HTTP(S) subscriptions, so `Nexo.Ingress.AwsSns` signature verification in Nexo.API can run unchanged.

## Deploy (SAM CLI)

1. Package and deploy (replace the URL with your Nexo endpoint):

```bash
sam deploy --guided --parameter-overrides NexoSmsUrl=https://your-host.example.com/api/ingress/sms/sns
```

2. In the **SNS console**, subscribe this function’s ARN to your inbound SMS topic (or use End User Messaging → SNS destination).

## DynamoDB table (when using `SmsIngressApprovalStore: DynamoDb`)

Create a table with **string** partition key `pk` and sort key `sk` (on-demand billing is fine):

```bash
aws dynamodb create-table \
  --table-name NexoSmsIngress \
  --attribute-definitions AttributeName=pk,AttributeType=S AttributeName=sk,AttributeType=S \
  --key-schema AttributeName=pk,KeyType=HASH AttributeName=sk,KeyType=RANGE \
  --billing-mode PAY_PER_REQUEST
```

Items use `pk = NexoSmsIngress` and `sk = sid:<MessageId>` (or a hash when no MessageId). Grant the API task role `dynamodb:PutItem` and `dynamodb:GetItem` on the table ARN.

## Notes

- Prefer **VPC + private API** or **API Gateway** in front of Nexo instead of exposing the API host directly.
- Outside the `Testing` environment, Nexo requires **non-empty** `AwsSnsAllowedTopicArnPrefixes` when the SNS webhook is enabled.
