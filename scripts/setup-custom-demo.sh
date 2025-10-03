#!/usr/bin/env bash
set -euo pipefail

# Setup custom demo environment with assets and configuration
# Usage: ./scripts/setup-custom-demo.sh [demo-name]

DEMO_NAME="${1:-CyberpunkStealth}"
DEMO_DIR="demos/$DEMO_NAME"

echo "🎮 Setting up Custom Demo: $DEMO_NAME"
echo "====================================="

# Create demo directory structure
mkdir -p "$DEMO_DIR"/{config,assets,scripts,output}

echo "📁 Created demo structure:"
echo "  - $DEMO_DIR/config/     (configuration files)"
echo "  - $DEMO_DIR/assets/     (custom assets)"
echo "  - $DEMO_DIR/scripts/    (demo-specific scripts)"
echo "  - $DEMO_DIR/output/     (generated content)"
echo ""

# Create demo-specific pipeline config
cat > "$DEMO_DIR/config/pipeline.json" << 'EOF'
{
  "name": "CyberpunkStealthDemo",
  "version": "1.0.0",
  "seed": 42,
  "prompt": "A cyberpunk stealth game with neon-lit corridors, hacking terminals, and robotic enemies. The player must navigate through a corporate facility, hack security systems, and avoid detection while collecting data drives. Include atmospheric lighting, particle effects, and a futuristic UI.",
  "artifacts": "output",
  "phases": [
    { "token": "Plan",      "type": "PlanPhase",      "cache": true,  "timeoutMs": 2000 },
    { "token": "Layout",    "type": "LayoutPhase",    "cache": true,  "timeoutMs": 3000 },
    { "token": "Placement", "type": "PlacementPhase", "cache": true,  "timeoutMs": 3000 },
    { "token": "Content",   "type": "ContentPhase",   "cache": false, "timeoutMs": 5000 },
    { "token": "Validate",  "type": "ValidatePhase",  "cache": true,  "timeoutMs": 2000 }
  ],
  "adapters": "adapters.json",
  "acceptance": "Assets/NexoDirectorStudio/Acceptance/Default.asset",
  "scenarios": ["Assets/NexoDirectorStudio/Scenarios/Smoke.asset"],
  "review": {
    "mode": "always-green",
    "canarySeeds": [42, 1337, 2024],
    "failSoft": true,
    "minEngagement": 75,
    "minGameplayQuality": 85
  }
}
EOF

# Create demo-specific adapters config
cat > "$DEMO_DIR/config/adapters.json" << 'EOF'
{
  "ollama": { 
    "endpoint": "http://localhost:11434", 
    "model": "llama3", 
    "timeoutMs": 60000,
    "systemPrompt": "You are a cyberpunk game designer. Create immersive, atmospheric game content with neon aesthetics, hacking mechanics, and stealth gameplay."
  },
  "comfy":  { 
    "endpoint": "http://127.0.0.1:8188",  
    "workflow": "cyberpunk_textures.json",
    "style": "cyberpunk",
    "colorPalette": ["#1a1a2e", "#16213e", "#0f3460", "#00ffff", "#ff00ff"]
  },
  "piper":  { 
    "voice": "en_US-amy-medium",
    "effects": ["reverb", "distortion"],
    "style": "robotic_futuristic"
  }
}
EOF

# Create demo runner script
cat > "$DEMO_DIR/scripts/run-demo.sh" << 'EOF'
#!/usr/bin/env bash
set -euo pipefail

# Run the custom demo
# Usage: ./scripts/run-demo.sh

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DEMO_DIR="$(dirname "$(dirname "$SCRIPT_DIR")")"
CONFIG_DIR="$DEMO_DIR/config"
OUTPUT_DIR="$DEMO_DIR/output"

echo "🎮 Running $DEMO_NAME Demo"
echo "========================="

# Copy configs to project root
cp "$CONFIG_DIR/pipeline.json" "$(dirname "$DEMO_DIR")/../nexo.pipeline.json"
cp "$CONFIG_DIR/adapters.json" "$(dirname "$DEMO_DIR")/../nexo.adapters.json"

# Run the demo
cd "$(dirname "$DEMO_DIR")/.."
./scripts/run-custom-demo.sh nexo.pipeline.json

# Copy results back to demo output
cp -r Artifacts/* "$OUTPUT_DIR/" 2>/dev/null || true
cp playmode-*.junit.xml "$OUTPUT_DIR/" 2>/dev/null || true
cp review_summary.json "$OUTPUT_DIR/" 2>/dev/null || true
cp CUSTOM_DEMO_SUMMARY.md "$OUTPUT_DIR/" 2>/dev/null || true

echo "✅ Demo complete! Check $OUTPUT_DIR/ for results."
EOF

chmod +x "$DEMO_DIR/scripts/run-demo.sh"

# Create asset templates
mkdir -p "$DEMO_DIR/assets"/{materials,prefabs,audio,textures}

cat > "$DEMO_DIR/assets/materials/cyberpunk-materials.json" << 'EOF'
{
  "materials": [
    {
      "name": "CyberpunkWall",
      "type": "Material",
      "properties": {
        "albedo": "#1a1a2e",
        "emission": "#16213e",
        "metallic": 0.8,
        "smoothness": 0.9
      }
    },
    {
      "name": "NeonAccent",
      "type": "Material", 
      "properties": {
        "albedo": "#00ffff",
        "emission": "#00ffff",
        "metallic": 0.0,
        "smoothness": 1.0
      }
    },
    {
      "name": "MetalGrating",
      "type": "Material",
      "properties": {
        "albedo": "#0f3460",
        "emission": "#00ffff",
        "metallic": 0.9,
        "smoothness": 0.7
      }
    }
  ]
}
EOF

cat > "$DEMO_DIR/assets/prefabs/cyberpunk-prefabs.json" << 'EOF'
{
  "prefabs": [
    {
      "name": "HackingTerminal",
      "type": "Prefab",
      "components": ["HackingInterface", "DataCollector", "SecurityBreach"],
      "materials": ["NeonAccent"],
      "description": "Interactive terminal for hacking security systems"
    },
    {
      "name": "SecurityDrone",
      "type": "Prefab", 
      "components": ["PatrolAI", "DetectionSystem", "AlarmTrigger"],
      "materials": ["CyberpunkWall"],
      "description": "Patrolling security drone with detection capabilities"
    },
    {
      "name": "DataDrive",
      "type": "Prefab",
      "components": ["Collectible", "DataStorage"],
      "materials": ["NeonAccent"],
      "description": "Collectible data drive containing mission-critical information"
    }
  ]
}
EOF

# Create README for the demo
cat > "$DEMO_DIR/README.md" << EOF
# $DEMO_NAME Demo

A custom game demo created with the Nexo framework featuring cyberpunk stealth gameplay.

## Features

- **Genre**: Cyberpunk Stealth
- **Theme**: Corporate infiltration with hacking mechanics
- **Assets**: Custom neon-lit materials and futuristic prefabs
- **AI Integration**: Ollama LLM, ComfyUI textures, Piper TTS

## Quick Start

\`\`\`bash
# Run the demo
./scripts/run-demo.sh

# Check results
ls output/
\`\`\`

## Configuration

- **Pipeline**: \`config/pipeline.json\`
- **Adapters**: \`config/adapters.json\`
- **Assets**: \`assets/\`

## Generated Content

The demo will generate:
- Game plan and layout
- Custom cyberpunk assets
- Interactive terminals and security systems
- Atmospheric audio and visual effects
- JUnit test results and validation reports

## Customization

Edit the configuration files to modify:
- Game prompt and theme
- Asset generation parameters
- AI model settings
- Review and validation criteria
EOF

echo "✅ Demo setup complete!"
echo ""
echo "📁 Demo structure:"
echo "  $DEMO_DIR/"
echo "    ├── config/          (pipeline.json, adapters.json)"
echo "    ├── assets/          (custom materials, prefabs, audio)"
echo "    ├── scripts/         (run-demo.sh)"
echo "    ├── output/          (generated content)"
echo "    └── README.md        (documentation)"
echo ""
echo "🚀 To run the demo:"
echo "  cd $DEMO_DIR"
echo "  ./scripts/run-demo.sh"
echo ""
echo "🎯 To customize:"
echo "  - Edit config/pipeline.json for game settings"
echo "  - Edit config/adapters.json for AI configuration"
echo "  - Add custom assets to assets/ directory"
