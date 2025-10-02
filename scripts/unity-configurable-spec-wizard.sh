#!/usr/bin/env bash
set -euo pipefail

UNITY="/Applications/Unity/Hub/Editor/6000.2.6f1/Unity.app/Contents/MacOS/Unity"
PROJ="/Users/ianfrelinger/CursorProjects/Nexo/DirectorStudioUnity"
ART="/Users/ianfrelinger/CursorProjects/Nexo"
mkdir -p "$ART"

# Default values - you can override these by passing arguments
INSPIRATION="${1:-"A story or narrative idea"}"
EMOTIONAL_GOAL="${2:-"Wonder and discovery"}"
CORE_FANTASY="${3:-"Exploration freedom"}"
PRIMARY_GENRE="${4:-"RPG"}"
VISUAL_STYLE="${5:-"Pixel Art/Retro"}"
PERSPECTIVE="${6:-"Top-down (strategic)"}"
CORE_INTERACTION="${7:-"Exploration/Movement"}"
PROGRESSION_SYSTEM="${8:-"Story progression"}"
CHALLENGE_TYPE="${9:-"Pattern recognition"}"
MULTIPLAYER_TYPE="${10:-"Single-player only"}"
SETTING="${11:-"Fantasy/Medieval"}"
NARRATIVE_APPROACH="${12:-"Story-driven (narrative is primary)"}"
PLAYER_CHARACTER="${13:-"Custom character (player creates)"}"
DEVELOPMENT_SCOPE="${14:-"Medium game (10-20 hours)"}"
TARGET_PLATFORM="${15:-"PC (Windows/Mac/Linux)"}"
UNIQUE_SELLING_POINT="${16:-"Compelling story"}"
FINAL_CHOICE="${17:-"This looks perfect!"}"

echo "🎮 Starting Configurable Game Specification Wizard"
echo "================================================="
echo ""
echo "This wizard will guide you through creating a detailed game specification"
echo "using your provided answers for validation and testing."
echo ""
echo "Your answers:"
echo "• Inspiration: $INSPIRATION"
echo "• Emotional Goal: $EMOTIONAL_GOAL"
echo "• Core Fantasy: $CORE_FANTASY"
echo "• Primary Genre: $PRIMARY_GENRE"
echo "• Visual Style: $VISUAL_STYLE"
echo "• Perspective: $PERSPECTIVE"
echo "• Core Interaction: $CORE_INTERACTION"
echo "• Progression System: $PROGRESSION_SYSTEM"
echo "• Challenge Type: $CHALLENGE_TYPE"
echo "• Multiplayer: $MULTIPLAYER_TYPE"
echo "• Setting: $SETTING"
echo "• Narrative Approach: $NARRATIVE_APPROACH"
echo "• Player Character: $PLAYER_CHARACTER"
echo "• Development Scope: $DEVELOPMENT_SCOPE"
echo "• Target Platform: $TARGET_PLATFORM"
echo "• Unique Selling Point: $UNIQUE_SELLING_POINT"
echo "• Final Choice: $FINAL_CHOICE"
echo ""

"$UNITY" \
  -batchmode -nographics \
  -projectPath "$PROJ" \
  -executeMethod NexoDirectorStudio.Editor.ConfigurableGameSpecWizard.RunWizard \
  -inspiration "$INSPIRATION" \
  -emotionalGoal "$EMOTIONAL_GOAL" \
  -coreFantasy "$CORE_FANTASY" \
  -primaryGenre "$PRIMARY_GENRE" \
  -visualStyle "$VISUAL_STYLE" \
  -perspective "$PERSPECTIVE" \
  -coreInteraction "$CORE_INTERACTION" \
  -progressionSystem "$PROGRESSION_SYSTEM" \
  -challengeType "$CHALLENGE_TYPE" \
  -multiplayerType "$MULTIPLAYER_TYPE" \
  -setting "$SETTING" \
  -narrativeApproach "$NARRATIVE_APPROACH" \
  -playerCharacter "$PLAYER_CHARACTER" \
  -developmentScope "$DEVELOPMENT_SCOPE" \
  -targetPlatform "$TARGET_PLATFORM" \
  -uniqueSellingPoint "$UNIQUE_SELLING_POINT" \
  -finalChoice "$FINAL_CHOICE" \
  -logFile "$ART/unity-configurable-spec-wizard.log" \
  -quit

echo ""
echo "📊 Configurable Game Specification Wizard Results:"
if [[ -f "$ART/game-specification.json" ]]; then
  echo "✅ Specification saved to: $ART/game-specification.json"
  echo "📄 Content preview:"
  head -30 "$ART/game-specification.json"
else
  echo "❌ No specification file generated"
fi

echo ""
echo "📋 Log file: $ART/unity-configurable-spec-wizard.log"
