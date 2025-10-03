using Director.Core.Protocol;

namespace Director.Avalonia.Services;

public class NexoCommandService
{
    public DirectorCommand CreateValidationCommand(string? workingDirectory = null)
    {
        return new DirectorCommand(
            Guid.NewGuid().ToString("N"),
            CommandTypes.RunNexo,
            new RunNexoPayload("validate", new[] { "--filter", "Category=Architecture", "--format-json" }, workingDirectory)
        );
    }

    public DirectorCommand CreateAnalysisCommand(string? workingDirectory = null)
    {
        return new DirectorCommand(
            Guid.NewGuid().ToString("N"),
            CommandTypes.RunNexo,
            new RunNexoPayload("analyze", new[] { "--format-json" }, workingDirectory)
        );
    }

    public DirectorCommand CreateListAgentsCommand(string? workingDirectory = null)
    {
        return new DirectorCommand(
            Guid.NewGuid().ToString("N"),
            CommandTypes.RunNexo,
            new RunNexoPayload("agent", new[] { "--list", "--format-json" }, workingDirectory)
        );
    }

    public DirectorCommand CreateCustomNexoCommand(string command, string[] args, string? workingDirectory = null)
    {
        return new DirectorCommand(
            Guid.NewGuid().ToString("N"),
            CommandTypes.RunNexo,
            new RunNexoPayload(command, args, workingDirectory)
        );
    }
}
