using Nexo.Shared.Models.Assembly;

namespace Nexo.Core.Application.Commands.Assembly
{
    /// <summary>
    /// Input for decompiling an assembly
    /// </summary>
    public class DecompileAssemblyInput
    {
        public string AssemblyPath { get; set; } = string.Empty;
        public DecompilationSettings Settings { get; set; } = new DecompilationSettings();
    }
}
