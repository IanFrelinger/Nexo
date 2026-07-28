#!/usr/bin/env python3
"""Resume after WSL crash: pack with --no-model + copy existing model tar, then cold-box."""
from __future__ import annotations

import json
import os
import shutil
import subprocess
import sys
import time
from pathlib import Path

NEXO = Path("/mnt/c/Users/icfre/Downloads/Nexo")
POCO = Path("/home/icfre/work/poco")
WORK = Path("/home/icfre/work")
DEMO = WORK / "proprietary-demo"
SAMPLES = WORK / "samples"
STAGE = Path("/home/icfre/deploy-gap-proof")
LOG = STAGE / "execute-resume.log"
BUNDLE = Path("/home/icfre/parser-pipeline-bundle.tar.gz")
COLD = Path("/home/icfre/cold-box-extractor")
EXISTING_MODEL = Path("/home/icfre/portability-proof/extractor/ollama-model.tar")


def log(msg: str) -> None:
    line = msg if msg.endswith("\n") else msg + "\n"
    sys.stdout.write(line)
    sys.stdout.flush()
    with LOG.open("a", encoding="utf-8") as f:
        f.write(line)


def sh(cmd: str, check: bool = True, timeout: int | None = None) -> subprocess.CompletedProcess:
    log(f"$ {cmd}")
    p = subprocess.run(cmd, shell=True, text=True, capture_output=True, timeout=timeout)
    if p.stdout:
        log(p.stdout.rstrip())
    if p.stderr:
        log(p.stderr.rstrip())
    if check and p.returncode != 0:
        raise SystemExit(f"FAIL rc={p.returncode}: {cmd}")
    return p


def cleanup() -> None:
    log("=== cleanup partial packs ===")
    for p in Path("/home/icfre").glob("pp-bundle.*"):
        log(f"rm {p}")
        shutil.rmtree(p, ignore_errors=True)
    # Keep STALE for now but prefer deleting if host pressure — it's inside ext4 with room
    sh("docker images --format '{{.Repository}}:{{.Tag}} {{.ID}}' | grep -E 'nexo-dep-extract|dep-extract:' | head", check=False)


def ensure_images() -> None:
    log("=== ensure agent/tool images ===")
    p = sh("docker image inspect ghcr.io/ianfrelinger/nexo-dep-extract-agent:latest", check=False)
    if p.returncode != 0:
        sh(
            f"docker build -f {NEXO}/docker/dep-extract-gui/Dockerfile "
            f"-t nexo-dep-extract-agent:latest "
            f"-t ghcr.io/ianfrelinger/nexo-dep-extract-agent:latest {NEXO}",
            timeout=1800,
        )
    p = sh("docker image inspect dep-extract:latest", check=False)
    if p.returncode != 0:
        sh(
            f"docker build -t dep-extract:latest "
            f"-t ghcr.io/ianfrelinger/nexo-dep-extract-tool:latest "
            f"{NEXO}/tools/dep_extract",
            timeout=600,
        )
    else:
        sh("docker tag dep-extract:latest ghcr.io/ianfrelinger/nexo-dep-extract-tool:latest", check=False)


def sync_fixtures() -> None:
    log("=== sync fixtures ===")
    src_root = NEXO / "src/Nexo.Bricks.DepExtract.Tests/Fixtures/e2e/projects"
    for name in ("P16_NestedRelIncludes", "P17_QThreadPeripheral", "P12_InternalDeps"):
        src = src_root / name
        dst = DEMO / name
        if not src.exists():
            continue
        if dst.exists():
            shutil.rmtree(dst)
        shutil.copytree(src, dst)
        log(f"copied {name}")
    SAMPLES.mkdir(parents=True, exist_ok=True)
    dest_sample = SAMPLES / "mock.evtq"
    if not dest_sample.exists():
        for cand in (
            Path("/home/icfre/portability-proof/extractor/samples/mock.evtq"),
            WORK / "samples/mock-telemetry.evtq",
        ):
            if cand.exists() and cand.resolve() != dest_sample.resolve():
                shutil.copy2(cand, dest_sample)
                log(f"sample {cand}")
                break
    else:
        log(f"sample already present {dest_sample}")
    # Dockerfile.custom sync
    win = Path("/mnt/c/Users/icfre/Poco/Dockerfile.custom")
    if win.exists() and POCO.exists():
        (POCO / "Dockerfile.custom").write_bytes(win.read_bytes().replace(b"\r\n", b"\n"))


def pack() -> None:
    log("=== pack --no-model + attach existing model tar ===")
    if not EXISTING_MODEL.exists():
        raise SystemExit(f"missing model tar {EXISTING_MODEL}")
    if BUNDLE.exists():
        BUNDLE.unlink()
    env = os.environ.copy()
    env["POCO_CONTEXT"] = str(POCO)
    env["BUNDLE_OUT"] = str(BUNDLE)
    # Pack images only
    cmd = f"bash {NEXO}/scripts/pack-parser-pipeline-bundle.sh latest --no-model"
    log(f"$ {cmd}")
    p = subprocess.run(cmd, shell=True, text=True, env=env, cwd=str(NEXO), capture_output=True)
    log((p.stdout or "")[-3000:])
    log((p.stderr or "")[-1000:])
    if p.returncode != 0:
        raise SystemExit("pack --no-model failed")

    # Inject model into the gzip bundle by re-packing directory
    # pack writes a .tar.gz — extract, copy model, recompress
    work = Path("/home/icfre/pp-repack")
    if work.exists():
        shutil.rmtree(work)
    work.mkdir()
    sh(f"tar xzf {BUNDLE} -C {work}", timeout=600)
    log(f"copying model {EXISTING_MODEL} ({EXISTING_MODEL.stat().st_size} bytes)")
    shutil.copy2(EXISTING_MODEL, work / "ollama-model.tar")
    # Update manifest hasModelTar
    man = work / "BUNDLE_MANIFEST.json"
    if man.exists():
        m = json.loads(man.read_text())
        m["hasModelTar"] = True
        m["modelTarSource"] = "portability-proof/extractor/ollama-model.tar"
        man.write_text(json.dumps(m, indent=2) + "\n")
    tmp = Path("/home/icfre/parser-pipeline-bundle.new.tar.gz")
    if tmp.exists():
        tmp.unlink()
    sh(f"tar -C {work} -cf - . | gzip > {tmp}", timeout=1800)
    tmp.replace(BUNDLE)
    shutil.rmtree(work, ignore_errors=True)
    log(f"final bundle {BUNDLE} size={BUNDLE.stat().st_size}")


def cold_box() -> dict:
    log("=== cold-box proof ===")
    if COLD.exists():
        shutil.rmtree(COLD)
    COLD.mkdir(parents=True)
    sh(f"tar xzf {BUNDLE} -C {COLD}", timeout=900)

    # Force load from bundle images (rmi first so we don't accidentally use host tags only)
    sh("docker rmi ghcr.io/ianfrelinger/nexo-dep-extract-agent:latest nexo-dep-extract-agent:latest || true", check=False)
    sh("docker rmi ghcr.io/ianfrelinger/nexo-dep-extract-tool:latest dep-extract:latest || true", check=False)

    for name in ("P12_InternalDeps", "P16_NestedRelIncludes", "P17_QThreadPeripheral"):
        shutil.copytree(DEMO / name, COLD / "proprietary-demo" / name, dirs_exist_ok=True)
    (COLD / "samples").mkdir(exist_ok=True)
    shutil.copy2(SAMPLES / "mock.evtq", COLD / "samples" / "mock.evtq")

    env_proj = "coldbox-ex"
    cmd = (
        f"cd {COLD} && COMPOSE_PROJECT_NAME={env_proj} AGENT_PORT=15241 EVTX_HOST_PORT=18091 "
        f"bash run.sh --poco {POCO}"
    )
    log(f"$ {cmd}")
    p = subprocess.run(cmd, shell=True, text=True, capture_output=True, timeout=2400)
    log((p.stdout or "")[-4000:])
    log((p.stderr or "")[-2000:])
    if p.returncode != 0:
        raise SystemExit("cold-box up failed")

    p = subprocess.run(
        f"cd {COLD} && COMPOSE_PROJECT_NAME={env_proj} AGENT_PORT=15241 bash run.sh pack-check",
        shell=True, text=True, capture_output=True, timeout=120,
    )
    log(p.stdout or "")
    log(p.stderr or "")
    if p.returncode != 0:
        raise SystemExit("pack-check failed")

    results = {}
    for name in ("P12_InternalDeps", "P16_NestedRelIncludes", "P17_QThreadPeripheral"):
        src = COLD / "proprietary-demo" / name
        sample = COLD / "samples" / "mock.evtq"
        cmd = (
            f"cd {COLD} && COMPOSE_PROJECT_NAME={env_proj} AGENT_PORT=15241 "
            f"bash run.sh onboard --src {src} --sample {sample} --min-events 5"
        )
        log(f"=== onboard {name} ===")
        p = subprocess.run(cmd, shell=True, text=True, capture_output=True, timeout=1200)
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

    subprocess.run(
        f"cd {COLD} && COMPOSE_PROJECT_NAME={env_proj} bash run.sh down",
        shell=True, check=False,
    )
    (STAGE / "cold-results.json").write_text(json.dumps(results, indent=2))
    if not results.get("P12_InternalDeps", {}).get("ok"):
        raise SystemExit("P12 cold onboard failed")
    return results


def checklist(results: dict) -> None:
    items = {
        "fresh_bundle": BUNDLE.exists() and BUNDLE.stat().st_size > 1_000_000_000,
        "manifest_in_bundle": (COLD / "BUNDLE_MANIFEST.json").exists(),
        "pack_check_script": "pack-check" in (COLD / "run.sh").read_text(errors="replace"),
        "p12_onboard": bool(results.get("P12_InternalDeps", {}).get("ok")),
        "p16_onboard": bool(results.get("P16_NestedRelIncludes", {}).get("ok")),
        "p17_onboard": bool(results.get("P17_QThreadPeripheral", {}).get("ok")),
        "qthread_shim_source": "QThreadShim" in (NEXO / "src/Nexo.Bricks.DepExtract/QtHeaderShims.cs").read_text(errors="replace"),
        "structure_preserving_install": "writtenRels" in (NEXO / "src/Nexo.Bricks.DepExtract.Gui/InstallExecutor.cs").read_text(errors="replace"),
        "dockerfile_custom_recursive": "find common -type d" in (POCO / "Dockerfile.custom").read_text(errors="replace"),
    }
    (STAGE / "checklist.json").write_text(json.dumps(items, indent=2))
    for k, v in items.items():
        log(f"  {'PASS' if v else 'FAIL'}: {k}")
    must = [
        "fresh_bundle", "manifest_in_bundle", "pack_check_script", "p12_onboard",
        "qthread_shim_source", "structure_preserving_install", "dockerfile_custom_recursive",
    ]
    failed = [k for k in must if not items[k]]
    soft = [k for k in ("p16_onboard", "p17_onboard") if not items[k]]
    if failed:
        (STAGE / "RESULT").write_text("FAIL\n" + json.dumps(failed))
        raise SystemExit(f"checklist failed: {failed}")
    if soft:
        (STAGE / "RESULT").write_text("PASS_WITH_GAPS\n" + json.dumps({"soft": soft, "results": results}, indent=2))
    else:
        (STAGE / "RESULT").write_text("PASS\n" + json.dumps(results, indent=2))
    log((STAGE / "RESULT").read_text())


def main() -> None:
    STAGE.mkdir(parents=True, exist_ok=True)
    LOG.write_text(f"resume {time.strftime('%Y-%m-%dT%H:%M:%S')}\n")
    cleanup()
    ensure_images()
    sync_fixtures()
    pack()
    results = cold_box()
    checklist(results)
    log("DONE")


if __name__ == "__main__":
    main()
