#!/usr/bin/env python3
"""
Quick COGS calculator for the SaaS cost model (illustrative defaults).

  python3 scripts/saas_cost_model.py
  python3 scripts/saas_cost_model.py --tenants 200 --jobs-per-day 80
"""

from __future__ import annotations

import argparse
from dataclasses import dataclass


@dataclass(frozen=True)
class Inputs:
    tenants: int
    days_per_month: int
    hours_busy_per_day: float  # unused in COGS v1; kept for future concurrency model
    jobs_per_tenant_per_day: int
    tokens_in_per_job: int
    tokens_out_per_job: int
    price_in_per_million: float
    price_out_per_million: float
    storage_gb_per_tenant_month: float
    storage_per_gb_month: float
    egress_gb_per_tenant_month: float
    egress_per_gb: float
    shared_vcpu: int
    price_vcpu_hour: float
    managed_db_month: float
    fixed_overhead_month: float
    revenue_per_tenant_month: float
    stripe_percent: float
    stripe_fixed_per_charge: float


def token_cost_per_job(inp: Inputs) -> float:
    return (inp.tokens_in_per_job / 1_000_000) * inp.price_in_per_million + (
        inp.tokens_out_per_job / 1_000_000
    ) * inp.price_out_per_million


def compute_monthly(inp: Inputs) -> dict[str, float]:
    jobs_per_tenant_month = inp.jobs_per_tenant_per_day * inp.days_per_month
    llm_total = inp.tenants * jobs_per_tenant_month * token_cost_per_job(inp)
    storage_total = inp.tenants * inp.storage_gb_per_tenant_month * inp.storage_per_gb_month
    egress_total = inp.tenants * inp.egress_gb_per_tenant_month * inp.egress_per_gb
    vcpu_hours = inp.shared_vcpu * 24 * inp.days_per_month
    compute_total = vcpu_hours * inp.price_vcpu_hour
    subtotal = llm_total + storage_total + egress_total + compute_total + inp.managed_db_month + inp.fixed_overhead_month
    cogs_per_tenant = subtotal / inp.tenants if inp.tenants else 0.0
    mrr = inp.tenants * inp.revenue_per_tenant_month
    stripe_fees = inp.tenants * (
        inp.stripe_percent * inp.revenue_per_tenant_month + inp.stripe_fixed_per_charge
    )
    gross_after_stripe = mrr - subtotal - stripe_fees
    margin = gross_after_stripe / mrr if mrr else 0.0
    return {
        "jobs_per_tenant_month": jobs_per_tenant_month,
        "token_cost_per_job": token_cost_per_job(inp),
        "llm_total": llm_total,
        "storage_total": storage_total,
        "egress_total": egress_total,
        "compute_total": compute_total,
        "managed_db": inp.managed_db_month,
        "fixed_overhead": inp.fixed_overhead_month,
        "cogs_total": subtotal,
        "cogs_per_tenant": cogs_per_tenant,
        "mrr": mrr,
        "stripe_fees": stripe_fees,
        "gross_profit": gross_after_stripe,
        "gross_margin": margin,
    }


def main() -> None:
    p = argparse.ArgumentParser(description="SaaS COGS sanity check (illustrative defaults).")
    p.add_argument("--tenants", type=int, default=100)
    p.add_argument("--days", type=int, default=30, help="Days in month")
    p.add_argument("--jobs-per-day", type=int, default=50, dest="jobs_per_day")
    p.add_argument("--tokens-in", type=int, default=4000, dest="tokens_in")
    p.add_argument("--tokens-out", type=int, default=800, dest="tokens_out")
    p.add_argument("--price-in-per-m", type=float, default=0.50, dest="price_in")
    p.add_argument("--price-out-per-m", type=float, default=1.50, dest="price_out")
    p.add_argument("--storage-gb", type=float, default=25.0, help="GB stored per tenant-month")
    p.add_argument("--storage-rate", type=float, default=0.023, help="$ per GB-month")
    p.add_argument("--egress-gb", type=float, default=5.0, help="Egress GB per tenant-month")
    p.add_argument("--egress-rate", type=float, default=0.09, help="$ per GB egress")
    p.add_argument("--shared-vcpu", type=int, default=16)
    p.add_argument("--vcpu-hour-rate", type=float, default=0.04)
    p.add_argument("--db-month", type=float, default=200.0)
    p.add_argument("--fixed-month", type=float, default=500.0)
    p.add_argument("--revenue-per-tenant", type=float, default=98.0, help="$ MRR per tenant (e.g. 2 seats x $49)")
    p.add_argument("--stripe-percent", type=float, default=0.029)
    p.add_argument("--stripe-fixed", type=float, default=0.30, help="$ per successful charge")
    p.add_argument("--no-llm", action="store_true", help="Assume customers bring own keys (zero LLM COGS)")
    args = p.parse_args()

    inp = Inputs(
        tenants=args.tenants,
        days_per_month=args.days,
        hours_busy_per_day=8.0,
        jobs_per_tenant_per_day=args.jobs_per_day,
        tokens_in_per_job=args.tokens_in,
        tokens_out_per_job=args.tokens_out,
        price_in_per_million=args.price_in,
        price_out_per_million=args.price_out,
        storage_gb_per_tenant_month=args.storage_gb,
        storage_per_gb_month=args.storage_rate,
        egress_gb_per_tenant_month=args.egress_gb,
        egress_per_gb=args.egress_rate,
        shared_vcpu=args.shared_vcpu,
        price_vcpu_hour=args.vcpu_hour_rate,
        managed_db_month=args.db_month,
        fixed_overhead_month=args.fixed_month,
        revenue_per_tenant_month=args.revenue_per_tenant,
        stripe_percent=args.stripe_percent,
        stripe_fixed_per_charge=args.stripe_fixed,
    )

    r = compute_monthly(inp)
    if args.no_llm:
        r["llm_total"] = 0.0
        r["cogs_total"] = (
            r["storage_total"]
            + r["egress_total"]
            + r["compute_total"]
            + inp.managed_db_month
            + inp.fixed_overhead_month
        )
        r["cogs_per_tenant"] = r["cogs_total"] / inp.tenants if inp.tenants else 0.0
        r["gross_profit"] = r["mrr"] - r["cogs_total"] - r["stripe_fees"]
        r["gross_margin"] = r["gross_profit"] / r["mrr"] if r["mrr"] else 0.0

    # Expected anchors from the written example (default args)
    checks: list[tuple[str, float, float]] = [
        ("token_cost_per_job", r["token_cost_per_job"], 0.0032),
        ("jobs_per_tenant_month", r["jobs_per_tenant_month"], 1500.0),
        ("llm_total (defaults)", r["llm_total"], 480.0),
        ("storage_total", r["storage_total"], 57.5),
        ("egress_total", r["egress_total"], 45.0),
        ("compute_total", r["compute_total"], 460.8),
        ("cogs_total", r["cogs_total"], 1743.3),
        ("cogs_per_tenant", r["cogs_per_tenant"], 17.433),
    ]

    print("=== Inputs ===")
    for k, v in vars(args).items():
        if k == "no_llm":
            continue
        print(f"  {k}: {v}")
    print(f"  no_llm: {args.no_llm}")

    print("\n=== Derived ===")
    print(f"  token_cost_per_job: ${r['token_cost_per_job']:.6f}")
    print(f"  jobs_per_tenant_month: {r['jobs_per_tenant_month']:.0f}")

    print("\n=== Monthly COGS (fleet) ===")
    print(f"  LLM:              ${r['llm_total']:,.2f}")
    print(f"  Storage:          ${r['storage_total']:,.2f}")
    print(f"  Egress:           ${r['egress_total']:,.2f}")
    print(f"  Compute (shared): ${r['compute_total']:,.2f}")
    print(f"  Managed DB:       ${inp.managed_db_month:,.2f}")
    print(f"  Fixed overhead:   ${inp.fixed_overhead_month:,.2f}")
    print(f"  TOTAL COGS:       ${r['cogs_total']:,.2f}")
    print(f"  COGS / tenant:    ${r['cogs_per_tenant']:,.2f}")

    print("\n=== Revenue (illustrative) ===")
    print(f"  MRR:              ${r['mrr']:,.2f}")
    print(f"  Stripe (approx):  ${r['stripe_fees']:,.2f}")
    print(f"  Gross profit:     ${r['gross_profit']:,.2f}")
    print(f"  Gross margin:     {100 * r['gross_margin']:.1f}%")

    print("\n=== Verification (defaults only; ~float tolerance) ===")
    tol = 0.05 if not args.no_llm else 1.0  # skip strict checks when overrides used
    defaults = (
        args.tenants == 100
        and args.days == 30
        and args.jobs_per_day == 50
        and args.tokens_in == 4000
        and args.tokens_out == 800
        and abs(args.price_in - 0.5) < 1e-9
        and abs(args.price_out - 1.5) < 1e-9
        and abs(args.storage_gb - 25) < 1e-9
        and abs(args.storage_rate - 0.023) < 1e-9
        and abs(args.egress_gb - 5) < 1e-9
        and abs(args.egress_rate - 0.09) < 1e-9
        and args.shared_vcpu == 16
        and abs(args.vcpu_hour_rate - 0.04) < 1e-9
        and abs(args.db_month - 200) < 1e-9
        and abs(args.fixed_month - 500) < 1e-9
    )
    if not defaults or args.no_llm:
        print("  (skipped: non-default flags or --no-llm)")
    else:
        failed = False
        for name, got, want in checks:
            if args.no_llm and "llm" in name.lower():
                continue
            ok = abs(got - want) <= tol
            failed = failed or not ok
            status = "OK" if ok else "FAIL"
            print(f"  [{status}] {name}: got {got:.6g}, want {want}")
        if failed:
            raise SystemExit(1)
        print("  All default checks passed.")


if __name__ == "__main__":
    main()
