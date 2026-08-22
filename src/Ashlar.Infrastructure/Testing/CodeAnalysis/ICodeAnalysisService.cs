namespace Ashlar.Infrastructure.Testing.CodeAnalysis;

/// <summary>
/// Abstraction for code compilation and decompilation services.
/// 
/// Provides portable code analysis capabilities that work across different platforms
/// without being tied to specific implementations or command-line tools.
/// </summary>
public interface ICodeAnalysisService
{
    /// <summary>
    /// Compiles C# source code into an assembly.
    /// </summary>
    /// <param name="sourceCode">C# source code to compile</param>
    /// <param name="assemblyName">Name for the output assembly</param>
    /// <param name="outputPath">Path where the compiled assembly should be written</param>
    /// <param name="references">Optional assembly references (paths to .dll files)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Compilation result with success status and diagnostics</returns>
    Task<CompilationResult> CompileAsync(
        string sourceCode,
        string assemblyName,
        string outputPath,
        IEnumerable<string>? references = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Decompiles a .NET assembly back to C# source code.
    /// </summary>
    /// <param name="assemblyPath">Path to the assembly to decompile</param>
    /// <param name="outputPath">Path where the decompiled source should be written</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Decompilation result with success status and source code</returns>
    Task<DecompilationResult> DecompileAsync(
        string assemblyPath,
        string outputPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes an assembly and extracts metadata.
    /// </summary>
    /// <param name="assemblyPath">Path to the assembly to analyze</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Analysis result with assembly metadata</returns>
    Task<AssemblyAnalysisResult> AnalyzeAssemblyAsync(
        string assemblyPath,
        CancellationToken cancellationToken = default);
}
