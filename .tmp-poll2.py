#!/usr/bin/env python3
import subprocess
from pathlib import Path

def sh(cmd):
    p = subprocess.run(cmd, shell=True, text=True, capture_output=True)
    print(p.stdout or "", end="")
    if p.stderr and p.returncode != 0:
        print(p.stderr, end="")

sh("ps -p 19139 -o pid,etime,cmd 2>&1 || echo curl_gone")
sh("docker stats --no-stream --format '{{.Name}} {{.CPUPerc}} {{.MemUsage}}' portability-ex-ollama-1 portability-ex-dep-extract-agent-1 2>&1")
sh("docker logs portability-ex-dep-extract-agent-1 2>&1 | grep -E 'onboard|Drafting|Ollama|oracle|adapt|Install|scaffold|error|FAIL|ok=' | tail -40")
sh("docker logs portability-ex-ollama-1 2>&1 | tail -15")
out = Path("/home/icfre/portability-proof/onboard.json.out")
print("onboard.out", out.stat().st_size if out.exists() else "missing")
