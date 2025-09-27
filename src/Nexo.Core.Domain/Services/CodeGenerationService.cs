using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.Extensions.Logging;

namespace Nexo.Core.Domain.Services
{
    /// <summary>
    /// Service for generating typed C# code using Roslyn.
    /// This class acts as an orchestrator, delegating specific functionalities to partial class implementations.
    /// </summary>
    public partial class CodeGenerationService : ICodeGenerationService
    {
        private readonly ILogger<CodeGenerationService> _logger;
        private readonly Dictionary<string, MetadataReference> _references;

        public CodeGenerationService(ILogger<CodeGenerationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _references = new Dictionary<string, MetadataReference>();
            InitializeReferences();
        }
    }
}