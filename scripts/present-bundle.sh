#!/usr/bin/env bash
set -euo pipefail

ART="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
REVIEW_DIR="$ART/Artifacts/review"
PRESENTATION_DIR="$ART/Presentation"

echo "🎬 Creating presentation bundle..."

# Create presentation directory
mkdir -p "$PRESENTATION_DIR"

# Copy review results
if [[ -d "$REVIEW_DIR" ]]; then
  echo "📊 Copying review results..."
  cp -r "$REVIEW_DIR" "$PRESENTATION_DIR/"
fi

# Copy latest smoke test results
if [[ -f "$ART/playmode-smoke-real.json" ]]; then
  echo "📈 Copying smoke test results..."
  cp "$ART/playmode-smoke-real.json" "$PRESENTATION_DIR/"
  cp "$ART/playmode-smoke-real.junit.xml" "$PRESENTATION_DIR/" 2>/dev/null || true
  cp "$ART/functional_smoke_real.json" "$PRESENTATION_DIR/" 2>/dev/null || true
fi

# Copy latest generated scenes
if [[ -d "$ART/DirectorStudioUnity/Assets/GeneratedScenes" ]]; then
  echo "🎮 Copying generated scenes..."
  mkdir -p "$PRESENTATION_DIR/scenes"
  cp "$ART/DirectorStudioUnity/Assets/GeneratedScenes"/*.unity "$PRESENTATION_DIR/scenes/" 2>/dev/null || true
fi

# Create presentation summary
cat > "$PRESENTATION_DIR/README.md" << EOF
# DirectorStudio Presentation Bundle

Generated: $(date)

## Contents

- \`review/\` - Review mode results and summaries
- \`scenes/\` - Generated Unity scenes
- \`playmode-smoke-real.*\` - Latest smoke test results
- \`functional_smoke_real.json\` - Functional validation results

## Quick Start

1. Open Unity project: \`DirectorStudioUnity/\`
2. Load any scene from \`scenes/\` folder
3. Press Play to experience the generated content

## Review Results

Check \`review/summary.json\` for detailed results across all canary seeds.

## Quality Gates

- Engagement Score: ≥70/100
- Gameplay Quality: ≥80/100
- All functional validations must pass

## Fallback Content

If any phase failed, fallback content was used to ensure a playable experience.
Check individual run logs for details on what was substituted.
EOF

echo "✅ Presentation bundle created at: $PRESENTATION_DIR"
echo "📁 Contents:"
ls -la "$PRESENTATION_DIR"

echo "🎯 Ready for presentation!"
