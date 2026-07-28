#!/usr/bin/env python3
import subprocess
from pathlib import Path

def sh(cmd):
    p = subprocess.run(cmd, shell=True, text=True, capture_output=True)
    print(p.stdout or "", end="")
    if p.stderr:
        print(p.stderr, end="")

sh("ps -p 19139 -o pid,etime,cmd 2>&1 || echo curl_gone")
sh("docker exec portability-ex-ollama-1 ollama list 2>&1")
sh("docker logs --tail 25 portability-ex-dep-extract-agent-1 2>&1")
out = Path("/home/icfre/portability-proof/onboard.json.out")
print("onboard.out bytes", out.stat().st_size if out.exists() else "missing")
log = Path("/home/icfre/portability-proof/finish.log")
print("--- finish.log tail ---")
print(log.read_text()[-2000:] if log.exists() else "missing")
