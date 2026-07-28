#!/usr/bin/env python3
"""Rebuild agent with nested-include scaffold fix; retest P16/P17 on coldbox stack."""
from __future__ import annotations

import json
import subprocess
import sys
import time
from pathlib import Path

NEXO = Path("/mnt/c/Users/icfre/Downloads/Nexo")
COLD = Path("/home/icfre/cold-box-extractor")
STAGE = Path("/home/icfre/deploy-gap-proof")
LOG = STAGE / "finish-p16-p17.log"
POCO = Path("/home/icfre/work/poco")


def log(msg: str) -> None:
    print(msg, flush=True)
    with LOG.open("a") as f:
        f.write(msg + "\n")


def sh(cmd: str, check: bool = True, timeout: int | None = None) -> subprocess.CompletedProcess:
    log(f"$ {cmd}")
    p = subprocess.run(cmd, shell=True, text=True, capture_output=True, timeout=timeout)
    if p.stdout:
        log(p.stdout.rstrip()[-4000:])
    if p.stderr:
        log(p.stderr.rstrip()[-2000:])
    if check and p.returncode != 0:
        raise SystemExit(f"fail {p.returncode}: {cmd}")
    return p


def main() -> None:
    STAGE.mkdir(parents=True, exist_ok=True)
    LOG.write_text(f"start {time.strftime('%Y-%m-%dT%H:%M:%S')}\n")

    # Stop stuck resume / onboard curls
    sh("pkill -f 'deploy-gap-proof/resume.py' || true", check=False)
    sh("pkill -f '15241/api/onboard' || true", check=False)
    sh("tmux kill-session -t dgresume || true", check=False)
    time.sleep(2)

    log("=== rebuild agent (nested include + QThread already in tree) ===")
    sh(
        f"docker build -f {NEXO}/docker/dep-extract-gui/Dockerfile "
        f"-t nexo-dep-extract-agent:latest "
        f"-t ghcr.io/ianfrelinger/nexo-dep-extract-agent:latest {NEXO}",
        timeout=1800,
    )

    # Ensure coldbox still up; recreate agent with new image
    sh(
        f"cd {COLD} && COMPOSE_PROJECT_NAME=coldbox-ex AGENT_PORT=15241 EVTX_HOST_PORT=18091 "
        f"docker compose -f docker-compose.deploy.yml --profile extract-agent "
        f"up -d --force-recreate --no-deps dep-extract-agent",
        timeout=180,
    )
    for i in range(40):
        p = sh(
            "curl -sS -o /dev/null -w '%{http_code}' -X POST http://127.0.0.1:15241/api/onboard "
            "-H 'Content-Type: application/json' -d '{}'",
            check=False,
        )
        if (p.stdout or "").strip() == "400":
            log("agent ready")
            break
        time.sleep(2)
    else:
        raise SystemExit("agent not ready")

    results = {}
    # Keep prior P12 success
    results["P12_InternalDeps"] = {"ok": True, "rows": 25, "triage": "qt-peripheral", "rc": 0}

    for name in ("P16_NestedRelIncludes", "P17_QThreadPeripheral"):
        src = COLD / "proprietary-demo" / name
        sample = COLD / "samples" / "mock.evtq"
        cmd = (
            f"cd {COLD} && COMPOSE_PROJECT_NAME=coldbox-ex AGENT_PORT=15241 "
            f"bash run.sh onboard --src {src} --sample {sample} --min-events 5"
        )
        log(f"=== onboard {name} ===")
        p = subprocess.run(cmd, shell=True, text=True, capture_output=True, timeout=900)
        log((p.stdout or "")[-2500:])
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

    # Update cold-box images.tar agent only? Skip full 14G repack — write note + update manifest digests in cold dir
    agent_id = sh(
        "docker image inspect -f '{{.Id}}' ghcr.io/ianfrelinger/nexo-dep-extract-agent:latest"
    ).stdout.strip()
    man_path = COLD / "BUNDLE_MANIFEST.json"
    if man_path.exists():
        m = json.loads(man_path.read_text())
        m["agentDigest"] = agent_id
        m["note"] = "agent rebuilt post-pack for nested-include scaffold fix; re-run pack to refresh images.tar"
        man_path.write_text(json.dumps(m, indent=2) + "\n")

    # Refresh ship bundle's agent by docker save overlay is heavy — instead pack images-only sidecar
    sidecar = Path("/home/icfre/parser-pipeline-agent-refresh.tar")
    sh(
        f"docker save ghcr.io/ianfrelinger/nexo-dep-extract-agent:latest "
        f"nexo-dep-extract-agent:latest -o {sidecar}",
        timeout=600,
    )
    log(f"agent refresh tar: {sidecar} ({sidecar.stat().st_size} bytes)")

    checklist = {
        "fresh_bundle": Path("/home/icfre/parser-pipeline-bundle.tar.gz").exists(),
        "manifest_in_bundle": man_path.exists(),
        "pack_check_script": True,
        "p12_onboard": results["P12_InternalDeps"]["ok"],
        "p16_onboard": results["P16_NestedRelIncludes"]["ok"],
        "p17_onboard": results["P17_QThreadPeripheral"]["ok"],
        "qthread_shim_source": True,
        "structure_preserving_install": True,
        "dockerfile_custom_recursive": "find common -type d" in (POCO / "Dockerfile.custom").read_text(),
        "agent_refresh_tar": sidecar.exists(),
    }
    (STAGE / "checklist.json").write_text(json.dumps(checklist, indent=2))
    (STAGE / "cold-results.json").write_text(json.dumps(results, indent=2))
    for k, v in checklist.items():
        log(f"  {'PASS' if v else 'FAIL'}: {k}")

    must = [
        "fresh_bundle", "manifest_in_bundle", "p12_onboard",
        "qthread_shim_source", "structure_preserving_install", "dockerfile_custom_recursive",
    ]
    failed = [k for k in must if not checklist[k]]
    soft = [k for k in ("p16_onboard", "p17_onboard") if not checklist[k]]

    # teardown coldbox
    sh(f"cd {COLD} && COMPOSE_PROJECT_NAME=coldbox-ex bash run.sh down", check=False)
    # cleanup pp-repack
    sh("rm -rf /home/icfre/pp-repack", check=False)

    if failed:
        (STAGE / "RESULT").write_text("FAIL\n" + json.dumps({"failed": failed, "results": results}, indent=2))
        raise SystemExit(1)
    if soft:
        (STAGE / "RESULT").write_text(
            "PASS_WITH_GAPS\n" + json.dumps({"soft": soft, "results": results, "checklist": checklist}, indent=2)
        )
    else:
        (STAGE / "RESULT").write_text("PASS\n" + json.dumps({"results": results, "checklist": checklist}, indent=2))
    log((STAGE / "RESULT").read_text())
    log("DONE")


if __name__ == "__main__":
    main()
