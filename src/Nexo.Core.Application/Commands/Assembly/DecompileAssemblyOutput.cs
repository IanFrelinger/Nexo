using Nexo.Shared.Models.Assembly;

namespace Nexo.Core.Application.Commands.Assembly
{
    /// <summary>
    /// Output for decompiling an assembly
    /// </summary>
    public class DecompileAssemblyOutput
    {
        public string AssemblyPath { get; set; } = string.Empty;
        public DecompilationResult DecompilationResult { get; set; } = new DecompilationResult();
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
