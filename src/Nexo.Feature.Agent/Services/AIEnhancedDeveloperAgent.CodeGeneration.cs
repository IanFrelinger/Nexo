using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Agent.Models;
using Nexo.Feature.AI.Models;

namespace Nexo.Feature.Agent.Services
{
    /// <summary>
    /// Code generation and testing functionality
    /// </summary>
    public partial class AiEnhancedDeveloperAgent
    {
        /// <summary>
        /// Handles the code generation request by processing the provided requirements and generating corresponding source code.
        /// </summary>
        /// <param name="request">The request object containing context and information required for code generation.</param>
        /// <param name="ct">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>An <see cref="AgentResponse"/> containing the success status and the generated code or a default response if requirements are not provided.</returns>
        private async Task<AgentResponse> HandleCodeGenerationAsync(AgentRequest request, CancellationToken ct)
        {
            var response = new AgentResponse
            {
                Success = true,
                Content = "Code generation completed"
            };

            if (request.Context == null || !request.Context.TryGetValue("requirements", out var value)) return response;
            var requirements = value?.ToString() ?? "No requirements provided";
            var generatedCode = await GenerateCodeAsync(requirements, ct);
            response = new AgentResponse
            {
                Success = true,
                Content = generatedCode
            };

            return response;
        }

        /// <summary>
        /// Handles the generation of test cases for a given piece of code.
        /// </summary>
        /// <param name="request">The request object containing context information, such as the code to be tested.</param>
        /// <param name="ct">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>An <see cref="AgentResponse"/> object indicating the success of the operation and containing the generated test code.</returns>
        private async Task<AgentResponse> HandleTestingAsync(AgentRequest request, CancellationToken ct)
        {
            var response = new AgentResponse
            {
                Success = true,
                Content = "Test generation completed"
            };

            if (request.Context == null || !request.Context.TryGetValue("code_to_test", out var value)) return response;
            var codeToTest = value?.ToString() ?? "No code to test provided";
            var testCode = await GenerateTestsAsync(codeToTest, ct);
            response = new AgentResponse
            {
                Success = true,
                Content = testCode
            };

            return response;
        }

        /// <summary>
        /// Generates C# code based on the provided requirements and returns the generated code as a string.
        /// The generation process includes creating compilable code, XML documentation comments, unit tests,
        /// and usage examples, adhering to best practices and design patterns.
        /// </summary>
        /// <param name="requirements">The textual description of the requirements for the code to be generated.</param>
        /// <param name="ct">A cancellation token to observe while waiting for the task to complete.</param>
        /// <returns>A string containing the generated code based on the given requirements.</returns>
        private async Task<string> GenerateCodeAsync(string requirements, CancellationToken ct)
        {
            var prompt = $@"Generate C# code based on the following requirements:

Requirements: {requirements}

Please provide:
1. Complete, compilable C# code
2. XML documentation comments
3. Unit tests
4. Usage examples

Ensure the code follows C# best practices and design patterns.";

            var request = new ModelRequest
            {
                Input = prompt,
                MaxTokens = 3000,
                Temperature = 0.2
            };

            var response = await ModelOrchestrator.ExecuteAsync(request, ct);
            return response.Response;
        }

        /// <summary>
        /// Generates comprehensive unit tests for the provided C# code using the specified testing framework.
        /// The generated tests include coverage for normal scenarios, edge cases, error conditions, and mock setups where applicable.
        /// </summary>
        /// <param name="codeToTest">The C# code for which unit tests should be generated.</param>
        /// <param name="ct">The cancellation token to observe, allowing the operation to be cancelled.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the generated test code as a string.</returns>
        private async Task<string> GenerateTestsAsync(string codeToTest, CancellationToken ct)
        {
            var prompt = $@"Generate comprehensive unit tests for the following C# code:

{codeToTest}

Please provide:
1. Unit tests using MSTest, NUnit, or xUnit
2. Test cases covering normal scenarios
3. Test cases covering edge cases
4. Test cases covering error conditions
5. Mock setup examples where applicable

Ensure good test coverage and follow testing best practices.";

            var request = new ModelRequest
            {
                Input = prompt,
                MaxTokens = 2500,
                Temperature = 0.2
            };

            var response = await ModelOrchestrator.ExecuteAsync(request, ct);
            return response.Response;
        }
    }
}
