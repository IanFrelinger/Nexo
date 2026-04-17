using System.Text.Json;
using FluentAssertions;
using Nexo.CLI.Commands;
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
}
