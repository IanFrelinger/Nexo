using System.Text.Json;
using FluentAssertions;
using Nexo.Commercial.GameDomain.Assets;

namespace Nexo.Commercial.Tests.GameDomain;
/// <summary>Tests for vfx physics input.</summary>
public class VfxPhysicsInputTests
{
    // ── VfxDescriptor ──────────────────────────────────────────────────

    [Fact]
    public void VfxDescriptor_DefaultValues()
    {
        var vfx = new VfxDescriptor();

        vfx.Id.Should().BeEmpty();
        vfx.Name.Should().BeEmpty();
        vfx.Category.Should().Be("impact");
        vfx.Duration.Should().Be(1.0);
        vfx.Loop.Should().BeFalse();
        vfx.PlayOnAwake.Should().BeTrue();
        vfx.Modules.Should().BeEmpty();
        vfx.Tags.Should().BeEmpty();
    }

    [Fact]
    public void VfxDescriptor_WithModules()
    {
        var vfx = new VfxDescriptor
        {
            Id = "vfx-muzzle",
            Name = "Muzzle Flash",
            Category = "muzzle_flash",
            Duration = 0.15,
            Loop = false,
            PlayOnAwake = true,
            Modules =
            [
                new ParticleModule
                {
                    ModuleName = "emission",
                    Properties = new Dictionary<string, object> { ["rateOverTime"] = 50 }
                },
                new ParticleModule
                {
                    ModuleName = "shape",
                    Properties = new Dictionary<string, object> { ["shapeType"] = "cone", ["angle"] = 15 }
                }
            ],
            Tags = ["weapon", "fire"]
        };

        vfx.Modules.Should().HaveCount(2);
        vfx.Modules[0].ModuleName.Should().Be("emission");
        vfx.Modules[1].Properties.Should().ContainKey("shapeType");
        vfx.Tags.Should().Contain("weapon");
    }

    [Fact]
    public void VfxDescriptor_JsonRoundTrip()
    {
        var original = new VfxDescriptor
        {
            Id = "vfx-explosion",
            Name = "Grenade Explosion",
            Category = "explosion",
            Duration = 2.5,
            Loop = false,
            Modules =
            [
                new ParticleModule { ModuleName = "emission", Properties = new Dictionary<string, object> { ["burst"] = 100 } }
            ],
            Tags = ["explosive"]
        };

        var opts = new JsonSerializerOptions(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };
        var json = JsonSerializer.Serialize(original, opts);
        var restored = JsonSerializer.Deserialize<VfxDescriptor>(json, opts);

        restored.Should().NotBeNull();
        restored!.Id.Should().Be("vfx-explosion");
        restored.Category.Should().Be("explosion");
        restored.Duration.Should().Be(2.5);
        restored.Modules.Should().HaveCount(1);
        restored.Tags.Should().Contain("explosive");
    }

    // ── PhysicsDescriptor ──────────────────────────────────────────────

    [Fact]
    public void PhysicsDescriptor_DefaultValues()
    {
        var phys = new PhysicsDescriptor();

        phys.Id.Should().BeEmpty();
        phys.Name.Should().BeEmpty();
        phys.Category.Should().Be("material");
        phys.DynamicFriction.Should().Be(0.6);
        phys.StaticFriction.Should().Be(0.6);
        phys.Bounciness.Should().Be(0.0);
        phys.FrictionCombine.Should().Be("average");
        phys.BounceCombine.Should().Be("average");
        phys.Mass.Should().Be(1.0);
        phys.Drag.Should().Be(0.0);
        phys.AngularDrag.Should().Be(0.05);
        phys.UseGravity.Should().BeTrue();
        phys.IsKinematic.Should().BeFalse();
        phys.Interpolation.Should().Be("none");
        phys.CollisionDetection.Should().Be("discrete");
        phys.ColliderType.Should().Be("box");
        phys.ColliderCenter.Should().Be(new Vector3Descriptor());
        phys.ColliderSize.Should().Be(new Vector3Descriptor(1, 1, 1));
        phys.ColliderRadius.Should().Be(0.5);
        phys.ColliderHeight.Should().Be(2.0);
        phys.IsTrigger.Should().BeFalse();
        phys.ProjectileSpeed.Should().Be(50.0);
        phys.GravityMultiplier.Should().Be(1.0);
        phys.MaxLifetime.Should().Be(5.0);
        phys.MaxBounces.Should().Be(0);
        phys.BounceEnergyRetention.Should().Be(0.6);
        phys.Tags.Should().BeEmpty();
    }

    [Fact]
    public void PhysicsDescriptor_ProjectileProfile()
    {
        var proj = new PhysicsDescriptor
        {
            Id = "phys-rocket",
            Name = "Rocket Projectile",
            Category = "projectile",
            Mass = 0.5,
            UseGravity = false,
            CollisionDetection = "continuous_dynamic",
            ColliderType = "capsule",
            ColliderRadius = 0.1,
            ColliderHeight = 0.5,
            ProjectileSpeed = 80.0,
            GravityMultiplier = 0.0,
            MaxLifetime = 8.0,
            MaxBounces = 0,
            Tags = ["weapon", "explosive"]
        };

        proj.Category.Should().Be("projectile");
        proj.Mass.Should().Be(0.5);
        proj.UseGravity.Should().BeFalse();
        proj.CollisionDetection.Should().Be("continuous_dynamic");
        proj.ProjectileSpeed.Should().Be(80.0);
        proj.GravityMultiplier.Should().Be(0.0);
        proj.Tags.Should().HaveCount(2);
    }

    [Fact]
    public void PhysicsDescriptor_JsonRoundTrip()
    {
        var original = new PhysicsDescriptor
        {
            Id = "phys-bouncy",
            Name = "Bouncy Ball",
            Category = "material",
            Bounciness = 0.95,
            DynamicFriction = 0.2,
            Mass = 0.1,
            Tags = ["toy"]
        };

        var opts = new JsonSerializerOptions(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };
        var json = JsonSerializer.Serialize(original, opts);
        var restored = JsonSerializer.Deserialize<PhysicsDescriptor>(json, opts);

        restored.Should().NotBeNull();
        restored!.Bounciness.Should().Be(0.95);
        restored.DynamicFriction.Should().Be(0.2);
        restored.Mass.Should().Be(0.1);
        restored.Tags.Should().Contain("toy");
    }

    // ── InputDescriptor ────────────────────────────────────────────────

    [Fact]
    public void InputDescriptor_DefaultValues()
    {
        var input = new InputDescriptor();

        input.Id.Should().BeEmpty();
        input.Name.Should().BeEmpty();
        input.ActionMaps.Should().BeEmpty();
        input.Tags.Should().BeEmpty();
    }

    [Fact]
    public void InputDescriptor_WithActionMapAndCompositeBindings()
    {
        var input = new InputDescriptor
        {
            Id = "input-fps",
            Name = "FPS Controls",
            ActionMaps =
            [
                new InputActionMap
                {
                    Name = "Gameplay",
                    Actions =
                    [
                        new InputAction
                        {
                            Name = "Move",
                            Type = "value",
                            ControlType = "Vector2",
                            Bindings =
                            [
                                new InputBinding { Path = "<Keyboard>/w", Composite = "2DVector", CompositePart = "up" },
                                new InputBinding { Path = "<Keyboard>/s", Composite = "2DVector", CompositePart = "down" },
                                new InputBinding { Path = "<Keyboard>/a", Composite = "2DVector", CompositePart = "left" },
                                new InputBinding { Path = "<Keyboard>/d", Composite = "2DVector", CompositePart = "right" },
                                new InputBinding { Path = "<Gamepad>/leftStick" }
                            ]
                        },
                        new InputAction
                        {
                            Name = "Fire",
                            Type = "button",
                            ControlType = "Button",
                            Bindings =
                            [
                                new InputBinding { Path = "<Mouse>/leftButton" },
                                new InputBinding { Path = "<Gamepad>/rightTrigger", Interactions = ["hold"] }
                            ]
                        }
                    ]
                }
            ],
            Tags = ["fps", "keyboard", "gamepad"]
        };

        input.ActionMaps.Should().HaveCount(1);
        input.ActionMaps[0].Name.Should().Be("Gameplay");
        input.ActionMaps[0].Actions.Should().HaveCount(2);

        var move = input.ActionMaps[0].Actions[0];
        move.Name.Should().Be("Move");
        move.Bindings.Should().HaveCount(5);
        move.Bindings[0].CompositePart.Should().Be("up");
        move.Bindings[4].Composite.Should().BeNull();

        var fire = input.ActionMaps[0].Actions[1];
        fire.Bindings[1].Interactions.Should().Contain("hold");
    }

    [Fact]
    public void InputDescriptor_JsonRoundTrip()
    {
        var original = new InputDescriptor
        {
            Id = "input-ui",
            Name = "UI Controls",
            ActionMaps =
            [
                new InputActionMap
                {
                    Name = "UI",
                    Actions =
                    [
                        new InputAction
                        {
                            Name = "Navigate",
                            Type = "passthrough",
                            ControlType = "Vector2",
                            Bindings = [new InputBinding { Path = "<Gamepad>/dpad", Processors = ["normalize"] }]
                        }
                    ]
                }
            ],
            Tags = ["ui"]
        };

        var opts = new JsonSerializerOptions(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };
        var json = JsonSerializer.Serialize(original, opts);
        var restored = JsonSerializer.Deserialize<InputDescriptor>(json, opts);

        restored.Should().NotBeNull();
        restored!.Id.Should().Be("input-ui");
        restored.ActionMaps.Should().HaveCount(1);
        restored.ActionMaps[0].Actions[0].Bindings[0].Processors.Should().Contain("normalize");
    }

    // ── AiNavigationDescriptor ─────────────────────────────────────────

    [Fact]
    public void AiNavigationDescriptor_DefaultValues()
    {
        var nav = new AiNavigationDescriptor();

        nav.Id.Should().BeEmpty();
        nav.Name.Should().BeEmpty();
        nav.PatrolRoutes.Should().BeEmpty();
        nav.CoverPoints.Should().BeEmpty();
        nav.TacticalPositions.Should().BeEmpty();
        nav.Tags.Should().BeEmpty();
    }

    [Fact]
    public void AiNavigationDescriptor_WithPatrolRouteAndCoverPoints()
    {
        var nav = new AiNavigationDescriptor
        {
            Id = "ai-nav-warehouse",
            Name = "Warehouse AI Nav",
            PatrolRoutes =
            [
                new PatrolRoute
                {
                    Name = "Perimeter",
                    Waypoints =
                    [
                        new Vector3Descriptor(0, 0, 0),
                        new Vector3Descriptor(10, 0, 0),
                        new Vector3Descriptor(10, 0, 10),
                        new Vector3Descriptor(0, 0, 10)
                    ],
                    Loop = true,
                    WaitTimePerPoint = 3.0,
                    MoveSpeed = 2.5
                }
            ],
            CoverPoints =
            [
                new CoverPoint
                {
                    Position = new Vector3Descriptor(5, 0, 2),
                    CoverDirection = new Vector3Descriptor(0, 0, 1),
                    CoverType = "half",
                    Rating = 0.8
                },
                new CoverPoint
                {
                    Position = new Vector3Descriptor(8, 0, 7),
                    CoverDirection = new Vector3Descriptor(-1, 0, 0),
                    CoverType = "full",
                    Rating = 0.95
                }
            ],
            TacticalPositions =
            [
                new TacticalPosition
                {
                    Name = "Sniper Balcony",
                    Position = new Vector3Descriptor(3, 5, 8),
                    Role = "sniper_nest",
                    ControlRadius = 25.0,
                    SightlineTargets =
                    [
                        new Vector3Descriptor(10, 0, 0),
                        new Vector3Descriptor(0, 0, 10)
                    ]
                }
            ],
            Tags = ["warehouse", "indoor"]
        };

        nav.PatrolRoutes.Should().HaveCount(1);
        nav.PatrolRoutes[0].Waypoints.Should().HaveCount(4);
        nav.PatrolRoutes[0].Loop.Should().BeTrue();
        nav.PatrolRoutes[0].MoveSpeed.Should().Be(2.5);

        nav.CoverPoints.Should().HaveCount(2);
        nav.CoverPoints[0].Rating.Should().Be(0.8);
        nav.CoverPoints[1].CoverType.Should().Be("full");

        nav.TacticalPositions.Should().HaveCount(1);
        nav.TacticalPositions[0].Role.Should().Be("sniper_nest");
        nav.TacticalPositions[0].SightlineTargets.Should().HaveCount(2);
    }

    [Fact]
    public void AiNavigationDescriptor_JsonRoundTrip()
    {
        var original = new AiNavigationDescriptor
        {
            Id = "ai-nav-01",
            Name = "Test Nav",
            PatrolRoutes = [new PatrolRoute { Name = "Route A", Waypoints = [new Vector3Descriptor(1, 2, 3)] }],
            CoverPoints = [new CoverPoint { Position = new Vector3Descriptor(4, 5, 6), CoverType = "lean_right", Rating = 0.7 }],
            TacticalPositions = [new TacticalPosition { Name = "Choke", Role = "choke_point", ControlRadius = 5 }],
            Tags = ["test"]
        };

        var opts = new JsonSerializerOptions(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };
        var json = JsonSerializer.Serialize(original, opts);
        var restored = JsonSerializer.Deserialize<AiNavigationDescriptor>(json, opts);

        restored.Should().NotBeNull();
        restored!.PatrolRoutes.Should().HaveCount(1);
        restored.CoverPoints[0].CoverType.Should().Be("lean_right");
        restored.TacticalPositions[0].Role.Should().Be("choke_point");
        restored.Tags.Should().Contain("test");
    }

    // ── BuildDescriptor ────────────────────────────────────────────────

    [Fact]
    public void BuildDescriptor_DefaultValues()
    {
        var build = new BuildDescriptor();

        build.Id.Should().BeEmpty();
        build.Name.Should().BeEmpty();
        build.TargetPlatform.Should().Be("StandaloneWindows64");
        build.OutputPath.Should().Be("Builds/");
        build.DevelopmentBuild.Should().BeFalse();
        build.AllowDebugging.Should().BeFalse();
        build.Scenes.Should().BeEmpty();
        build.CompanyName.Should().BeEmpty();
        build.ProductName.Should().BeEmpty();
        build.BundleVersion.Should().Be("0.1.0");
        build.Quality.Should().NotBeNull();
        build.ScriptingDefines.Should().BeEmpty();
        build.Tags.Should().BeEmpty();
    }

    [Fact]
    public void BuildDescriptor_WithQualityConfig()
    {
        var build = new BuildDescriptor
        {
            Id = "build-release",
            Name = "Release Build",
            TargetPlatform = "StandaloneWindows64",
            OutputPath = "Builds/Win64/",
            CompanyName = "NexoGames",
            ProductName = "FPS Arena",
            BundleVersion = "1.0.0",
            Scenes = ["Assets/Scenes/MainMenu.unity", "Assets/Scenes/Arena.unity"],
            Quality = new QualityConfig
            {
                VSyncCount = 1,
                TargetFrameRate = 120,
                ShadowQuality = "all",
                ShadowResolution = 4096,
                ShadowDistance = 200.0,
                AntiAliasing = "8x",
                TextureQuality = "full",
                MaxLod = 0
            },
            ScriptingDefines = ["RELEASE", "ENABLE_ANALYTICS"],
            Tags = ["release", "windows"]
        };

        build.Quality.TargetFrameRate.Should().Be(120);
        build.Quality.ShadowResolution.Should().Be(4096);
        build.Quality.AntiAliasing.Should().Be("8x");
        build.Scenes.Should().HaveCount(2);
        build.ScriptingDefines.Should().Contain("RELEASE");
    }

    [Fact]
    public void QualityConfig_DefaultValues()
    {
        var q = new QualityConfig();

        q.VSyncCount.Should().Be(1);
        q.TargetFrameRate.Should().Be(-1);
        q.ShadowQuality.Should().Be("all");
        q.ShadowResolution.Should().Be(2048);
        q.ShadowDistance.Should().Be(150.0);
        q.AntiAliasing.Should().Be("4x");
        q.TextureQuality.Should().Be("full");
        q.MaxLod.Should().Be(0);
    }

    [Fact]
    public void BuildDescriptor_JsonRoundTrip()
    {
        var original = new BuildDescriptor
        {
            Id = "build-dev",
            Name = "Dev Build",
            TargetPlatform = "StandaloneLinux64",
            DevelopmentBuild = true,
            AllowDebugging = true,
            Scenes = ["Assets/Scenes/Test.unity"],
            Quality = new QualityConfig { AntiAliasing = "2x", ShadowDistance = 50.0 },
            ScriptingDefines = ["DEBUG"],
            Tags = ["dev"]
        };

        var opts = new JsonSerializerOptions(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };
        var json = JsonSerializer.Serialize(original, opts);
        var restored = JsonSerializer.Deserialize<BuildDescriptor>(json, opts);

        restored.Should().NotBeNull();
        restored!.TargetPlatform.Should().Be("StandaloneLinux64");
        restored.DevelopmentBuild.Should().BeTrue();
        restored.Quality.AntiAliasing.Should().Be("2x");
        restored.Quality.ShadowDistance.Should().Be(50.0);
        restored.ScriptingDefines.Should().Contain("DEBUG");
    }
}
