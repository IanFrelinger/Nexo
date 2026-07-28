#!/usr/bin/env python3
"""Kill stuck onboard, rebuild dep-extract tool with sibling cpp, re-run onboard."""
import json
import subprocess
import time
from pathlib import Path

STAGE = Path("/home/icfre/portability-proof")
EX = STAGE / "extractor"
NEXO = Path("/mnt/c/Users/icfre/Downloads/Nexo")
LOG = STAGE / "finish2.log"


def sh(cmd, check=True, timeout=None):
    print(f"$ {cmd}", flush=True)
    p = subprocess.run(cmd, shell=True, text=True, capture_output=True, timeout=timeout)
    if p.stdout:
        print(p.stdout, end="", flush=True)
    if p.stderr:
        print(p.stderr, end="", flush=True)
    if check and p.returncode != 0:
        raise SystemExit(f"cmd failed rc={p.returncode}: {cmd}")
    return p


def main():
    LOG.write_text("")
    def log(msg):
        print(msg, flush=True)
        with LOG.open("a") as f:
            f.write(msg + "\n")

    log("=== kill stuck onboard ===")
    sh("pkill -f 'curl.*15239/api/onboard' || true", check=False)
    sh("tmux kill-session -t portfin || true", check=False)
    time.sleep(1)

    log("=== rebuild dep-extract tool (sibling cpp) ===")
    sh(f"docker build -t dep-extract:latest -t ghcr.io/ianfrelinger/nexo-dep-extract-tool:latest {NEXO}/tools/dep_extract",
       timeout=600)

    # Verify sibling support is in the image
    p = sh("docker run --rm --entrypoint cat dep-extract:latest /extract_deps.sh | grep -c companion || true", check=False)
    log(f"companion mentions in image script: {(p.stdout or '').strip()}")

    # Ensure agent still has onboard
    code = sh("curl -sS -o /dev/null -w '%{http_code}' -X POST http://127.0.0.1:15239/api/onboard -H 'Content-Type: application/json' -d '{}'",
              check=False).stdout.strip()
    log(f"onboard probe http={code}")
    if code != "400":
        raise SystemExit("onboard endpoint missing again")

    req = {
        "srcDir": str(EX / "proprietary-demo/P12_InternalDeps"),
        "samples": [str(EX / "samples/mock.evtq")],
        "outlineFiles": 0,
        "minEvents": 5,
        "preferScaffold": True,
    }
    req_path = STAGE / "onboard.json"
    req_path.write_text(json.dumps(req, indent=2))
    out_path = STAGE / "onboard.json.out"
    err_path = STAGE / "onboard.err"
    out_path.write_text("")
    err_path.write_text("")

    log("=== onboard P12 preferScaffold ===")
    log(json.dumps(req, indent=2))
    t0 = time.time()
    p = subprocess.run(
        [
            "curl", "-sS", "--max-time", "900",
            "-X", "POST", "http://127.0.0.1:15239/api/onboard",
            "-H", "Content-Type: application/json",
            "-d", f"@{req_path}",
            "-o", str(out_path),
        ],
        text=True,
        capture_output=True,
        timeout=920,
    )
    err_path.write_text(p.stderr or "")
    elapsed = time.time() - t0
    log(f"curl_rc={p.returncode} elapsed={elapsed:.1f}s bytes={out_path.stat().st_size}")
    if p.stderr:
        log("stderr: " + p.stderr[:2000])

    if not out_path.stat().st_size:
        log("FAIL empty onboard response")
        sh("docker logs --tail 80 portability-ex-dep-extract-agent-1", check=False)
        raise SystemExit(1)

    d = json.loads(out_path.read_text())
    log(
        f"onboard: ok={d.get('ok')} stage={d.get('stage')} triage={d.get('triage')} "
        f"rows={d.get('oracleRows')} image={d.get('imageTag')} err={d.get('error')}"
    )
    (STAGE / "onboard.ok").write_text("1" if d.get("ok") else "0")
    if not d.get("ok"):
        log(json.dumps(d, indent=2)[:5000])
        raise SystemExit(2)

    log("=== refresh images.tar ===")
    imgs = [
        "ghcr.io/ianfrelinger/nexo-dep-extract-agent:latest",
        "ghcr.io/ianfrelinger/nexo-dep-extract-tool:latest",
        "evtx:latest",
        "ollama/ollama:latest",
        "docker:27-cli",
        "ubuntu:26.04",
    ]
    tmp = STAGE / "images-refresh.tar"
    sh("docker save " + " ".join(imgs) + f" -o {tmp}", timeout=1200)
    tmp.replace(EX / "images.tar")
    size = sh(f"du -h {EX/'images.tar'}").stdout.strip()
    log(f"PASS images.tar refreshed {size}")

    # App health
    ah = sh("curl -sS -o /dev/null -w '%{http_code}' http://127.0.0.1:28280/health", check=False).stdout.strip()
    log(f"poco-app health http={ah}")

    log("=== teardown portable stacks ===")
    sh(f"cd {EX} && COMPOSE_PROJECT_NAME=portability-ex docker compose -f docker-compose.deploy.yml --profile extract-agent down",
       check=False)
    sh(f"cd {STAGE/'app'} && COMPOSE_PROJECT_NAME=poco-app bash run.sh down", check=False)

    (STAGE / "RESULT").write_text("PASS\n")
    log("PORTABILITY PROOF: PASS")


if __name__ == "__main__":
    main()
