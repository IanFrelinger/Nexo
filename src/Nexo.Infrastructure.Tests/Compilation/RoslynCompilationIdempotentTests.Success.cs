using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Models;
using Nexo.Infrastructure.Compilation;
using Xunit;

namespace Nexo.Infrastructure.Tests.Compilation
{
    /// <summary>
    /// Success test cases for Roslyn compilation
    /// </summary>
    public partial class RoslynCompilationIdempotentTests
    {
        #region Idempotent Rebuild Tests

        [Fact]
        public async Task CompileToAssemblyAsync_IdenticalCode_ShouldProduceSameResult()
        {
            // Arrange
            var code = @"
                using System;
                public class TestClass
                {
                    public string GetMessage() => ""Hello World"";
                }
            ";

            // Act - Compile multiple times
            var result1 = await _compiler.CompileToAssemblyAsync(code);
            var result2 = await _compiler.CompileToAssemblyAsync(code);

            // Assert
            Assert.True(result1.Success);
            Assert.True(result2.Success);
            Assert.NotNull(result1.Assembly);
            Assert.NotNull(result2.Assembly);
            Assert.Equal(result1.Assembly.Length, result2.Assembly.Length);
        }

        [Fact]
        public async Task CompileToAssemblyAsync_IdenticalCodeWithDifferentWhitespace_ShouldProduceSameResult()
        {
            // Arrange
            var code1 = @"
                using System;
                public class TestClass
                {
                    public string GetMessage() => ""Hello World"";
                }
            ";

            var code2 = @"
using System;
public class TestClass
{
    public string GetMessage() => ""Hello World"";
}
            ";

            // Act
            var result1 = await _compiler.CompileToAssemblyAsync(code1);
            var result2 = await _compiler.CompileToAssemblyAsync(code2);

            // Assert
            Assert.True(result1.Success);
            Assert.True(result2.Success);
            Assert.Equal(result1.Assembly.Length, result2.Assembly.Length);
        }

        [Fact]
        public async Task CompileToAssemblyAsync_IdenticalCodeWithDifferentComments_ShouldProduceSameResult()
        {
            // Arrange
            var code1 = @"
                using System;
                public class TestClass
                {
                    public string GetMessage() => ""Hello World"";
                }
            ";

            var code2 = @"
                using System;
                // This is a comment
                public class TestClass
                {
                    public string GetMessage() => ""Hello World""; // Another comment
                }
            ";

            // Act
            var result1 = await _compiler.CompileToAssemblyAsync(code1);
            var result2 = await _compiler.CompileToAssemblyAsync(code2);

            // Assert
            Assert.True(result1.Success);
            Assert.True(result2.Success);
            Assert.Equal(result1.Assembly.Length, result2.Assembly.Length);
        }

        [Fact]
        public async Task CompileToAssemblyAsync_ConcurrentCompilation_ShouldBeThreadSafe()
        {
            // Arrange
            var code = @"
                using System;
                public class TestClass
                {
                    public string GetMessage() => ""Hello World"";
                }
            ";

            var tasks = new List<Task<CompilationResult>>();

            // Act - Compile concurrently
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(_compiler.CompileToAssemblyAsync(code));
            }

            var results = await Task.WhenAll(tasks);

            // Assert
            Assert.All(results, result => Assert.True(result.Success));
            Assert.All(results, result => Assert.NotNull(result.Assembly));
            
            // All results should be identical
            var firstResult = results.First();
            Assert.All(results, result => Assert.Equal(firstResult.Assembly.Length, result.Assembly.Length));
        }

        #endregion

        #region Fix and Compile Tests

        [Fact]
        public async Task FixAndCompileAsync_SimpleSyntaxError_ShouldFixAndCompile()
        {
            // Arrange
            var codeWithError = @"
                using System;
                public class TestClass
                {
                    public string GetMessage() => ""Hello World""
                }
            ";

            var errors = new[] { "Missing semicolon" };

            // Act
            var result = await _compiler.FixAndCompileAsync(codeWithError, errors);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Assembly);
        }

        [Fact]
        public async Task FixAndCompileAsync_TypeError_ShouldFixAndCompile()
        {
            // Arrange
            var codeWithError = @"
                using System;
                public class TestClass
                {
                    public string GetMessage() => 123
                }
            ";

            var errors = new[] { "Cannot convert int to string" };

            // Act
            var result = await _compiler.FixAndCompileAsync(codeWithError, errors);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Assembly);
        }

        #endregion

        #region Minimal Repro Tests

        [Theory]
        [InlineData("public class Test { public int GetValue() => 42; }")]
        [InlineData("using System; public class Test { public string GetMessage() => \"Hello\"; }")]
        [InlineData("public class Test { public void DoSomething() { } }")]
        [InlineData("public class Test { public bool IsValid() => true; }")]
        public async Task CompileToAssemblyAsync_MinimalCode_ShouldCompileSuccessfully(string code)
        {
            // Act
            var result = await _compiler.CompileToAssemblyAsync(code);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Assembly);
            Assert.True(result.Assembly.Length > 0);
        }

        #endregion

        #region Assembly Validation Tests

        [Fact]
        public async Task CompileToAssemblyAsync_GeneratedAssembly_ShouldBeLoadable()
        {
            // Arrange
            var code = @"
                using System;
                public class TestClass
                {
                    public string GetMessage() => ""Hello World"";
                    public int GetNumber() => 42;
                }
            ";

            // Act
            var result = await _compiler.CompileToAssemblyAsync(code);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Assembly);
            
            // Verify assembly can be loaded
            var assembly = System.Reflection.Assembly.Load(result.Assembly);
            Assert.NotNull(assembly);
            
            var testClass = assembly.GetType("TestClass");
            Assert.NotNull(testClass);
            
            var instance = Activator.CreateInstance(testClass);
            Assert.NotNull(instance);
            
            var getMessageMethod = testClass.GetMethod("GetMessage");
            Assert.NotNull(getMessageMethod);
            
            var message = getMessageMethod.Invoke(instance, null);
            Assert.Equal("Hello World", message);
        }

        [Fact]
        public async Task CompileToAssemblyAsync_GeneratedAssembly_ShouldContainExpectedTypes()
        {
            // Arrange
            var code = @"
                using System;
                public class TestClass
                {
                    public string GetMessage() => ""Hello World"";
                }
                
                public class AnotherClass
                {
                    public int GetValue() => 42;
                }
            ";

            // Act
            var result = await _compiler.CompileToAssemblyAsync(code);

            // Assert
            Assert.True(result.Success);
            
            var assembly = System.Reflection.Assembly.Load(result.Assembly);
            var types = assembly.GetTypes();
            
            Assert.Contains(types, t => t.Name == "TestClass");
            Assert.Contains(types, t => t.Name == "AnotherClass");
        }

        #endregion

        #region Performance Tests

        [Fact]
        public async Task CompileToAssemblyAsync_LargeCode_ShouldCompileWithinReasonableTime()
        {
            // Arrange
            var largeCode = GenerateLargeCode(1000);

            // Act
            var startTime = DateTime.UtcNow;
            var result = await _compiler.CompileToAssemblyAsync(largeCode);
            var duration = DateTime.UtcNow - startTime;

            // Assert
            Assert.True(result.Success);
            Assert.True(duration.TotalSeconds < 10, "Compilation should complete within 10 seconds");
        }

        [Fact]
        public async Task CompileToAssemblyAsync_MultipleCompilations_ShouldNotDegradePerformance()
        {
            // Arrange
            var code = @"
                using System;
                public class TestClass
                {
                    public string GetMessage() => ""Hello World"";
                }
            ";

            var durations = new List<TimeSpan>();

            // Act - Compile multiple times
            for (int i = 0; i < 10; i++)
            {
                var startTime = DateTime.UtcNow;
                var result = await _compiler.CompileToAssemblyAsync(code);
                var duration = DateTime.UtcNow - startTime;
                durations.Add(duration);
                
                Assert.True(result.Success);
            }

            // Assert - Performance should not degrade significantly
            var firstDuration = durations.First();
            var lastDuration = durations.Last();
            var performanceRatio = lastDuration.TotalMilliseconds / firstDuration.TotalMilliseconds;
            
            Assert.True(performanceRatio < 2.0, "Performance should not degrade more than 2x");
        }

        #endregion
    }
}
