#!/usr/bin/env python3
from pathlib import Path
import subprocess
import time

src = Path("/mnt/c/Users/icfre/Downloads/Nexo/.tmp-finish-portability.sh")
dst = Path("/home/icfre/portability-proof/finish.sh")
data = src.read_bytes().replace(b"\r\n", b"\n").replace(b"\r", b"\n")
dst.write_bytes(data)
dst.chmod(0o755)
print("wrote", dst, "bytes", len(data), "crlf_gone", b"\r" not in data)

log = Path("/home/icfre/portability-proof/finish.log")
if log.exists():
    log.unlink()

subprocess.run(["tmux", "kill-session", "-t", "portfin"], check=False)
cmd = "bash /home/icfre/portability-proof/finish.sh; echo EXIT:$?; sleep 300"
subprocess.run(["tmux", "new-session", "-d", "-s", "portfin", cmd], check=True)
time.sleep(4)
print("--- tmux ---")
subprocess.run(["tmux", "ls"])
subprocess.run(["tmux", "capture-pane", "-t", "portfin", "-p"])
print("--- log ---")
if log.exists():
    print(log.read_text()[-3000:])
else:
    print("(no log yet)")
