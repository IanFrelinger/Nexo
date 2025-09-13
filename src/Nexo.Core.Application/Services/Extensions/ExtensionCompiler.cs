using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Extensions;
using Nexo.Core.Domain.Interfaces; // For IPlugin
using Nexo.Core.Domain.Models.Extensions;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.Extensions
{
    public class ExtensionCompiler : IExtensionCompiler
    {
        private readonly ILogger<ExtensionCompiler> _logger;

        public ExtensionCompiler(ILogger<ExtensionCompiler> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<ExtensionGenerationResult> CompileExtensionAsync(string code, string assemblyName)
        {
            var result = new ExtensionGenerationResult
            {
                GeneratedCode = code
            };
            result.IsSuccess = false;

            try
            {
                _logger.LogInformation("Compiling extension: {AssemblyName}", assemblyName);

                // Parse the C# code
                var syntaxTree = CSharpSyntaxTree.ParseText(code, path: $"{assemblyName}.cs");

                // Get required assembly references
                var references = GetRequiredReferences();

                // Create compilation options
                var compilationOptions = new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                    .WithOptimizationLevel(OptimizationLevel.Release)
                    .WithPlatform(Platform.AnyCpu)
                    .WithAllowUnsafe(false);

                // Create compilation
                var compilation = CSharpCompilation.Create(
                    assemblyName: assemblyName,
                    syntaxTrees: new[] { syntaxTree },
                    references: references,
                    options: compilationOptions);

                // Compile to memory stream
                using var ms = new MemoryStream();
                var emitResult = compilation.Emit(ms);

                if (emitResult.Success)
                {
                    result.CompiledAssembly = ms.ToArray();
                    result.IsSuccess = true;

                    _logger.LogInformation("Extension compiled successfully: {AssemblyName} ({Size} bytes)", 
                        assemblyName, result.CompiledAssembly.Length);
                }
                else
                {
                    // Process compilation errors
                    foreach (var diagnostic in emitResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
                    {
                        var location = diagnostic.Location;
                        var lineSpan = location.GetLineSpan();
                        
                        result.AddCompilationError(
                            diagnostic.GetMessage(),
                            lineSpan.StartLinePosition.Line + 1,
                            lineSpan.StartLinePosition.Character + 1,
                            diagnostic.Id);
                    }

                    _logger.LogWarning("Extension compilation failed: {AssemblyName} ({ErrorCount} errors)", 
                        assemblyName, result.CompilationErrors.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error compiling extension: {AssemblyName}", assemblyName);
                result.AddCompilationError($"Unexpected compilation error: {ex.Message}", 0, 0, "COMP0001");
            }

            return Task.FromResult(result);
        }

        private MetadataReference[] GetRequiredReferences()
        {
            return new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(IPlugin).Assembly.Location), // Reference our IPlugin
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Console").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Collections").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Threading.Tasks").Location),
                MetadataReference.CreateFromFile(Assembly.Load("Microsoft.Extensions.Logging.Abstractions").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Private.CoreLib").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Linq").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.IO").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Reflection").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime.Loader").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime.InteropServices").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.ComponentModel").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Diagnostics.Process").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Net.Http").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Text.Json").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Xml.Linq").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Security.Claims").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Security.Cryptography.Algorithms").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Security.Cryptography.X509Certificates").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Text.RegularExpressions").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Threading.Channels").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Threading.Tasks.Extensions").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Threading.Tasks.Parallel").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Transactions").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.ValueTuple").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Xml.ReaderWriter").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Xml.XDocument").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Xml.XmlDocument").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Xml.XPath").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Xml.XPath.XDocument").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Net.Primitives").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Net.Sockets").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Net.Security").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Net.Http.Json").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Net.WebSockets").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Net.WebSockets.Client").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Net.HttpListener").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Net.Mail").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Net.NameResolution").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Net.NetworkInformation").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Net.Ping").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Net.Requests").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Net.ServicePoint").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Net.WebClient").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Net.WebHeaderCollection").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Net.WebProxy").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Numerics").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Numerics.Vectors").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.ObjectModel").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Reflection.DispatchProxy").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Reflection.Emit").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Reflection.Emit.ILGeneration").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Reflection.Emit.Lightweight").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Reflection.Extensions").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Reflection.Metadata").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Reflection.Primitives").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Reflection.TypeExtensions").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Resources.Reader").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Resources.ResourceManager").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Resources.Writer").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime.CompilerServices.Unsafe").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime.CompilerServices.VisualC").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime.Extensions").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime.Handles").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime.InteropServices.JavaScript").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime.InteropServices.RuntimeInformation").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime.Intrinsics").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime.Numerics").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime.Serialization").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime.Serialization.Formatters").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime.Serialization.Json").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime.Serialization.Primitives").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime.Serialization.Xml").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Security.AccessControl").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Security.Claims").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Security.Cryptography.Cng").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Security.Cryptography.Csp").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Security.Cryptography.Encoding").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Security.Cryptography.OpenSsl").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Security.Cryptography.Primitives").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Security.Cryptography.X509Certificates").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Security.Principal").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Security.Principal.Windows").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Security.SecureString").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.ServiceModel.Web").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.ServiceProcess").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Text.Encoding.CodePages").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Text.Encoding.Extensions").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Text.Encodings.Web").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Threading.Thread").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Threading.ThreadPool").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Threading.Timer").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Transactions.Local").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Web").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Web.HttpUtility").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Windows").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Xml.Serialization").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Xml.XmlDocument").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Xml.XPath").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Xml.XPath.XDocument").Location),
                MetadataReference.CreateFromFile(Assembly.Load("WindowsBase").Location)
            };
        }
    }
}