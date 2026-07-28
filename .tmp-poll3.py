#!/usr/bin/env python3
from pathlib import Path
import subprocess

def sh(cmd):
    p = subprocess.run(cmd, shell=True, text=True, capture_output=True)
    print(p.stdout or "", end="")
    if p.stderr:
        print(p.stderr, end="")

sh("tmux capture-pane -t portreb -p | tail -25")
print("---log---")
log = Path("/home/icfre/portability-proof/finish2.log")
print(log.read_text()[-2500:] if log.exists() else "missing")
print("---out---")
out = Path("/home/icfre/portability-proof/onboard.json.out")
print("bytes", out.stat().st_size if out.exists() else "missing")
if out.exists() and out.stat().st_size:
    print(out.read_text()[:2000])
sh("docker logs portability-ex-dep-extract-agent-1 2>&1 | grep -E 'sibling|companion|Checksum|scaffold|oracle|onboard|Drafting|Install|image|PASS|FAIL|ok' | tail -30")
