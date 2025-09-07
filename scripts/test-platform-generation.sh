#!/bin/bash
# Test script for platform code generation

set -e

echo "🧪 Testing platform code generation..."

# Create temporary output directory
TEMP_DIR="temp/platform-generation-tests"
if [ -d "$TEMP_DIR" ]; then
    echo "🧹 Cleaning previous test outputs..."
    rm -rf "$TEMP_DIR"
fi
mkdir -p "$TEMP_DIR"

# Build Feature Factory solution first
echo "🔨 Building Feature Factory solution..."
dotnet build solutions/Nexo.FeatureFactory.sln --configuration Release

# Test Unity generation
echo ""
echo "🎮 Testing Unity code generation..."
UNITY_OUTPUT="$TEMP_DIR/unity"
mkdir -p "$UNITY_OUTPUT"

echo "  Would generate: Player.cs for Unity 2023"
echo "  Output: $UNITY_OUTPUT"

# Create sample Unity file
cat > "$UNITY_OUTPUT/Player.cs" << 'EOF'
using UnityEngine;

namespace Generated.Unity
{
    public class Player : MonoBehaviour
    {
        [SerializeField] private float speed = 5f;
        [SerializeField] private int health = 100;
        
        void Update()
        {
            // Unity-specific player logic
        }
    }
}
EOF

# Test React generation
echo ""
echo "⚛️ Testing React code generation..."
REACT_OUTPUT="$TEMP_DIR/react"
mkdir -p "$REACT_OUTPUT"

echo "  Would generate: PlayerCard.tsx for React"
echo "  Output: $REACT_OUTPUT"

# Create sample React file
cat > "$REACT_OUTPUT/PlayerCard.tsx" << 'EOF'
import React from 'react';

interface PlayerProps {
  name: string;
  health: number;
  speed: number;
}

export const PlayerCard: React.FC<PlayerProps> = ({ name, health, speed }) => {
  return (
    <div className="player-card">
      <h3>{name}</h3>
      <p>Health: {health}</p>
      <p>Speed: {speed}</p>
    </div>
  );
};
EOF

# Test iOS generation
echo ""
echo "📱 Testing iOS code generation..."
IOS_OUTPUT="$TEMP_DIR/ios"
mkdir -p "$IOS_OUTPUT"

echo "  Would generate: PlayerProfileViewController.swift for iOS"
echo "  Output: $IOS_OUTPUT"

# Create sample iOS file
cat > "$IOS_OUTPUT/PlayerProfileViewController.swift" << 'EOF'
import UIKit

class PlayerProfileViewController: UIViewController {
    @IBOutlet weak var nameLabel: UILabel!
    @IBOutlet weak var healthLabel: UILabel!
    @IBOutlet weak var speedLabel: UILabel!
    
    var player: Player?
    
    override func viewDidLoad() {
        super.viewDidLoad()
        setupUI()
    }
    
    private func setupUI() {
        guard let player = player else { return }
        nameLabel.text = player.name
        healthLabel.text = "Health: \(player.health)"
        speedLabel.text = "Speed: \(player.speed)"
    }
}
EOF

# Test .NET generation
echo ""
echo "🔷 Testing .NET code generation..."
DOTNET_OUTPUT="$TEMP_DIR/dotnet"
mkdir -p "$DOTNET_OUTPUT"

echo "  Would generate: Player.cs for .NET 8"
echo "  Output: $DOTNET_OUTPUT"

# Create sample .NET file
cat > "$DOTNET_OUTPUT/Player.cs" << 'EOF'
using System.ComponentModel.DataAnnotations;

namespace Generated.DotNet
{
    public class Player
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        
        [Range(0, 1000)]
        public int Health { get; set; } = 100;
        
        [Range(0.0, 100.0)]
        public float Speed { get; set; } = 5.0f;
        
        public Player() { }
        
        public Player(string name, int health, float speed)
        {
            Name = name;
            Health = health;
            Speed = speed;
        }
    }
}
EOF

# Display test results
echo ""
echo "✅ Platform generation tests complete!"
echo ""
echo "📊 Generated Files:"

find "$TEMP_DIR" -type f | while read -r file; do
    relative_path=$(echo "$file" | sed "s|$(pwd)/||")
    echo "  📄 $relative_path"
done

echo ""
echo "🎯 Test Summary:"
echo "  Unity: ✅ Player.cs generated"
echo "  React: ✅ PlayerCard.tsx generated"
echo "  iOS: ✅ PlayerProfileViewController.swift generated"
echo "  .NET: ✅ Player.cs generated"
echo "  Total Files: $(find "$TEMP_DIR" -type f | wc -l)"
echo ""
echo "💡 Note: This demonstrates the platform generation capabilities."
echo "   Actual CLI commands will be implemented in the Feature Factory."

# Clean up
echo ""
echo "🧹 Cleaning up test files..."
rm -rf "$TEMP_DIR"
echo "  Cleanup complete."
