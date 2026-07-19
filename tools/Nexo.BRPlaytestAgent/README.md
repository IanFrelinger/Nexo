# Nexo BR Playtest Agent

Runs the deterministic Weapon Lab playthrough and writes reports under
`.nexo/playtest/br-weapon-lab`.

```bash
# Semantic playthrough with camera-native screenshots
dotnet run --project tools/Nexo.BRPlaytestAgent --no-build

# Windowed playthrough recorded and finalized as MP4
dotnet run --project tools/Nexo.BRPlaytestAgent -- --record-video

# Real macOS keyboard/mouse playthrough
dotnet run --project tools/Nexo.BRPlaytestAgent -- --virtual-player
```

`--record-video` enables the executable's camera-native AVFoundation encoder.
It records the final post-processed gameplay render at 1920×1080 and cannot
include desktop windows or window chrome. Screen Recording and Accessibility
permission are required only by `--virtual-player`, which drives real macOS
input and captures desktop evidence.

Output:

```text
.nexo/playtest/br-weapon-lab/videos/complete-weapon-showcase.mp4
```

## Unattended Google Drive upload

When both environment variables are set, finished MP4 captures are uploaded
automatically after each playtest cycle through the open-core
`IArtifactSink` / `GoogleDriveArtifactSink` adapters. No browser login is
required; this uses a Google Cloud service account.

```bash
export NEXO_GDRIVE_CREDENTIALS="$HOME/.config/nexo/gdrive-service-account.json"
export NEXO_GDRIVE_FOLDER_ID="your-shared-folder-id"
export NEXO_GDRIVE_SHARE_ANYONE=true   # optional, default true
dotnet run --project tools/Nexo.BRPlaytestAgent -- --record-video --daemon
```

Setup:

1. Create a Google Cloud project and enable the **Google Drive API**.
2. Create a **service account** and download its JSON key.
3. In Google Drive, create or pick a folder and share it with the service
   account email (`...@....iam.gserviceaccount.com`) as **Editor**.
4. Copy the folder ID from the Drive URL:
   `https://drive.google.com/drive/folders/<folder-id>`

The playtest report is patched with `driveUploads` entries containing
`webViewLink` values. Upload failures fail the cycle so unattended runs do not
silently drop captures.

## Built-player capture mode

The macOS game executable also contains its own camera-native capture mode. It
does not require the Nexo playtest agent and never records the desktop, window
chrome, or other applications:

```bash
"/path/to/NexoBRWeaponLab.app/Contents/MacOS/NexoBRWeaponLab" \
  --br-capture \
  --br-capture-duration 60 \
  --br-capture-output "$HOME/Movies/weapon-lab.mp4" \
  --br-capture-auto-quit
```

Arguments:

| Argument | Meaning |
|---|---|
| `--br-capture` | Enable the debug capture overlay and recorder |
| `--br-capture-duration N` | Bounded recording duration in seconds (`5…3600`) |
| `--br-capture-output PATH` | Final MP4 path; `.mp4` is added if omitted |
| `--br-capture-auto-quit` | Exit after MP4 conversion completes |
| `--br-capture-width N` | Output width; defaults to `1920` |
| `--br-capture-height N` | Output height; defaults to `1080` |
| `--br-capture-fps N` | Output frame rate (`15…60`); defaults to `30` |

The final post-processed gameplay camera is encoded directly to H.264 MP4 by
AVFoundation. Covering or minimizing the game does not affect the captured
frames. Closing the game during recording stops frame submission and defers
shutdown only until the MP4 index is finalized.
