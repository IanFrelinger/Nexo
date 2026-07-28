#!/usr/bin/env bash
# Restore a backup created by backup-deployment-volumes.sh while the stack is down.
set -euo pipefail

ARCHIVE="${1:?usage: $0 backup.tar.gz}"
test -f "$ARCHIVE"
tmp="$(mktemp -d)"
trap 'rm -rf "$tmp"' EXIT
tar -C "$tmp" -xzf "$ARCHIVE"
while IFS= read -r volume; do
  docker volume create "$volume" >/dev/null
  docker run --rm -v "$volume":/data -v "$tmp/$volume":/backup:ro ubuntu:26.04 \
    sh -c 'rm -rf /data/* /data/.[!.]* /data/..?* 2>/dev/null || true; tar -C /data -xf /backup/data.tar'
  echo "Restored $volume"
done <"$tmp/volumes.txt"
