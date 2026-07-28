#!/usr/bin/env python3
from pathlib import Path
import subprocess
import time

src = Path("/mnt/c/Users/icfre/Downloads/Nexo/.tmp-execute-deploy-gaps.py")
stage = Path("/home/icfre/deploy-gap-proof")
stage.mkdir(parents=True, exist_ok=True)
dst = stage / "execute.py"
dst.write_bytes(src.read_bytes().replace(b"\r\n", b"\n").replace(b"\r", b"\n"))
dst.chmod(0o755)

# Keep work/poco Dockerfile.custom in sync with Windows Poco edit
poco_win = Path("/mnt/c/Users/icfre/Poco/Dockerfile.custom")
poco_work = Path("/home/icfre/work/poco/Dockerfile.custom")
if poco_win.exists() and poco_work.exists():
    poco_work.write_bytes(poco_win.read_bytes().replace(b"\r\n", b"\n"))
    print("synced Dockerfile.custom → work/poco")

subprocess.run(["tmux", "kill-session", "-t", "deploygap"], check=False)
cmd = f"python3 {dst}; echo EXIT:$?; sleep 180"
subprocess.run(["tmux", "new-session", "-d", "-s", "deploygap", cmd], check=True)
time.sleep(3)
subprocess.run(["tmux", "capture-pane", "-t", "deploygap", "-p"])
print("---log---")
log = stage / "execute.log"
print(log.read_text()[-2000:] if log.exists() else "(no log yet)")
