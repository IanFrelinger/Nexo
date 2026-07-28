#!/usr/bin/env python3
from pathlib import Path
import subprocess
import time

src = Path("/mnt/c/Users/icfre/Downloads/Nexo/.tmp-resume-deploygap.py")
stage = Path("/home/icfre/deploy-gap-proof")
stage.mkdir(parents=True, exist_ok=True)
dst = stage / "resume.py"
dst.write_bytes(src.read_bytes().replace(b"\r\n", b"\n").replace(b"\r", b"\n"))
dst.chmod(0o755)

# Drop multi-GB leftovers that filled the host during the crash
for p in Path("/home/icfre").glob("pp-bundle.*"):
    subprocess.run(["rm", "-rf", str(p)], check=False)
    print("removed", p)
stale = Path("/home/icfre/parser-pipeline-bundle.STALE-pre-gapfix.tar.gz")
if stale.exists():
    stale.unlink()
    print("removed stale bundle")

subprocess.run(["tmux", "kill-session", "-t", "deploygap"], check=False)
subprocess.run(["tmux", "kill-session", "-t", "dgresume"], check=False)
cmd = f"python3 {dst}; echo EXIT:$?; sleep 180"
subprocess.run(["tmux", "new-session", "-d", "-s", "dgresume", cmd], check=True)
time.sleep(4)
subprocess.run(["tmux", "capture-pane", "-t", "dgresume", "-p"])
print("---")
log = stage / "execute-resume.log"
print(log.read_text()[-1500:] if log.exists() else "no log")
