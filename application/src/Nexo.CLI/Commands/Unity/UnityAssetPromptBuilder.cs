namespace Nexo.CLI.Commands.Unity;
/// <summary>Unity asset prompt builder.</summary>
internal static class UnityAssetPromptBuilder
{
    internal static string BuildAssetPrompt(string assetType, string description)
    {
        var schemaHint = assetType switch
        {
            "material" => "MaterialDescriptor JSON with fields: Id, Name, ShaderName, Color (hex), Metallic (0-1), Smoothness (0-1), EmissionColor (hex or null), RenderMode (Opaque|Cutout|Transparent), TextureSlots (dict of slot name to texture path).",
            "prefab" => "PrefabDescriptor JSON with fields: Id, Name, Components (list of {TypeName, Properties}), Children (nested PrefabDescriptors), Position/Rotation/Scale (Vector3 with X,Y,Z).",
            "scene" => "SceneDescriptor JSON with fields: Id, Name, RootObjects (list of PrefabDescriptors), AmbientLightColor (hex), SkyboxMaterial (string or null), NavMeshAreas (list of {Name, Center, Size} with Vector3).",
            "audio" => "AudioDescriptor JSON with fields: Id, Name, Category (sfx|music|ambient|ui|voice|foley), SubCategory, Volume (0-1), Pitch, PitchVariance (random offset ±), Loop, FadeInSeconds, FadeOutSeconds, Priority (0-256), SpatialBlend (0=2D, 1=3D), MinDistance, MaxDistance, RolloffMode (linear|logarithmic|custom), DopplerLevel, Spread (0-360), MixerGroup (Master|SFX|Music|Ambient|UI|Voice), DuckingVolume, BypassEffects, BypassReverb, Variations (list of {VariantId, PitchOffset, VolumeOffset, ClipSuffix}), Tags.",
            "animation" => "AnimationDescriptor JSON with fields: Id, Name, Category (character|weapon|environment|ui|camera), DefaultState, States (list of {Name, ClipName, Speed, Loop, Mirror, SpeedParameterName, Events [{Time (0-1), FunctionName, StringParameter, FloatParameter, IntParameter}], Position {X,Y}}), Transitions (list of {FromState (use \"Any\" for AnyState), ToState, TransitionDuration, ExitTime (0-1), HasExitTime, HasFixedDuration, Conditions [{ParameterName, Mode (equals|not_equal|greater|less|if_true|if_false), Threshold}]}), Parameters (list of {Name, Type (float|int|bool|trigger), DefaultValue}), Layers (list of {Name, Weight, BlendingMode (override|additive), AvatarMask (null for full body, e.g. \"UpperBody\"), IKPass}), Tags. Design the state machine with proper transitions between states — for example a character animator should have Idle, Walk, Run, Jump states with parameter-driven transitions. Use layers to separate upper/lower body animations. Add AnimationEvents for gameplay hooks like footstep sounds or weapon fire frames.",
            "soundbank" => "SoundBankDescriptor JSON with fields: Id, Name, Category (weapon|character|environment|ui|music), Events (list of {EventName (e.g. \"fire\", \"reload\", \"footstep_concrete\"), AudioDescriptorIds (list of IDs for random selection pool), SelectionMode (random|sequential|random_no_repeat), CooldownSeconds, MaxConcurrent, VolumeMultiplier, PitchMultiplier}), Tags. Map game events to audio responses — each event references one or more AudioDescriptor IDs. Use cooldowns to prevent audio spam and MaxConcurrent to limit simultaneous instances. Selection modes control how variants are picked: random for uniform, sequential for round-robin, random_no_repeat to avoid repeating the last played clip.",
            "animationset" => "AnimationSetDescriptor JSON with fields: Id, Name, Category (character|weapon|vehicle), Mappings (list of {GameplayState (idle|walk|run|jump|crouch|slide|ads|fire|reload|die|etc.), AnimationDescriptorId, CrossfadeDuration}), BlendTrees (list of {Name, BlendParameter (e.g. \"Speed\", \"Direction\"), BlendParameterY (for 2D trees, null for 1D), BlendType (1D|2DSimpleDirectional|2DFreeformDirectional|2DFreeformCartesian), Children [{AnimationDescriptorId, Threshold, ThresholdY, TimeScale}]}), Tags. Map gameplay states to animation descriptors with crossfade durations for smooth transitions. Use BlendTrees for locomotion blending — a 1D tree blends idle/walk/run by speed, a 2D tree adds directional strafing. Each BlendTreeChild positions an animation on the blend axis via Threshold.",
            "ui" => "UIDescriptor JSON with fields: Id, Name, CanvasMode (overlay|camera|worldspace), Elements (list of {Type (text|button|image|panel|slider|input), Name, Position (Vector3), Size (Vector2 with X,Y), Properties}).",
            "vfx" => "VfxDescriptor JSON with fields: Id, Name, Category (muzzle_flash|impact|explosion|trail|ambient|shield|pickup|death), Duration, Loop, PlayOnAwake, Modules (list of {ModuleName (emission|shape|velocity|color|size|noise|collision|renderer), Properties (dict of string to value)}), Tags.",
            "physics" => "PhysicsDescriptor JSON with fields: Id, Name, Category (material|collider|rigidbody|projectile), DynamicFriction (0-1), StaticFriction (0-1), Bounciness (0-1), FrictionCombine (average|minimum|maximum|multiply), BounceCombine, Mass, Drag, AngularDrag, UseGravity, IsKinematic, Interpolation (none|interpolate|extrapolate), CollisionDetection (discrete|continuous|continuous_dynamic|continuous_speculative), ColliderType (box|sphere|capsule|mesh|convex_mesh), ColliderCenter (Vector3), ColliderSize (Vector3), ColliderRadius, ColliderHeight, IsTrigger, ProjectileSpeed, GravityMultiplier, MaxLifetime, MaxBounces, BounceEnergyRetention, Tags.",
            "input" => "InputDescriptor JSON with fields: Id, Name, ActionMaps (list of {Name, Actions (list of {Name, Type (button|value|passthrough), ControlType (Button|Vector2|Axis), Bindings (list of {Path, Composite (2DVector|1DAxis|null), CompositePart (up|down|left|right|null), Interactions (list), Processors (list)})})}), Tags.",
            "network" => "NetworkDescriptor JSON with fields: Id, Name, Category (player|projectile|pickup|game_state|environment), IsPlayerObject, DontDestroyWithOwner, SyncedVariables (list of {Name, Type (float|int|bool|Vector3|Quaternion|string|custom), Permission (server|owner|everyone), DeliveryMode (reliable|unreliable), SendRate}), Rpcs (list of {Name, Direction (server|client), DeliveryMode, Parameters (list of {Name, Type})}), Spawning ({AutoSpawn, SpawnAuthority (server|owner), DespawnDelay, PoolId}), Tags.",
            "ainavigation" => "AiNavigationDescriptor JSON with fields: Id, Name, PatrolRoutes (list of {Name, Waypoints (list of Vector3), Loop, WaitTimePerPoint, MoveSpeed}), CoverPoints (list of {Position (Vector3), CoverDirection (Vector3), CoverType (half|full|lean_left|lean_right), Rating (0-1)}), TacticalPositions (list of {Name, Position (Vector3), Role (sniper_nest|choke_point|flank_route|power_position|objective_watch|generic), ControlRadius, SightlineTargets (list of Vector3)}), Tags.",
            "build" => "BuildDescriptor JSON with fields: Id, Name, TargetPlatform (StandaloneWindows64|StandaloneOSX|StandaloneLinux64|WebGL|Android|iOS), OutputPath, DevelopmentBuild, AllowDebugging, Scenes (list of scene paths), CompanyName, ProductName, BundleVersion, Quality ({VSyncCount, TargetFrameRate, ShadowQuality (disable|hard_only|all), ShadowResolution, ShadowDistance, AntiAliasing (disabled|2x|4x|8x), TextureQuality (full|half|quarter|eighth), MaxLod}), ScriptingDefines (list), Tags.",
            _ => ""
        };

        return $@"{UnityDevCommand.UnitySystemPrompt}

    You are generating a Unity asset descriptor and its companion C# loader script.

    Asset type: {assetType}
    Description: {description}

    Schema: {schemaHint}

    Generate TWO files:
    1. A JSON descriptor file at: Assets/NexoAssets/{assetType}/{{name}}.json
       - The JSON must conform exactly to the schema above.
    2. A C# loader script at: Assets/NexoAssets/{assetType}/{{name}}Loader.cs
       - The loader should read the JSON at runtime using JsonUtility or System.Text.Json and create the corresponding Unity objects.
       - Use a MonoBehaviour that loads on Awake or a static utility method.

    Each file must start with a // FILE: marker with the relative path from the project root.";
    }

}
