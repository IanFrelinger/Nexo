#!/usr/bin/env python3
from pathlib import Path
import os
import signal
import subprocess
import time

# Kill prior close-gaps
for line in subprocess.run(["pgrep", "-af", "close-gaps.py"], text=True, capture_output=True).stdout.splitlines():
    try:
        pid = int(line.split()[0])
        if pid != os.getpid():
            os.kill(pid, signal.SIGTERM)
    except Exception:
        pass
time.sleep(2)

src = Path("/mnt/c/Users/icfre/Downloads/Nexo/.tmp-close-gaps.py")
dst = Path("/home/icfre/deploy-gap-proof/close-gaps.py")
dst.write_bytes(src.read_bytes().replace(b"\r\n", b"\n"))
dst.chmod(0o755)
Path("/home/icfre/deploy-gap-proof/close-gaps.log").write_text("")
out = Path("/home/icfre/deploy-gap-proof/close-gaps.nohup.out")
with out.open("w") as f:
    p = subprocess.Popen(
        ["python3", str(dst)],
        cwd="/home/icfre/deploy-gap-proof",
        stdout=f,
        stderr=subprocess.STDOUT,
        start_new_session=True,
    )
print("PID", p.pid)
time.sleep(4)
print(Path("/home/icfre/deploy-gap-proof/close-gaps.log").read_text()[-1500:])
