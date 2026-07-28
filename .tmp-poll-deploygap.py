#!/usr/bin/env python3
from pathlib import Path
import subprocess

subprocess.run(["tmux", "capture-pane", "-t", "deploygap", "-p"], check=False)
print("---LOG TAIL---")
log = Path("/home/icfre/deploy-gap-proof/execute.log")
if log.exists():
    text = log.read_text(errors="replace")
    print(text[-4000:])
    print(f"(log bytes={len(text)})")
else:
    print("no log")
print("---RESULT---")
r = Path("/home/icfre/deploy-gap-proof/RESULT")
print(r.read_text() if r.exists() else "none")
subprocess.run(["tmux", "ls"], check=False)
