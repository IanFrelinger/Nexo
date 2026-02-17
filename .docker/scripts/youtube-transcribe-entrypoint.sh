#!/bin/sh
# Extract audio from VIDEO_URL and transcribe with Whisper tiny. Output transcript to stdout.
set -e
VIDEO_URL="${VIDEO_URL:-}"
if [ -z "$VIDEO_URL" ]; then
  echo "Set VIDEO_URL env var" >&2
  exit 1
fi

cd /workspace
yt-dlp -x --audio-format wav -o audio.wav --no-playlist "$VIDEO_URL"
whisper audio.wav --model "${WHISPER_MODEL:-tiny}" --output_format txt --output_dir /workspace
cat /workspace/audio.txt 2>/dev/null || true
