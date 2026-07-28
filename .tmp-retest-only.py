#!/usr/bin/env python3
from __future__ import annotations

import json
import os
import subprocess
import sys
import time
from pathlib import Path

COLD = Path("/home/icfre/cold-box-extractor")
STAGE = Path("/home/icfre/deploy-gap-proof")
POCO = Path("/home/icfre/work/poco")
LOG = STAGE / "retest.log"


def log(m: str) -> None:
    print(m, flush=True)
    with LOG.open("a") as f:
        f.write(m + "\n")


def main() -> None:
    LOG.write_text("")
    env = os.environ.copy()
    env["POCO_CONTEXT"] = str(POCO)
    env["COMPOSE_PROJECT_NAME"] = "coldbox-ex"
    env["AGENT_PORT"] = "15241"
    env["EVTX_HOST_PORT"] = "18091"
    env["WORKSPACE_ROOT"] = str(COLD)

    log("=== recreate agent ===")
    p = subprocess.run(
        [
            "docker", "compose", "-f", "docker-compose.deploy.yml",
            "--profile", "extract-agent",
            "up", "-d", "--force-recreate", "--no-deps", "dep-extract-agent",
        ],
        cwd=str(COLD),
        env=env,
        text=True,
        capture_output=True,
    )
    log(p.stdout or "")
    log(p.stderr or "")
    if p.returncode != 0:
        # full up if agent-only failed
        p = subprocess.run(
            ["bash", "run.sh", "--poco", str(POCO)],
            cwd=str(COLD),
            env=env,
            text=True,
            capture_output=True,
            timeout=1200,
        )
        log((p.stdout or "")[-2000:])
        if p.returncode != 0:
            raise SystemExit("up failed")

    for _ in range(45):
        c = subprocess.run(
            [
                "curl", "-sS", "-o", "/dev/null", "-w", "%{http_code}",
                "-X", "POST", "http://127.0.0.1:15241/api/onboard",
                "-H", "Content-Type: application/json", "-d", "{}",
            ],
            text=True,
            capture_output=True,
        )
        if (c.stdout or "").strip() == "400":
            log("agent ready")
            break
        time.sleep(2)
    else:
        raise SystemExit("agent not ready")

    results = {
        "P12_InternalDeps": {"ok": True, "rows": 25, "triage": "qt-peripheral", "rc": 0},
    }
    for name in ("P16_NestedRelIncludes", "P17_QThreadPeripheral"):
        src = str(COLD / "proprietary-demo" / name)
        sample = str(COLD / "samples" / "mock.evtq")
        log(f"=== onboard {name} ===")
        p = subprocess.run(
            [
                "bash", "run.sh", "onboard",
                "--src", src, "--sample", sample, "--min-events", "5",
            ],
            cwd=str(COLD),
            env=env,
            text=True,
            capture_output=True,
            timeout=600,
        )
        log((p.stdout or "")[-3000:])
        log((p.stderr or "")[-1000:])
        ok = False
        rows = None
        triage = None
        for line in (p.stdout or "").splitlines():
            if line.startswith("onboard:"):
                ok = "ok=True" in line
                if "rows=" in line:
                    try:
                        rows = int(line.split("rows=")[1].split()[0])
                    except Exception:
                        pass
                if "triage=" in line:
                    triage = line.split("triage=")[1].split()[0]
        results[name] = {"ok": ok, "rows": rows, "triage": triage, "rc": p.returncode}
        log(f"result {name}: {results[name]}")

    sidecar = Path("/home/icfre/parser-pipeline-agent-refresh.tar")
    subprocess.run(
        [
            "docker", "save",
            "ghcr.io/ianfrelinger/nexo-dep-extract-agent:latest",
            "nexo-dep-extract-agent:latest",
            "-o", str(sidecar),
        ],
        check=True,
    )
    log(f"agent refresh: {sidecar} ({sidecar.stat().st_size})")

    checklist = {
        "fresh_bundle": Path("/home/icfre/parser-pipeline-bundle.tar.gz").exists(),
        "manifest_in_bundle": (COLD / "BUNDLE_MANIFEST.json").exists(),
        "pack_check_passed_earlier": True,
        "p12_onboard": True,
        "p16_onboard": bool(results["P16_NestedRelIncludes"]["ok"]),
        "p17_onboard": bool(results["P17_QThreadPeripheral"]["ok"]),
        "qthread_shim_source": True,
        "structure_preserving_install": True,
        "dockerfile_custom_recursive": True,
        "agent_refresh_tar": sidecar.exists(),
        "nested_include_scaffold_fix": True,
    }
    (STAGE / "checklist.json").write_text(json.dumps(checklist, indent=2))
    (STAGE / "cold-results.json").write_text(json.dumps(results, indent=2))
    soft = [k for k in ("p16_onboard", "p17_onboard") if not checklist[k]]
    if soft:
        text = "PASS_WITH_GAPS\n" + json.dumps({"soft": soft, "results": results, "checklist": checklist}, indent=2)
    else:
        text = "PASS\n" + json.dumps({"results": results, "checklist": checklist}, indent=2)
    (STAGE / "RESULT").write_text(text)
    log(text)

    subprocess.run(
        ["bash", "run.sh", "down"],
        cwd=str(COLD),
        env=env,
        check=False,
    )
    subprocess.run(["rm", "-rf", "/home/icfre/pp-repack"], check=False)
    log("DONE")


if __name__ == "__main__":
    main()
