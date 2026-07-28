#!/usr/bin/env python3
import subprocess
from pathlib import Path

def sh(cmd):
    p = subprocess.run(cmd, shell=True, text=True, capture_output=True)
    print((p.stdout or "") + (p.stderr or ""), end="")

sh("ps -ef | grep -E 'resume.py|pack-parser|gzip|docker save|tar ' | grep -v grep | head -20")
sh("ls -lh /home/icfre/parser-pipeline-bundle.tar.gz /home/icfre/parser-pipeline-bundle.new.tar.gz /home/icfre/pp-bundle.*/images.tar /home/icfre/pp-repack/ollama-model.tar 2>&1 | head -20")
sh("tmux capture-pane -t dgresume -p 2>&1 | tail -20")
r = Path("/home/icfre/deploy-gap-proof/RESULT")
print("RESULT:", r.read_text() if r.exists() else "none")
log = Path("/home/icfre/deploy-gap-proof/execute-resume.log")
if log.exists():
    print("---LOG---")
    print(log.read_text()[-2500:])
