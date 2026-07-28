#!/usr/bin/env python3
import subprocess
import os
from pathlib import Path

def run(cmd):
    print("$", cmd)
    p = subprocess.run(cmd, shell=True, text=True, capture_output=True)
    print(p.stdout)
    if p.stderr:
        print(p.stderr)
    return p.returncode

run("ps -ef | grep -E 'curl|finish.sh' | grep -v grep")
run("ls -la /home/icfre/portability-proof/extractor/proprietary-demo/P12_InternalDeps /home/icfre/portability-proof/extractor/samples/mock.evtq")
run("cat /home/icfre/portability-proof/onboard.json")
print("--- direct curl 20s ---")
p = subprocess.run(
    [
        "curl", "-sS", "--max-time", "20", "-w", "\nHTTP:%{http_code} time:%{time_total}\n",
        "-X", "POST", "http://127.0.0.1:15239/api/onboard",
        "-H", "Content-Type: application/json",
        "-d", "@/home/icfre/portability-proof/onboard.json",
        "-o", "/tmp/onboard-probe.out",
    ],
    text=True,
    capture_output=True,
)
print("rc", p.returncode)
print(p.stdout)
print(p.stderr)
out = Path("/tmp/onboard-probe.out")
print("out bytes", out.stat().st_size if out.exists() else 0)
if out.exists() and out.stat().st_size:
    print(out.read_text()[:1500])
run("docker logs --tail 40 portability-ex-dep-extract-agent-1 2>&1 | tail -40")
