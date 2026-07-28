#!/usr/bin/env python3
from pathlib import Path
import subprocess
import time

# Normalize and stage scripts on Linux FS
for name in (".tmp-rebuild-and-reonboard.py",):
    src = Path("/mnt/c/Users/icfre/Downloads/Nexo") / name
    dst = Path("/home/icfre/portability-proof") / name.lstrip(".")
    data = src.read_bytes().replace(b"\r\n", b"\n").replace(b"\r", b"\n")
    # Fix wrong path to extract_deps in verify grep
    text = data.decode()
    text = text.replace(
        "docker run --rm --entrypoint cat dep-extract:latest /extract_deps.sh | grep -c companion || true",
        "docker run --rm --entrypoint cat dep-extract:latest /usr/local/bin/extract_deps.sh | grep -c companion || true",
    )
    dst.write_text(text)
    dst.chmod(0o755)
    print("staged", dst)

subprocess.run(["tmux", "kill-session", "-t", "portreb"], check=False)
subprocess.run(["tmux", "kill-session", "-t", "portfin"], check=False)
subprocess.run(["pkill", "-f", "curl.*15239/api/onboard"], check=False)
time.sleep(1)
cmd = "python3 /home/icfre/portability-proof/tmp-rebuild-and-reonboard.py; echo EXIT:$?; sleep 180"
subprocess.run(["tmux", "new-session", "-d", "-s", "portreb", cmd], check=True)
time.sleep(5)
subprocess.run(["tmux", "capture-pane", "-t", "portreb", "-p"])
log = Path("/home/icfre/portability-proof/finish2.log")
print("---log---")
print(log.read_text()[-2000:] if log.exists() else "(no log)")
