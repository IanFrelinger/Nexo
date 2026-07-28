#!/usr/bin/env python3
from pathlib import Path
import subprocess
import time

src = Path("/mnt/c/Users/icfre/Downloads/Nexo/.tmp-finish-p16-p17.py")
dst = Path("/home/icfre/deploy-gap-proof/finish-p16-p17.py")
dst.write_bytes(src.read_bytes().replace(b"\r\n", b"\n"))
dst.chmod(0o755)
subprocess.run(["tmux", "kill-session", "-t", "dgresume"], check=False)
subprocess.run(["tmux", "kill-session", "-t", "dgfin"], check=False)
subprocess.run(["pkill", "-f", "deploy-gap-proof/resume.py"], check=False)
subprocess.run(["pkill", "-f", "15241/api/onboard"], check=False)
time.sleep(1)
subprocess.run(
    ["tmux", "new-session", "-d", "-s", "dgfin", f"python3 {dst}; echo EXIT:$?; sleep 120"],
    check=True,
)
time.sleep(3)
subprocess.run(["tmux", "capture-pane", "-t", "dgfin", "-p"])
