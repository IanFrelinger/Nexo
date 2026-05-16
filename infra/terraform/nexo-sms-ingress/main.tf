terraform {
  required_version = ">= 1.5.0"
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = ">= 5.0"
    }
  }
}

variable "name_prefix" {
  type        = string
  description = "Prefix for resource names"
  default     = "nexo-sms-ingress"
}

variable "create_waf" {
  type        = bool
  description = "When true, attach a rate-based WAFv2 Web ACL (requires an ALB ARN via var.alb_arn)."
  default     = false
}

variable "alb_arn" {
  type        = string
  description = "Application Load Balancer ARN for WAF association (only when create_waf is true)."
  default     = ""
}

resource "aws_dynamodb_table" "sms_ingress" {
  name         = "${var.name_prefix}-approvals"
  billing_mode = "PAY_PER_REQUEST"
  hash_key     = "pk"
  range_key    = "sk"

  attribute {
    name = "pk"
    type = "S"
  }

  attribute {
    name = "sk"
    type = "S"
  }

  point_in_time_recovery {
    enabled = true
  }
}

resource "aws_wafv2_web_acl" "rate" {
  count       = var.create_waf ? 1 : 0
  name        = "${var.name_prefix}-rate"
  description = "Rate-based protection for Nexo SMS ingress (tune limit for your traffic)."
  scope       = "REGIONAL"

  default_action {
    allow {}
  }

  rule {
    name     = "RateLimitAll"
    priority = 1
    action {
      block {}
    }
    statement {
      rate_based_statement {
        limit                 = 2000
        aggregate_key_type    = "IP"
        evaluation_window_sec = 300
      }
    }
    visibility_config {
      cloudwatch_metrics_enabled = true
      metric_name                = "${var.name_prefix}Rate"
      sampled_requests_enabled   = true
    }
  }

  visibility_config {
    cloudwatch_metrics_enabled = true
    metric_name                = "${var.name_prefix}Acl"
    sampled_requests_enabled   = true
  }
}

resource "aws_wafv2_web_acl_association" "alb" {
  count        = var.create_waf && var.alb_arn != "" ? 1 : 0
  resource_arn = var.alb_arn
  web_acl_arn  = aws_wafv2_web_acl.rate[0].arn
}

output "dynamodb_table_name" {
  value = aws_dynamodb_table.sms_ingress.name
}

output "dynamodb_table_arn" {
  value = aws_dynamodb_table.sms_ingress.arn
}

output "waf_web_acl_arn" {
  value = try(aws_wafv2_web_acl.rate[0].arn, null)
}
