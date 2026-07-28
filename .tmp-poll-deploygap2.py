#!/usr/bin/env python3
import subprocess
from pathlib import Path

def sh(cmd):
    p = subprocess.run(cmd, shell=True, text=True, capture_output=True)
    print(p.stdout or "", end="")
    if p.stderr:
        print(p.stderr, end="")

sh("ps -ef | grep -E 'pack-parser|pigz|gzip|docker save|tar -C|execute.py' | grep -v grep")
sh("ls -lh /home/icfre/parser-pipeline-bundle.tar.gz /home/icfre/parser-pipeline-bundle.STALE-pre-gapfix.tar.gz 2>&1")
sh("ls -lh /home/icfre/pp-bundle.* 2>&1 | head")
sh("du -sh /home/icfre/pp-bundle.* 2>/dev/null | head")
sh("tmux capture-pane -t deploygap -p | tail -15")
print("RESULT", Path("/home/icfre/deploy-gap-proof/RESULT").read_text() if Path("/home/icfre/deploy-gap-proof/RESULT").exists() else "none")
