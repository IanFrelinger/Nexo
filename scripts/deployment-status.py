#!/usr/bin/env python3
"""Write a structured, append-only deployment status/evidence record."""
from __future__ import annotations
import argparse, json, subprocess
from datetime import datetime, timezone
from pathlib import Path

def cmd(*args: str) -> str:
    try:
        return subprocess.check_output(args, text=True, stderr=subprocess.DEVNULL).strip()
    except Exception:
        return ""

def main() -> None:
    p=argparse.ArgumentParser()
    p.add_argument("--preflight", type=Path, required=True)
    p.add_argument("--out", type=Path, default=Path("deployment_evidence.jsonl"))
    p.add_argument("--run-id", default=datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ"))
    a=p.parse_args()
    preflight=json.loads(a.preflight.read_text())
    services=[]
    for line in cmd("docker","compose","ps","--format","json").splitlines():
        try:
            row=json.loads(line)
            services.append({"name":row.get("Service"),"healthy":row.get("Health") in ("healthy",""),"health_detail":row.get("State")})
        except json.JSONDecodeError:
            pass
    images=[]
    for row in cmd("docker","compose","images","--format","json").splitlines():
        try:
            item=json.loads(row); images.append({"name":item.get("Container"),"ref":item.get("Repository"),"digest":item.get("ID","")})
        except json.JSONDecodeError:
            pass
    record={"schema_version":"1.0.0","record_type":"deployment_evidence","deployment_run_id":a.run_id,
            "started_at":preflight["checked_at"],"finished_at":datetime.now(timezone.utc).isoformat(),
            "outcome":"PASS" if preflight["preflight_pass"] else "FAIL",
            "fail_reason":None if preflight["preflight_pass"] else "; ".join(preflight["failures"]),
            "host":{**preflight["host"],"selected_profile":preflight["profile"]["id"],"preflight_pass":preflight["preflight_pass"]},
            "images":images,"services":services,"tests":[],"onboarding_run_ids":[]}
    a.out.parent.mkdir(parents=True,exist_ok=True)
    with a.out.open("a",encoding="utf-8") as f: f.write(json.dumps(record,sort_keys=True)+"\n")
    print(json.dumps(record,indent=2))
if __name__=="__main__": main()
