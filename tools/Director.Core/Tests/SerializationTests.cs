using Director.Core.Protocol;
using System.Text.Json;
using Xunit;

namespace Director.Core.Tests;

public class SerializationTests
{
    [Fact]
    public void DirectorCommand_SerializationRoundtrip_Success()
    {
        var original = new DirectorCommand("test-id", "TestType", new { Value = "test" });
        
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<DirectorCommand>(json);
        
        Assert.NotNull(deserialized);
        Assert.Equal(original.Id, deserialized.Id);
        Assert.Equal(original.Type, deserialized.Type);
    }

    [Fact]
    public void DirectorEvent_SerializationRoundtrip_Success()
    {
        var original = new DirectorEvent("TestEvent", new { Message = "test" });
        
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<DirectorEvent>(json);
        
        Assert.NotNull(deserialized);
        Assert.Equal(original.Type, deserialized.Type);
    }

    [Fact]
    public void RunNexoPayload_SerializationRoundtrip_Success()
    {
        var original = new RunNexoPayload("validate", new[] { "--format-json" }, "/test/path");
        
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<RunNexoPayload>(json);
        
        Assert.NotNull(deserialized);
        Assert.Equal(original.Command, deserialized.Command);
        Assert.Equal(original.Args, deserialized.Args);
        Assert.Equal(original.WorkingDirectory, deserialized.WorkingDirectory);
    }

    [Fact]
    public void LogLineEvent_SerializationRoundtrip_Success()
    {
        var original = new LogLineEvent("Test log message", LogLevel.Warning);
        
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<LogLineEvent>(json);
        
        Assert.NotNull(deserialized);
        Assert.Equal(original.Line, deserialized.Line);
        Assert.Equal(original.Level, deserialized.Level);
    }

    [Fact]
    public void GateResultEvent_SerializationRoundtrip_Success()
    {
        var original = new GateResultEvent("TestGate", true, "All tests passed");
        
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<GateResultEvent>(json);
        
        Assert.NotNull(deserialized);
        Assert.Equal(original.GateName, deserialized.GateName);
        Assert.Equal(original.Passed, deserialized.Passed);
        Assert.Equal(original.Message, deserialized.Message);
    }

    [Fact]
    public void CommandTypes_Constants_AreValid()
    {
        Assert.Equal("RunNexo", CommandTypes.RunNexo);
        Assert.Equal("OpenScene", CommandTypes.OpenScene);
        Assert.Equal("TogglePlay", CommandTypes.TogglePlay);
        Assert.Equal("ApplyUIMod", CommandTypes.ApplyUIMod);
        Assert.Equal("GetProjectInfo", CommandTypes.GetProjectInfo);
        Assert.Equal("ListScenes", CommandTypes.ListScenes);
        Assert.Equal("Ping", CommandTypes.Ping);
    }
}
