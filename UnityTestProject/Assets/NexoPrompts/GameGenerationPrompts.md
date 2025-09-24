# 🎮 Nexo Agent Game Generation Prompts

## **Primary Generation Task**
```
Generate a complete Doom-style first-person shooter game in Unity with the following requirements:

GAME TYPE: First-Person Shooter
ART STYLE: Dark Sci-Fi Horror
COLOR PALETTE: Red, Orange, Dark Gray
TARGET FPS: 60
PLATFORM: Windows PC

REQUIRED COMPONENTS:
1. FPS Controller with WASD movement and mouse look
2. Weapon System with Shotgun and Plasma Rifle
3. Enemy AI for Imp, Demon, and Cacodemon
4. Health System with damage feedback
5. Audio Manager with 3D spatial audio
6. UI Manager with retro-futuristic HUD
7. Game Manager for level progression

ASSET GENERATION:
- Generate all textures, models, and audio assets
- Create atmospheric lighting and particle effects
- Build complete demo level with rooms and corridors
- Implement enemy spawning and wave management

TECHNICAL REQUIREMENTS:
- Use Unity's built-in systems (Input System, NavMesh, etc.)
- Include proper error handling and logging
- Optimize for 60 FPS performance
- Generate C# scripts under Assets/Scripts/
- Create prefabs under Assets/Prefabs/
- Build scene under Assets/Scenes/

OUTPUT FORMAT:
- Generate all scripts as .cs files
- Create Unity scene file
- Generate asset files (textures, models, audio)
- Include documentation and setup instructions
```

## **Script Generation Prompts**

### **FPS Controller Prompt**
```
Generate a Unity C# script for FPSController with:
- WASD movement with CharacterController
- Mouse look with camera rotation
- Jump mechanics with gravity
- Running with Left Shift
- Ground detection with raycast
- Footstep audio system
- Smooth movement and camera controls
- Include proper error handling and logging
- Use Unity Input System
- Target 60 FPS performance
```

### **Weapon System Prompt**
```
Generate a Unity C# script for WeaponSystem with:
- Multiple weapon support (Shotgun, Plasma Rifle)
- Raycast-based shooting with damage
- Ammo management and reloading
- Weapon switching with number keys
- Muzzle flash and bullet hole effects
- Audio system for firing and reloading
- Recoil system with camera shake
- Laser sight toggle with L key
- Enemy damage detection
- Performance optimized for 60 FPS
```

### **Enemy AI Prompt**
```
Generate a Unity C# script for EnemyAI with:
- NavMesh-based pathfinding
- Player detection and hunting behavior
- Attack patterns for Imp, Demon, Cacodemon
- Health system with damage feedback
- Death animations and effects
- Audio system for movement and attacks
- Spawn system with wave management
- Performance optimized AI
- Include proper error handling
```

## **Asset Generation Prompts**

### **Texture Generation Prompts**
```
Generate the following textures for a dark sci-fi horror game:

1. Wall Texture: "Dark sci-fi wall texture with rust and metal details, high contrast, red and orange colors, 512x512 resolution"

2. Floor Texture: "Industrial floor texture with grime and wear, dark gray with orange highlights, weathered surface, 512x512 resolution"

3. Weapon Icons: "Shotgun weapon icon, retro-futuristic style, red and black colors, 256x256 resolution"

4. Enemy Sprites: "Imp enemy sprite, demonic creature, red skin with glowing eyes, 256x256 resolution"

5. UI Elements: "Health bar UI element, retro-futuristic design, green and red colors, 512x128 resolution"

6. Effects: "Blood splatter decal, realistic blood effect, dark red color, 256x256 resolution"
```

### **3D Model Generation Prompts**
```
Generate 3D models for the following game objects:

1. Shotgun: "Sci-fi shotgun weapon model, detailed geometry, metallic materials, 1 meter length"

2. Plasma Rifle: "Futuristic plasma rifle model, energy effects, blue and white colors, 1.2 meters length"

3. Imp Enemy: "Demonic imp creature model, humanoid shape, red skin, glowing eyes, 1.5 meters height"

4. Demon Enemy: "Large demon model, muscular build, dark red skin, horns, 2 meters height"

5. Cacodemon: "Floating eye monster model, spherical body, tentacles, red color, 1.8 meters diameter"
```

## **Level Generation Prompts**

### **Demo Level Prompt**
```
Generate a Unity scene for a Doom-style demo level with:

STRUCTURE:
- Multiple interconnected rooms
- Strategic chokepoints for combat
- Hidden areas and secrets
- Linear progression path

LIGHTING:
- Atmospheric lighting with shadows
- Dynamic light sources
- Fog effects for atmosphere
- Emergency lighting in some areas

ENEMY PLACEMENT:
- Imp enemies in open areas
- Demon enemies in narrow corridors
- Cacodemon enemies in large rooms
- Progressive difficulty increase

INTERACTIVE ELEMENTS:
- Doors that open with switches
- Health and ammo pickups
- Weapon upgrades
- Checkpoint system

PERFORMANCE:
- Optimized for 60 FPS
- Proper LOD systems
- Efficient lighting setup
- Memory management
```

## **Audio Generation Prompts**

### **Audio Asset Prompts**
```
Generate audio assets for the game:

WEAPON SOUNDS:
- Shotgun firing sound (loud, impactful)
- Plasma rifle firing sound (energy-based)
- Reloading sounds for both weapons
- Weapon switching sounds

ENEMY SOUNDS:
- Imp movement and attack sounds
- Demon growling and charging sounds
- Cacodemon floating and attack sounds
- Death sounds for all enemies

AMBIENT AUDIO:
- Industrial background hum
- Distant machinery sounds
- Atmospheric wind effects
- Emergency alarm sounds

UI SOUNDS:
- Menu interaction sounds
- Health pickup sounds
- Ammo pickup sounds
- Level completion sounds
```

## **Testing and Validation Prompts**

### **Performance Testing Prompt**
```
Test the generated game for:
- 60 FPS performance on mid-range hardware
- Memory usage optimization
- Loading time optimization
- Audio performance
- Rendering performance
- AI performance

Generate performance report with:
- Frame rate analysis
- Memory usage statistics
- Loading time measurements
- Optimization recommendations
```

### **Gameplay Testing Prompt**
```
Test the generated game for:
- Player movement responsiveness
- Weapon system functionality
- Enemy AI behavior
- Audio system quality
- UI system usability
- Level progression flow

Generate gameplay report with:
- Control responsiveness analysis
- Combat system evaluation
- AI behavior assessment
- User experience feedback
- Bug reports and fixes
```

## **Debugging and Monitoring Prompts**

### **Debug System Prompt**
```
Generate a debugging system with:
- Real-time performance monitoring
- Error logging and reporting
- Asset loading status
- AI behavior visualization
- Audio system monitoring
- Memory usage tracking

Include:
- Debug console with commands
- Performance overlay
- Error notification system
- Asset validation tools
- AI state visualization
```

## **Integration Prompts**

### **Nexo Agent Integration Prompt**
```
Integrate the generated game with Nexo Agent system:
- Use existing Nexo Agent for code generation
- Leverage image generation services for assets
- Implement natural language game modification
- Add real-time development assistance
- Include automated testing and validation
- Enable dynamic content generation

Ensure:
- Seamless integration with existing Nexo framework
- Proper error handling and fallbacks
- Performance optimization
- Extensibility for future features
```
