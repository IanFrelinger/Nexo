using System.Text.Json;
using FluentAssertions;
using Ashlar.Commercial.GameDomain.Assets;

namespace Ashlar.Commercial.Tests.GameDomain;
/// <summary>Tests for asset descriptor.</summary>
public class AssetDescriptorTests
{
    [Fact]
    public void MaterialDescriptor_DefaultValues()
    {
        var mat = new MaterialDescriptor();

        mat.Id.Should().BeEmpty();
        mat.Name.Should().BeEmpty();
        mat.ShaderName.Should().Be("Standard");
        mat.Color.Should().Be("#FFFFFF");
        mat.Metallic.Should().Be(0);
        mat.Smoothness.Should().Be(0);
        mat.EmissionColor.Should().BeNull();
        mat.RenderMode.Should().Be("Opaque");
        mat.TextureSlots.Should().BeEmpty();
    }

    [Fact]
    public void MaterialDescriptor_WithCustomProperties()
    {
        var mat = new MaterialDescriptor
        {
            Id = "gold",
            Name = "Polished Gold",
            ShaderName = "Universal Render Pipeline/Lit",
            Color = "#FFD700",
            Metallic = 1.0,
            Smoothness = 0.9,
            EmissionColor = "#332200",
            RenderMode = "Opaque",
            TextureSlots = new Dictionary<string, string>
            {
                ["_MainTex"] = "Textures/gold_albedo.png",
                ["_BumpMap"] = "Textures/gold_normal.png"
            }
        };

        mat.Id.Should().Be("gold");
        mat.Name.Should().Be("Polished Gold");
        mat.ShaderName.Should().Be("Universal Render Pipeline/Lit");
        mat.Metallic.Should().Be(1.0);
        mat.Smoothness.Should().Be(0.9);
        mat.EmissionColor.Should().Be("#332200");
        mat.TextureSlots.Should().HaveCount(2);
        mat.TextureSlots["_MainTex"].Should().Be("Textures/gold_albedo.png");
    }

    [Fact]
    public void MaterialDescriptor_JsonRoundTrip()
    {
        var original = new MaterialDescriptor
        {
            Id = "steel",
            Name = "Brushed Steel",
            Metallic = 0.8,
            Smoothness = 0.6,
            RenderMode = "Opaque",
            TextureSlots = new Dictionary<string, string> { ["_MainTex"] = "tex/steel.png" }
        };

        var opts = new JsonSerializerOptions(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };
        var json = JsonSerializer.Serialize(original, opts);
        var restored = JsonSerializer.Deserialize<MaterialDescriptor>(json, opts);

        restored.Should().NotBeNull();
        restored!.Id.Should().Be(original.Id);
        restored.Metallic.Should().Be(original.Metallic);
        restored.TextureSlots.Should().ContainKey("_MainTex");
    }

    [Fact]
    public void MaterialDescriptor_JsonRoundTrip_PreservesRenderMode()
    {
        var original = new MaterialDescriptor
        {
            Id = "mat-1",
            Name = "Test Mat",
            ShaderName = "Standard",
            Color = "#FF0000",
            Metallic = 0.5,
            Smoothness = 0.7,
            RenderMode = "Transparent"
        };

        var opts = new JsonSerializerOptions(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };
        var json = JsonSerializer.Serialize(original, opts);
        var restored = JsonSerializer.Deserialize<MaterialDescriptor>(json, opts);

        restored.Should().NotBeNull();
        restored!.Id.Should().Be("mat-1");
        restored.RenderMode.Should().Be("Transparent");
    }

    [Fact]
    public void PrefabDescriptor_DefaultValues()
    {
        var prefab = new PrefabDescriptor();

        prefab.Id.Should().BeEmpty();
        prefab.Name.Should().BeEmpty();
        prefab.Components.Should().BeEmpty();
        prefab.Children.Should().BeEmpty();
        prefab.Position.Should().Be(new Vector3Descriptor());
        prefab.Rotation.Should().Be(new Vector3Descriptor());
        prefab.Scale.Should().Be(new Vector3Descriptor(1, 1, 1));
    }

    [Fact]
    public void PrefabDescriptor_WithChildren()
    {
        var child = new PrefabDescriptor
        {
            Id = "turret-barrel",
            Name = "Barrel",
            Position = new Vector3Descriptor(0, 1, 0)
        };

        var parent = new PrefabDescriptor
        {
            Id = "turret",
            Name = "Turret",
            Components = [new ComponentEntry { TypeName = "MeshRenderer" }],
            Children = [child],
            Scale = new Vector3Descriptor(2, 2, 2)
        };

        parent.Children.Should().HaveCount(1);
        parent.Children[0].Id.Should().Be("turret-barrel");
        parent.Children[0].Position.Y.Should().Be(1);
        parent.Components.Should().HaveCount(1);
        parent.Components[0].TypeName.Should().Be("MeshRenderer");
    }

    [Fact]
    public void SceneDescriptor_DefaultValues()
    {
        var scene = new SceneDescriptor();

        scene.Id.Should().BeEmpty();
        scene.Name.Should().BeEmpty();
        scene.RootObjects.Should().BeEmpty();
        scene.AmbientLightColor.Should().Be("#404040");
        scene.SkyboxMaterial.Should().BeNull();
        scene.NavMeshAreas.Should().BeEmpty();
    }

    [Fact]
    public void SceneDescriptor_WithNavMeshAreas()
    {
        var scene = new SceneDescriptor
        {
            Id = "arena",
            Name = "Combat Arena",
            RootObjects = [new PrefabDescriptor { Id = "floor", Name = "Floor" }],
            AmbientLightColor = "#808080",
            SkyboxMaterial = "Skyboxes/DaySky",
            NavMeshAreas =
            [
                new NavMeshArea
                {
                    Name = "Walkable",
                    Center = new Vector3Descriptor(0, 0, 0),
                    Size = new Vector3Descriptor(50, 1, 50)
                },
                new NavMeshArea
                {
                    Name = "NotWalkable",
                    Center = new Vector3Descriptor(10, 0, 10),
                    Size = new Vector3Descriptor(5, 1, 5)
                }
            ]
        };

        scene.RootObjects.Should().HaveCount(1);
        scene.NavMeshAreas.Should().HaveCount(2);
        scene.NavMeshAreas[0].Name.Should().Be("Walkable");
        scene.NavMeshAreas[0].Size.X.Should().Be(50);
        scene.NavMeshAreas[1].Center.X.Should().Be(10);
    }

    [Fact]
    public void AudioDescriptor_DefaultValues()
    {
        var audio = new AudioDescriptor();

        audio.Id.Should().BeEmpty();
        audio.Name.Should().BeEmpty();
        audio.Category.Should().Be("sfx");
        audio.SubCategory.Should().BeEmpty();
        audio.Volume.Should().Be(1.0);
        audio.Pitch.Should().Be(1.0);
        audio.PitchVariance.Should().Be(0.0);
        audio.Loop.Should().BeFalse();
        audio.FadeInSeconds.Should().Be(0);
        audio.FadeOutSeconds.Should().Be(0);
        audio.Priority.Should().Be(128);
        audio.SpatialBlend.Should().Be(1.0);
        audio.MinDistance.Should().Be(1.0);
        audio.MaxDistance.Should().Be(50.0);
        audio.RolloffMode.Should().Be("logarithmic");
        audio.DopplerLevel.Should().Be(1.0);
        audio.Spread.Should().Be(0.0);
        audio.MixerGroup.Should().Be("Master");
        audio.DuckingVolume.Should().Be(1.0);
        audio.BypassEffects.Should().BeFalse();
        audio.BypassReverb.Should().BeFalse();
        audio.Variations.Should().BeEmpty();
        audio.Tags.Should().BeEmpty();
    }

    [Fact]
    public void AudioDescriptor_JsonRoundTrip_PreservesSpatialAndVariationMetadata()
    {
        var original = new AudioDescriptor
        {
            Id = "explosion",
            Name = "Explosion SFX",
            Category = "sfx",
            SubCategory = "explosion",
            Volume = 0.8,
            Pitch = 0.9,
            PitchVariance = 0.1,
            SpatialBlend = 1.0,
            Loop = false,
            MixerGroup = "SFX",
            Variations =
            [
                new AudioVariation { VariantId = "v1", PitchOffset = 0.05, ClipSuffix = "_01" }
            ],
            Tags = ["combat", "explosion"]
        };

        var opts = new JsonSerializerOptions(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };
        var json = JsonSerializer.Serialize(original, opts);
        var restored = JsonSerializer.Deserialize<AudioDescriptor>(json, opts);

        restored.Should().NotBeNull();
        restored!.Id.Should().Be("explosion");
        restored.SubCategory.Should().Be("explosion");
        restored.SpatialBlend.Should().Be(1.0);
        restored.MixerGroup.Should().Be("SFX");
        restored.Variations.Should().HaveCount(1);
        restored.Variations[0].VariantId.Should().Be("v1");
        restored.Tags.Should().HaveCount(2);
    }

    [Fact]
    public void UIDescriptor_DefaultValues()
    {
        var ui = new UIDescriptor();

        ui.Id.Should().BeEmpty();
        ui.Name.Should().BeEmpty();
        ui.CanvasMode.Should().Be("overlay");
        ui.Elements.Should().BeEmpty();
    }

    [Fact]
    public void Vector3Descriptor_DefaultValues()
    {
        var v = new Vector3Descriptor();

        v.X.Should().Be(0);
        v.Y.Should().Be(0);
        v.Z.Should().Be(0);
    }

    [Fact]
    public void Vector3Descriptor_Equality()
    {
        var a = new Vector3Descriptor(1.0, 2.0, 3.0);
        var b = new Vector3Descriptor(1.0, 2.0, 3.0);
        var c = new Vector3Descriptor(1.0, 2.0, 4.0);

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
        a.Should().NotBe(c);
    }

    [Fact]
    public void Vector2Descriptor_DefaultValues()
    {
        var v = new Vector2Descriptor();

        v.X.Should().Be(0);
        v.Y.Should().Be(0);
    }

    [Fact]
    public void Vector2Descriptor_Equality()
    {
        var a = new Vector2Descriptor(1.0, 2.0);
        var b = new Vector2Descriptor(1.0, 2.0);
        var c = new Vector2Descriptor(1.0, 3.0);

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
        a.Should().NotBe(c);
    }

    [Fact]
    public void ComponentEntry_DefaultValues()
    {
        var entry = new ComponentEntry();

        entry.TypeName.Should().BeEmpty();
        entry.Properties.Should().BeEmpty();
    }

    [Fact]
    public void NavMeshArea_DefaultValues()
    {
        var area = new NavMeshArea();

        area.Name.Should().BeEmpty();
        area.Center.Should().Be(new Vector3Descriptor());
        area.Size.Should().Be(new Vector3Descriptor());
    }

    [Fact]
    public void UIElement_DefaultValues()
    {
        var elem = new UIElement();

        elem.Type.Should().Be("panel");
        elem.Name.Should().BeEmpty();
        elem.Position.Should().Be(new Vector3Descriptor());
        elem.Size.Should().Be(new Vector2Descriptor());
        elem.Properties.Should().BeEmpty();
    }
}
