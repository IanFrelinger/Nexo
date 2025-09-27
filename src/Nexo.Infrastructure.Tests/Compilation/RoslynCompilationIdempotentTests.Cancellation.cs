using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Models;
using Nexo.Infrastructure.Compilation;
using Xunit;

namespace Nexo.Infrastructure.Tests.Compilation
{
    /// <summary>
    /// Cancellation test cases for Roslyn compilation
    /// </summary>
    public partial class RoslynCompilationIdempotentTests
    {
        #region Cancellation Tests

        [Fact]
        public async Task CompileToAssemblyAsync_CancellationRequested_ShouldCancelGracefully()
        {
            // Arrange
            var code = @"
                using System;
                public class TestClass
                {
                    public string GetMessage() => ""Hello World"";
                }
            ";

            using var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                _compiler.CompileToAssemblyAsync(code, cts.Token));
        }

        [Fact]
        public async Task CompileToAssemblyAsync_CancellationDuringCompilation_ShouldCancelGracefully()
        {
            // Arrange
            var largeCode = GenerateLargeCode(10000); // Large code to ensure compilation takes time
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100)); // Cancel after 100ms

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                _compiler.CompileToAssemblyAsync(largeCode, cts.Token));
        }

        [Fact]
        public async Task CompileToAssemblyAsync_NoCancellation_ShouldCompleteSuccessfully()
        {
            // Arrange
            var code = @"
                using System;
                public class TestClass
                {
                    public string GetMessage() => ""Hello World"";
                }
            ";

            using var cts = new CancellationTokenSource();

            // Act
            var result = await _compiler.CompileToAssemblyAsync(code, cts.Token);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Assembly);
        }

        [Fact]
        public async Task FixAndCompileAsync_CancellationRequested_ShouldCancelGracefully()
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
            using var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                _compiler.FixAndCompileAsync(codeWithError, errors, cts.Token));
        }

        #endregion

        #region Timeout Tests

        [Fact]
        public async Task CompileToAssemblyAsync_Timeout_ShouldCompleteWithinTimeout()
        {
            // Arrange
            var code = @"
                using System;
                public class TestClass
                {
                    public string GetMessage() => ""Hello World"";
                }
            ";

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            // Act
            var result = await _compiler.CompileToAssemblyAsync(code, cts.Token);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Assembly);
        }

        [Fact]
        public async Task CompileToAssemblyAsync_LongRunningCompilation_ShouldRespectTimeout()
        {
            // Arrange
            var largeCode = GenerateLargeCode(50000); // Very large code
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500)); // Short timeout

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                _compiler.CompileToAssemblyAsync(largeCode, cts.Token));
        }

        #endregion

        #region Concurrent Cancellation Tests

        [Fact]
        public async Task CompileToAssemblyAsync_ConcurrentCancellation_ShouldHandleGracefully()
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
            var cancellationTokens = new List<CancellationTokenSource>();

            // Act - Start multiple compilations with different cancellation tokens
            for (int i = 0; i < 5; i++)
            {
                var cts = new CancellationTokenSource();
                cancellationTokens.Add(cts);
                
                if (i % 2 == 0) // Cancel half of them
                {
                    cts.Cancel();
                }
                
                tasks.Add(_compiler.CompileToAssemblyAsync(code, cts.Token));
            }

            // Assert
            var results = await Task.WhenAll(tasks);
            
            // Some should succeed, some should be cancelled
            var successCount = results.Count(r => r.Success);
            var cancelledCount = results.Count(r => !r.Success && r.Errors.Any(e => e.Contains("cancelled") || e.Contains("canceled")));
            
            Assert.True(successCount > 0, "Some compilations should succeed");
            Assert.True(cancelledCount > 0, "Some compilations should be cancelled");

            // Cleanup
            foreach (var cts in cancellationTokens)
            {
                cts.Dispose();
            }
        }

        #endregion
    }
}
