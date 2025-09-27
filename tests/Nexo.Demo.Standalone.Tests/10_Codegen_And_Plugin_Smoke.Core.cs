using Nexo.Demo.Tests.Support;
using FluentAssertions;
using Xunit;

namespace Nexo.Demo.Tests;
{
            ? "using Microsoft.CodeAnalysis;\npublic interface IGeneratedRecipeExecutor { Task<ExecutionResult> ExecuteAsync(Dictionary<string, string> inputs); }\npublic partial class GeneratedRecipe : IGeneratedRecipeExecutor { public async Task<ExecutionResult> ExecuteAsync(Dictionary<string, string> inputs) { return new ExecutionResult { Success = true }; } }\npublic partial class ExecutionResult { public bool Success { get; set; } }"
}