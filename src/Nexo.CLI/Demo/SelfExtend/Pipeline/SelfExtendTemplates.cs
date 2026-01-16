namespace Nexo.CLI.Demo.SelfExtend.Pipeline;

internal static class SelfExtendTemplates
{
    public static string RenderGeneratedCommand(SelfExtendContext ctx, string message)
    {
        var className = ctx.GeneratedCommandClassName;
        var cmd = ctx.Spec.CommandName;
        var msg = message.Replace("\"", "\\\"");

        return $$"""
        using System.CommandLine;
        using System.Text.Json;

        namespace Nexo.CLI.Commands.DemoGenerated;

        /// <summary>
        /// Demo-generated command created by `nexo demo self-extend`.
        /// </summary>
        public sealed class {{className}} : Command
        {
            public {{className}}() : base("{{cmd}}", "Demo-generated command")
            {
                this.SetHandler(() =>
                {
                    Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, command = "{{cmd}}", message = "{{msg}}" }));
                    Environment.ExitCode = 0;
                });
            }
        }
        """;
    }
}

