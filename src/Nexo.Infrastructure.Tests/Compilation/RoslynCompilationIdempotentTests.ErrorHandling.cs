using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Models;
using Nexo.Infrastructure.Compilation;
using Xunit;

namespace Nexo.Infrastructure.Tests.Compilation
{
    /// <summary>
    /// Error handling test cases for Roslyn compilation
    /// </summary>
    public partial class RoslynCompilationIdempotentTests
    {
        #region Compilation Failure Diagnostics Tests

        [Fact]
        public async Task CompileToAssemblyAsync_SyntaxError_ShouldProvideDetailedDiagnostics()
        {
            // Arrange
            var codeWithSyntaxError = @"
                using System;
                public class TestClass
                {
                    public string GetMessage() => ""Hello World""
                    // Missing semicolon
                }
            ";

            // Act
            var result = await _compiler.CompileToAssemblyAsync(codeWithSyntaxError);

            // Assert
            Assert.False(result.Success);
            Assert.NotEmpty(result.Errors);
            Assert.Contains(result.Errors, error => 
                error.Contains(";") || error.Contains("semicolon") || error.Contains("expected"));
        }

        [Fact]
        public async Task CompileToAssemblyAsync_TypeError_ShouldProvideDetailedDiagnostics()
        {
            // Arrange
            var codeWithTypeError = @"
                using System;
                public class TestClass
                {
                    public string GetMessage() => 123; // Type mismatch
                }
            ";

            // Act
            var result = await _compiler.CompileToAssemblyAsync(codeWithTypeError);

            // Assert
            Assert.False(result.Success);
            Assert.NotEmpty(result.Errors);
            Assert.Contains(result.Errors, error => 
                error.Contains("int") || error.Contains("string") || error.Contains("conversion"));
        }

        [Fact]
        public async Task CompileToAssemblyAsync_MissingReference_ShouldProvideDetailedDiagnostics()
        {
            // Arrange
            var codeWithMissingReference = @"
                using System;
                using System.NonExistentNamespace;
                public class TestClass
                {
                    public string GetMessage() => ""Hello World"";
                }
            ";

            // Act
            var result = await _compiler.CompileToAssemblyAsync(codeWithMissingReference);

            // Assert
            Assert.False(result.Success);
            Assert.NotEmpty(result.Errors);
            Assert.Contains(result.Errors, error => 
                error.Contains("NonExistentNamespace") || error.Contains("using") || error.Contains("namespace"));
        }

        [Fact]
        public async Task CompileToAssemblyAsync_MultipleErrors_ShouldProvideAllDiagnostics()
        {
            // Arrange
            var codeWithMultipleErrors = @"
                using System;
                public class TestClass
                {
                    public string GetMessage() => 123 // Missing semicolon and type error
                    public int GetNumber() => ""string"" // Type error
                }
            ";

            // Act
            var result = await _compiler.CompileToAssemblyAsync(codeWithMultipleErrors);

            // Assert
            Assert.False(result.Success);
            Assert.True(result.Errors.Count >= 2);
        }

        #endregion

        #region Fix and Compile Error Tests

        [Fact]
        public async Task FixAndCompileAsync_UnfixableError_ShouldReturnFailure()
        {
            // Arrange
            var codeWithError = @"
                using System;
                public class TestClass
                {
                    public string GetMessage() => NonExistentMethod()
                }
            ";

            var errors = new[] { "NonExistentMethod does not exist" };

            // Act
            var result = await _compiler.FixAndCompileAsync(codeWithError, errors);

            // Assert
            Assert.False(result.Success);
            Assert.NotEmpty(result.Errors);
        }

        #endregion

        #region Minimal Error Tests

        [Theory]
        [InlineData("public class Test { public int GetValue() => \"string\"; }")]
        [InlineData("public class Test { public string GetMessage() => 123; }")]
        [InlineData("public class Test { public void DoSomething() => \"not void\"; }")]
        [InlineData("public class Test { public bool IsValid() => \"not bool\"; }")]
        public async Task CompileToAssemblyAsync_MinimalTypeErrors_ShouldFailWithDiagnostics(string code)
        {
            // Act
            var result = await _compiler.CompileToAssemblyAsync(code);

            // Assert
            Assert.False(result.Success);
            Assert.NotEmpty(result.Errors);
        }

        #endregion
    }
}
