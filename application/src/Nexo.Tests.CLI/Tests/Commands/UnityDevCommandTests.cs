using System.Text.Json;
using FluentAssertions;
using Nexo.CLI.Commands;
using Nexo.GameDomain.Assets;
using Xunit;

namespace Nexo.Tests.CLI.Tests.Commands;

[Trait("Category", "CLI")]
public sealed class UnityDevCommandTests
{
    [Fact(Timeout = 15000)]
    public void ParseFiles_EmptyInput_ReturnsEmpty()
    {
        UnityDevCommand.ParseFiles("").Should().BeEmpty();
        UnityDevCommand.ParseFiles("   ").Should().BeEmpty();
    }

    [Fact(Timeout = 15000)]
    public void ParseFiles_SingleFile_ParsesCorrectly()
    {
        var input = @"// FILE: Assets/Scripts/Player.cs
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private float _speed = 5f;
}";
        var result = UnityDevCommand.ParseFiles(input);

        result.Should().HaveCount(1);
        result[0].RelativePath.Should().Be("Assets/Scripts/Player.cs");
        result[0].Content.Should().Contain("public class Player");
        result[0].Content.Should().Contain("[SerializeField]");
    }

    [Fact(Timeout = 15000)]
    public void ParseFiles_MultipleFiles_SplitsCorrectly()
    {
        var input = @"// FILE: Assets/Scripts/Weapons/IWeapon.cs
public interface IWeapon
{
    void Fire();
}
// FILE: Assets/Scripts/Weapons/Pistol.cs
public class Pistol : MonoBehaviour, IWeapon
{
    public void Fire() { }
}
// FILE: Assets/Tests/EditMode/Generated/PistolTests.cs
using NUnit.Framework;

public class PistolTests
{
    [Test]
    public void Fire_DoesNotThrow() { }
}";
        var result = UnityDevCommand.ParseFiles(input);

        result.Should().HaveCount(3);
        result[0].RelativePath.Should().Be("Assets/Scripts/Weapons/IWeapon.cs");
        result[1].RelativePath.Should().Be("Assets/Scripts/Weapons/Pistol.cs");
        result[2].RelativePath.Should().Be("Assets/Tests/EditMode/Generated/PistolTests.cs");
        result[0].Content.Should().Contain("interface IWeapon");
        result[1].Content.Should().Contain("class Pistol");
        result[2].Content.Should().Contain("[Test]");
    }

    [Fact(Timeout = 15000)]
    public void ParseFiles_IgnoresContentBeforeFirstMarker()
    {
        var input = @"Here is some preamble text from the LLM.
This should be ignored.

// FILE: Assets/Scripts/Foo.cs
public class Foo { }";
        var result = UnityDevCommand.ParseFiles(input);

        result.Should().HaveCount(1);
        result[0].RelativePath.Should().Be("Assets/Scripts/Foo.cs");
        result[0].Content.Should().Contain("class Foo");
        result[0].Content.Should().NotContain("preamble");
    }

    [Fact(Timeout = 15000)]
    public void ParseFiles_HandlesBackslashPaths()
    {
        var input = @"// FILE: Assets\Scripts\Bar.cs
public class Bar { }";
        var result = UnityDevCommand.ParseFiles(input);

        result.Should().HaveCount(1);
        result[0].RelativePath.Should().Be("Assets/Scripts/Bar.cs");
    }

    [Fact(Timeout = 15000)]
    public void ValidateProjectRoot_MissingDirectory_ReturnsFalse()
    {
        var originalErr = Console.Error;
        using var errWriter = new StringWriter();
        Console.SetError(errWriter);
        try
        {
            var result = UnityDevCommand.ValidateProjectRoot("/nonexistent/path/to/unity", json: false);
            result.Should().BeFalse();
            errWriter.ToString().Should().Contain("not found", Exactly.Once());
        }
        finally
        {
            Console.SetError(originalErr);
        }
    }

    [Fact(Timeout = 15000)]
    public void ValidateProjectRoot_MissingAssetsFolder_ReturnsFalse()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"nexo-unity-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var originalErr = Console.Error;
            using var errWriter = new StringWriter();
            Console.SetError(errWriter);
            try
            {
                var result = UnityDevCommand.ValidateProjectRoot(tempDir, json: false);
                result.Should().BeFalse();
                errWriter.ToString().Should().Contain("Assets");
            }
            finally
            {
                Console.SetError(originalErr);
            }
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact(Timeout = 15000)]
    public void ValidateProjectRoot_ValidProject_ReturnsTrue()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"nexo-unity-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(tempDir, "Assets"));
        try
        {
            UnityDevCommand.ValidateProjectRoot(tempDir, json: false).Should().BeTrue();
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact(Timeout = 15000)]
    public void ValidateProjectRoot_Json_MissingDirectory_EmitsJsonError()
    {
        var originalOut = Console.Out;
        using var outWriter = new StringWriter();
        Console.SetOut(outWriter);
        try
        {
            var result = UnityDevCommand.ValidateProjectRoot("/nonexistent/json/test", json: true);
            result.Should().BeFalse();

            var output = outWriter.ToString().Trim();
            using var doc = JsonDocument.Parse(output);
            doc.RootElement.GetProperty("ok").GetBoolean().Should().BeFalse();
            doc.RootElement.GetProperty("error").GetString().Should().Contain("not found");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact(Timeout = 15000)]
    public void WriteManifest_CreatesValidJsonManifest()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"nexo-unity-manifest-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var files = new List<string> { "Assets/Scripts/Player.cs", "Assets/Scripts/Enemy.cs" };
            UnityDevCommand.WriteManifest(tempDir, "health system", files);

            var manifestPath = Path.Combine(tempDir, ".nexo-gen-manifest.json");
            File.Exists(manifestPath).Should().BeTrue();

            var content = File.ReadAllText(manifestPath);
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            root.GetProperty("generatedAt").GetString().Should().NotBeNullOrWhiteSpace();
            root.GetProperty("prompt").GetString().Should().Be("health system");

            var filesArr = root.GetProperty("files");
            filesArr.ValueKind.Should().Be(JsonValueKind.Array);
            filesArr.GetArrayLength().Should().Be(2);
            filesArr[0].GetString().Should().Be("Assets/Scripts/Player.cs");
            filesArr[1].GetString().Should().Be("Assets/Scripts/Enemy.cs");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact(Timeout = 15000)]
    public void ExecuteList_EmptyProject_ReturnsZeroSystems()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"nexo-unity-list-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(tempDir, "Assets"));
        try
        {
            var originalOut = Console.Out;
            using var writer = new StringWriter();
            Console.SetOut(writer);
            try
            {
                var exitCode = UnityDevCommand.ExecuteList(tempDir, json: true);
                exitCode.Should().Be(0);

                var output = writer.ToString().Trim();
                using var doc = JsonDocument.Parse(output);
                doc.RootElement.GetProperty("ok").GetBoolean().Should().BeTrue();
                doc.RootElement.GetProperty("systems").GetArrayLength().Should().Be(0);
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact(Timeout = 15000)]
    public void ExecuteList_WithGeneratedSystems_ListsThem()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"nexo-unity-list-{Guid.NewGuid():N}");
        var genDir = Path.Combine(tempDir, "Assets", "Scripts", "Generated", "Weapons");
        Directory.CreateDirectory(genDir);
        File.WriteAllText(Path.Combine(genDir, "Gun.cs"), "public class Gun {}");
        try
        {
            var originalOut = Console.Out;
            using var writer = new StringWriter();
            Console.SetOut(writer);
            try
            {
                var exitCode = UnityDevCommand.ExecuteList(tempDir, json: true);
                exitCode.Should().Be(0);

                var output = writer.ToString().Trim();
                using var doc = JsonDocument.Parse(output);
                doc.RootElement.GetProperty("ok").GetBoolean().Should().BeTrue();
                var systems = doc.RootElement.GetProperty("systems");
                systems.GetArrayLength().Should().Be(1);
                systems[0].GetProperty("name").GetString().Should().Be("Weapons");
                systems[0].GetProperty("fileCount").GetInt32().Should().Be(1);
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact(Timeout = 15000)]
    public void UnityDevCommand_HasExpectedSubcommands()
    {
        var cmd = new UnityDevCommand(() => null!);
        var subNames = cmd.Subcommands.Select(s => s.Name).ToList();
        subNames.Should().Contain("generate");
        subNames.Should().Contain("iterate");
        subNames.Should().Contain("list");
        subNames.Should().Contain("assets");
        subNames.Should().Contain("qa");
        subNames.Should().Contain("fullstack");
    }

    [Fact(Timeout = 15000)]
    public void Generate_Subcommand_HasExpectedOptions()
    {
        var cmd = new UnityDevCommand(() => null!);
        var gen = cmd.Subcommands.Single(s => s.Name == "generate");
        var optNames = gen.Options.Select(o => o.Name).ToList();
        optNames.Should().Contain("project-root");
        optNames.Should().Contain("system");
        optNames.Should().Contain("output-dir");
        optNames.Should().Contain("test-dir");
        optNames.Should().Contain("dry-run");
    }

    [Fact(Timeout = 15000)]
    public void Iterate_Subcommand_HasExpectedOptions()
    {
        var cmd = new UnityDevCommand(() => null!);
        var iter = cmd.Subcommands.Single(s => s.Name == "iterate");
        var optNames = iter.Options.Select(o => o.Name).ToList();
        optNames.Should().Contain("project-root");
        optNames.Should().Contain("change");
        optNames.Should().Contain("system-dir");
    }

    [Fact(Timeout = 15000)]
    public void Init_Subcommand_Exists()
    {
        var cmd = new UnityDevCommand(() => null!);
        cmd.Subcommands.Should().Contain(s => s.Name == "init");
    }

    [Fact(Timeout = 15000)]
    public void ExecuteInit_CreatesProjectStructure()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "nexo-unity-init-" + Guid.NewGuid().ToString("N")[..8]);
        try
        {
            var result = UnityDevCommand.ExecuteInit(tempDir, "TestGame", false);
            result.Should().Be(0);

            Directory.Exists(Path.Combine(tempDir, "Assets")).Should().BeTrue();
            Directory.Exists(Path.Combine(tempDir, "Assets", "Scripts")).Should().BeTrue();
            Directory.Exists(Path.Combine(tempDir, "Assets", "Scripts", "Generated")).Should().BeTrue();
            Directory.Exists(Path.Combine(tempDir, "Assets", "Prefabs")).Should().BeTrue();
            Directory.Exists(Path.Combine(tempDir, "Assets", "Tests", "EditMode")).Should().BeTrue();
            Directory.Exists(Path.Combine(tempDir, "Packages")).Should().BeTrue();
            Directory.Exists(Path.Combine(tempDir, "ProjectSettings")).Should().BeTrue();
            Directory.Exists(Path.Combine(tempDir, ".nexo")).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact(Timeout = 15000)]
    public void ExecuteInit_CreatesManifestWithDependencies()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "nexo-unity-init-" + Guid.NewGuid().ToString("N")[..8]);
        try
        {
            UnityDevCommand.ExecuteInit(tempDir, "TestGame", false);

            var manifestPath = Path.Combine(tempDir, "Packages", "manifest.json");
            File.Exists(manifestPath).Should().BeTrue();
            var json = File.ReadAllText(manifestPath);
            json.Should().Contain("com.unity.inputsystem");
            json.Should().Contain("com.unity.netcode.gameobjects");
            json.Should().Contain("com.unity.test-framework");
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact(Timeout = 15000)]
    public void ExecuteInit_CreatesAssemblyDefinitions()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "nexo-unity-init-" + Guid.NewGuid().ToString("N")[..8]);
        try
        {
            UnityDevCommand.ExecuteInit(tempDir, "MyGame", false);

            var asmdef = Path.Combine(tempDir, "Assets", "Scripts", "MyGame.asmdef");
            File.Exists(asmdef).Should().BeTrue();
            var asmJson = File.ReadAllText(asmdef);
            asmJson.Should().Contain("\"name\": \"MyGame\"");

            var testAsmdef = Path.Combine(tempDir, "Assets", "Tests", "EditMode", "MyGame.Tests.asmdef");
            File.Exists(testAsmdef).Should().BeTrue();
            var testJson = File.ReadAllText(testAsmdef);
            testJson.Should().Contain("\"name\": \"MyGame.Tests\"");
            testJson.Should().Contain("UNITY_INCLUDE_TESTS");
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact(Timeout = 15000)]
    public void ExecuteInit_CreatesGitignore()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "nexo-unity-init-" + Guid.NewGuid().ToString("N")[..8]);
        try
        {
            UnityDevCommand.ExecuteInit(tempDir, "TestGame", false);

            var gitignore = Path.Combine(tempDir, ".gitignore");
            File.Exists(gitignore).Should().BeTrue();
            var content = File.ReadAllText(gitignore);
            content.Should().Contain("/[Ll]ibrary/");
            content.Should().Contain(".nexo/");
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact(Timeout = 15000)]
    public void ExecuteInit_CreatesNexoConfig()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "nexo-unity-init-" + Guid.NewGuid().ToString("N")[..8]);
        try
        {
            UnityDevCommand.ExecuteInit(tempDir, "TestGame", false);

            var configPath = Path.Combine(tempDir, ".nexo", "config.json");
            File.Exists(configPath).Should().BeTrue();
            var json = File.ReadAllText(configPath);
            json.Should().Contain("ollama");
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact(Timeout = 15000)]
    public void ExecuteInit_IdempotentOnExistingProject()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "nexo-unity-init-" + Guid.NewGuid().ToString("N")[..8]);
        try
        {
            UnityDevCommand.ExecuteInit(tempDir, "TestGame", false);
            var firstManifest = File.ReadAllText(Path.Combine(tempDir, "Packages", "manifest.json"));

            UnityDevCommand.ExecuteInit(tempDir, "TestGame", false);
            var secondManifest = File.ReadAllText(Path.Combine(tempDir, "Packages", "manifest.json"));

            firstManifest.Should().Be(secondManifest, "init should be idempotent");
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact(Timeout = 15000)]
    public void ExecuteInit_JsonOutput_ReturnsStructuredResult()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "nexo-unity-init-" + Guid.NewGuid().ToString("N")[..8]);
        try
        {
            var sw = new StringWriter();
            Console.SetOut(sw);
            UnityDevCommand.ExecuteInit(tempDir, "TestGame", true);
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });

            var output = sw.ToString();
            output.Should().Contain("\"ok\": true");
            output.Should().Contain("\"projectName\": \"TestGame\"");
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact(Timeout = 15000)]
    public void Assets_Subcommand_HasExpectedOptions()
    {
        var cmd = new UnityDevCommand(() => null!);
        var assets = cmd.Subcommands.Single(s => s.Name == "assets");
        var optNames = assets.Options.Select(o => o.Name).ToList();
        optNames.Should().Contain("project-root");
        optNames.Should().Contain("type");
        optNames.Should().Contain("description");
        optNames.Should().Contain("format-json");
    }

    [Fact(Timeout = 15000)]
    public void Qa_Subcommand_HasExpectedOptions()
    {
        var cmd = new UnityDevCommand(() => null!);
        var qa = cmd.Subcommands.Single(s => s.Name == "qa");
        var optNames = qa.Options.Select(o => o.Name).ToList();
        optNames.Should().Contain("project-root");
        optNames.Should().Contain("max-iterations");
        optNames.Should().Contain("format-json");
        optNames.Should().Contain("unity-path");
    }

    [Fact(Timeout = 15000)]
    public void Fullstack_Subcommand_Exists()
    {
        var cmd = new UnityDevCommand(() => null!);
        var fullstack = cmd.Subcommands.SingleOrDefault(s => s.Name == "fullstack");
        fullstack.Should().NotBeNull();

        var optNames = fullstack!.Options.Select(o => o.Name).ToList();
        optNames.Should().Contain("project-root");
        optNames.Should().Contain("game-description");
        optNames.Should().Contain("format-json");
    }

    [Fact(Timeout = 15000)]
    public void FindUnityEditor_ReturnsNullWhenNotInstalled()
    {
        var result = UnityDevCommand.FindUnityEditor();
        // On CI/test machines without Unity, this should return null (or a valid path if installed)
        // We just verify it doesn't throw
        (result == null || File.Exists(result)).Should().BeTrue();
    }

    [Fact(Timeout = 15000)]
    public void BuildAssetPrompt_ContainsAssetTypeAndDescription()
    {
        var prompt = UnityDevCommand.BuildAssetPrompt("material", "shiny gold material");
        prompt.Should().Contain("material");
        prompt.Should().Contain("shiny gold material");
        prompt.Should().Contain("MaterialDescriptor");
        prompt.Should().Contain("// FILE:");
    }

    [Fact(Timeout = 15000)]
    public void BuildAssetPrompt_AllTypesProduceSchemaHints()
    {
        var types = new[] { "material", "prefab", "scene", "audio", "animation", "soundbank", "animationset", "ui" };
        foreach (var type in types)
        {
            var prompt = UnityDevCommand.BuildAssetPrompt(type, "test");
            prompt.Should().Contain("Descriptor", $"type '{type}' should reference a descriptor schema");
        }
    }

    [Fact(Timeout = 15000)]
    public void Truncate_ShortText_ReturnsUnchanged()
    {
        UnityDevCommand.Truncate("hello", 100).Should().Be("hello");
    }

    [Fact(Timeout = 15000)]
    public void Truncate_LongText_IsTruncated()
    {
        var long_text = new string('x', 200);
        var result = UnityDevCommand.Truncate(long_text, 50);
        result.Should().HaveLength(50 + "\n... (truncated)".Length);
        result.Should().EndWith("(truncated)");
    }

    [Fact(Timeout = 15000)]
    public void Truncate_NullOrEmpty_ReturnsInput()
    {
        UnityDevCommand.Truncate("", 10).Should().Be("");
        UnityDevCommand.Truncate(null!, 10).Should().BeNull();
    }

    [Fact(Timeout = 15000)]
    public void BuildAssetPrompt_Animation_ContainsSchemaHints()
    {
        var prompt = UnityDevCommand.BuildAssetPrompt("animation", "FPS character animator");
        prompt.Should().Contain("AnimationDescriptor");
        prompt.Should().Contain("state machine");
        prompt.Should().Contain("Transitions");
        prompt.Should().Contain("Layers");
        prompt.Should().Contain("AvatarMask");
        prompt.Should().Contain("AnimationEvents");
    }

    [Fact(Timeout = 15000)]
    public void BuildAssetPrompt_SoundBank_ContainsSchemaHints()
    {
        var prompt = UnityDevCommand.BuildAssetPrompt("soundbank", "weapon sounds");
        prompt.Should().Contain("SoundBankDescriptor");
        prompt.Should().Contain("SelectionMode");
        prompt.Should().Contain("CooldownSeconds");
        prompt.Should().Contain("MaxConcurrent");
        prompt.Should().Contain("AudioDescriptorIds");
    }

    [Fact(Timeout = 15000)]
    public void BuildAssetPrompt_AnimationSet_ContainsSchemaHints()
    {
        var prompt = UnityDevCommand.BuildAssetPrompt("animationset", "FPS character locomotion");
        prompt.Should().Contain("AnimationSetDescriptor");
        prompt.Should().Contain("BlendTree");
        prompt.Should().Contain("CrossfadeDuration");
        prompt.Should().Contain("GameplayState");
        prompt.Should().Contain("Threshold");
    }

    [Fact(Timeout = 15000)]
    public void Animation_SoundBank_AnimationSet_AreValidAssetTypes()
    {
        var prompt1 = UnityDevCommand.BuildAssetPrompt("animation", "test");
        var prompt2 = UnityDevCommand.BuildAssetPrompt("soundbank", "test");
        var prompt3 = UnityDevCommand.BuildAssetPrompt("animationset", "test");

        prompt1.Should().Contain("AnimationDescriptor");
        prompt2.Should().Contain("SoundBankDescriptor");
        prompt3.Should().Contain("AnimationSetDescriptor");
    }

    [Fact(Timeout = 15000)]
    public void AssetDescriptor_JsonRoundTrip_Material()
    {
        var original = new Nexo.GameDomain.Assets.MaterialDescriptor
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
        var restored = JsonSerializer.Deserialize<Nexo.GameDomain.Assets.MaterialDescriptor>(json, opts);

        restored.Should().NotBeNull();
        restored!.Id.Should().Be("mat-1");
        restored.RenderMode.Should().Be("Transparent");
    }

    [Fact(Timeout = 15000)]
    public void AssetDescriptor_JsonRoundTrip_Audio()
    {
        var original = new Nexo.GameDomain.Assets.AudioDescriptor
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
            Variations = new[]
            {
                new Nexo.GameDomain.Assets.AudioVariation { VariantId = "v1", PitchOffset = 0.05, ClipSuffix = "_01" }
            },
            Tags = new[] { "combat", "explosion" }
        };

        var opts = new JsonSerializerOptions(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };
        var json = JsonSerializer.Serialize(original, opts);
        var restored = JsonSerializer.Deserialize<Nexo.GameDomain.Assets.AudioDescriptor>(json, opts);

        restored.Should().NotBeNull();
        restored!.Id.Should().Be("explosion");
        restored.SubCategory.Should().Be("explosion");
        restored.SpatialBlend.Should().Be(1.0);
        restored.MixerGroup.Should().Be("SFX");
        restored.Variations.Should().HaveCount(1);
        restored.Variations[0].VariantId.Should().Be("v1");
        restored.Tags.Should().HaveCount(2);
    }

    [Fact(Timeout = 15000)]
    public void Pin_Subcommand_Exists()
    {
        var cmd = new UnityDevCommand(() => null!);
        var pin = cmd.Subcommands.SingleOrDefault(s => s.Name == "pin");
        pin.Should().NotBeNull();

        var optNames = pin!.Options.Select(o => o.Name).ToList();
        optNames.Should().Contain("project-root");
        optNames.Should().Contain("file");
        optNames.Should().Contain("field");
        optNames.Should().Contain("value");
        optNames.Should().Contain("unpin");
    }

    [Fact(Timeout = 15000)]
    public void Compose_Subcommand_Exists()
    {
        var cmd = new UnityDevCommand(() => null!);
        var compose = cmd.Subcommands.SingleOrDefault(s => s.Name == "compose");
        compose.Should().NotBeNull();

        var optNames = compose!.Options.Select(o => o.Name).ToList();
        optNames.Should().Contain("project-root");
        optNames.Should().Contain("config");
        optNames.Should().Contain("format-json");
    }

    [Fact(Timeout = 15000)]
    public void Generate_Subcommand_HasTemplateOption()
    {
        var cmd = new UnityDevCommand(() => null!);
        var gen = cmd.Subcommands.Single(s => s.Name == "generate");
        var optNames = gen.Options.Select(o => o.Name).ToList();
        optNames.Should().Contain("template");
    }

    [Fact(Timeout = 15000)]
    public void ExecutePin_PinsField()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"nexo-pin-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var descriptorFile = Path.Combine("weapons", "rifle.json");
        var fullDescriptorPath = Path.Combine(tempDir, descriptorFile);
        Directory.CreateDirectory(Path.GetDirectoryName(fullDescriptorPath)!);
        File.WriteAllText(fullDescriptorPath, "{}");
        try
        {
            var sw = new StringWriter();
            Console.SetOut(sw);
            var code = UnityDevCommand.ExecutePin(tempDir, descriptorFile, "damage", "42", false, false);
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });

            code.Should().Be(0);

            var overridePath = Path.ChangeExtension(fullDescriptorPath, ".overrides.json");
            File.Exists(overridePath).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact(Timeout = 15000)]
    public void ExecutePin_UnpinsField()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"nexo-pin-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var descriptorFile = Path.Combine("weapons", "rifle.json");
        var fullDescriptorPath = Path.Combine(tempDir, descriptorFile);
        Directory.CreateDirectory(Path.GetDirectoryName(fullDescriptorPath)!);
        File.WriteAllText(fullDescriptorPath, "{}");
        try
        {
            UnityDevCommand.ExecutePin(tempDir, descriptorFile, "damage", "42", false, false);

            var sw = new StringWriter();
            Console.SetOut(sw);
            var code = UnityDevCommand.ExecutePin(tempDir, descriptorFile, "damage", null, true, false);
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });

            code.Should().Be(0);

            var overridePath = Path.ChangeExtension(fullDescriptorPath, ".overrides.json");
            File.Exists(overridePath).Should().BeFalse();
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact(Timeout = 15000)]
    public void ExecutePin_NoValue_ReturnsError()
    {
        var code = UnityDevCommand.ExecutePin("/tmp", "file.json", "field", null, false, false);
        code.Should().Be(1);
    }

    [Fact(Timeout = 15000)]
    public void BuildGeneratePrompt_InjectsConstraintFragment()
    {
        var prompt = UnityDevCommand.BuildGeneratePrompt(
            "weapon system", "Assets/Scripts/Generated", "Assets/Tests",
            constraintFragment: "\nProject constraints:\n- Max 200 lines per file");

        prompt.Should().Contain("Max 200 lines per file");
        prompt.Should().Contain("weapon system");
    }

    [Fact(Timeout = 15000)]
    public void BuildGeneratePrompt_InjectsTemplateFragment()
    {
        var prompt = UnityDevCommand.BuildGeneratePrompt(
            "weapon system", "Assets/Scripts/Generated", "Assets/Tests",
            templateFragment: "\nFixed values from template:\n  name: Shotgun");

        prompt.Should().Contain("Fixed values from template");
        prompt.Should().Contain("name: Shotgun");
    }

    [Fact(Timeout = 15000)]
    public void BuildGeneratePrompt_InjectsCompositionContext()
    {
        var prompt = UnityDevCommand.BuildGeneratePrompt(
            "combat system", "Assets/Scripts/Generated", "Assets/Tests",
            compositionContext: "Health system provides IDamageable interface");

        prompt.Should().Contain("Context from upstream systems");
        prompt.Should().Contain("Health system provides IDamageable interface");
    }
}
