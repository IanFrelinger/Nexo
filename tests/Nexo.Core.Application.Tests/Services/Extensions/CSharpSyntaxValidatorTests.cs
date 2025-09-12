using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Nexo.Core.Application.Services.Extensions;
using Nexo.Core.Domain.Composition;

namespace Nexo.Core.Application.Tests.Services.Extensions
{
    /// <summary>
    /// Tests for the CSharpSyntaxValidator service
    /// </summary>
    public class CSharpSyntaxValidatorTests
    {
        private readonly Mock<ILogger<CSharpSyntaxValidator>> _mockLogger;
        private readonly CSharpSyntaxValidator _validator;

        public CSharpSyntaxValidatorTests()
        {
            _mockLogger = new Mock<ILogger<CSharpSyntaxValidator>>();
            _validator = new CSharpSyntaxValidator(_mockLogger.Object);
        }

        [Fact]
        public async Task ValidateAsync_WithValidCSharpCode_ShouldReturnSuccess()
        {
            // Arrange
            var validCode = @"
using System;

public class TestClass
{
    public string Name { get; set; }
    
    public void DoSomething()
    {
        Console.WriteLine(""Hello World"");
    }
}";

            // Act
            var result = await _validator.ValidateAsync(validCode);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
            Assert.False(result.HasWarnings);
        }

        [Fact]
        public async Task ValidateAsync_WithSyntaxErrors_ShouldReturnFailure()
        {
            // Arrange
            var invalidCode = @"
using System;

public class TestClass
{
    public string Name { get; set; }
    
    public void DoSomething()
    {
        Console.WriteLine(""Hello World""
        // Missing closing parenthesis
    }
}";

            // Act
            var result = await _validator.ValidateAsync(invalidCode);

            // Assert
            Assert.False(result.IsValid);
            Assert.NotEmpty(result.Errors);
            Assert.Contains(result.Errors, e => e.Message.Contains("expected"));
        }

        [Fact]
        public async Task ValidateAsync_WithEmptyCode_ShouldReturnFailure()
        {
            // Arrange
            var emptyCode = "";

            // Act
            var result = await _validator.ValidateAsync(emptyCode);

            // Assert
            Assert.False(result.IsValid);
            Assert.NotEmpty(result.Errors);
            Assert.Contains(result.Errors, e => e.Message.Contains("empty"));
        }

        [Fact]
        public async Task ValidateAsync_WithWhitespaceOnly_ShouldReturnFailure()
        {
            // Arrange
            var whitespaceCode = "   \n\t   ";

            // Act
            var result = await _validator.ValidateAsync(whitespaceCode);

            // Assert
            Assert.False(result.IsValid);
            Assert.NotEmpty(result.Errors);
            Assert.Contains(result.Errors, e => e.Message.Contains("empty"));
        }

        [Fact]
        public async Task ValidateAsync_WithMissingSemicolon_ShouldReturnFailure()
        {
            // Arrange
            var codeWithMissingSemicolon = @"
using System

public class TestClass
{
    public string Name { get; set; }
}";

            // Act
            var result = await _validator.ValidateAsync(codeWithMissingSemicolon);

            // Assert
            Assert.False(result.IsValid);
            Assert.NotEmpty(result.Errors);
        }

        [Fact]
        public async Task ValidateAsync_WithMismatchedBraces_ShouldReturnFailure()
        {
            // Arrange
            var codeWithMismatchedBraces = @"
public class TestClass
{
    public void DoSomething()
    {
        if (true)
        {
            Console.WriteLine(""Hello"");
        // Missing closing brace
}";

            // Act
            var result = await _validator.ValidateAsync(codeWithMismatchedBraces);

            // Assert
            Assert.False(result.IsValid);
            Assert.NotEmpty(result.Errors);
        }

        [Fact]
        public async Task ValidateAsync_WithInvalidNamespace_ShouldReturnFailure()
        {
            // Arrange
            var codeWithInvalidNamespace = @"
using System;

namespace TestNamespace
{
    public class TestClass
    {
        public string Name { get; set; }
    }
// Missing closing brace for namespace - this will cause a syntax error
";

            // Act
            var result = await _validator.ValidateAsync(codeWithInvalidNamespace);

            // Assert
            Assert.False(result.IsValid);
            Assert.NotEmpty(result.Errors);
        }

        [Fact]
        public async Task ValidateAsync_WithValidInterface_ShouldReturnSuccess()
        {
            // Arrange
            var validInterfaceCode = @"
using System;

public interface ITestInterface
{
    string Name { get; set; }
    void DoSomething();
    Task<string> GetDataAsync();
}";

            // Act
            var result = await _validator.ValidateAsync(validInterfaceCode);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task ValidateAsync_WithValidEnum_ShouldReturnSuccess()
        {
            // Arrange
            var validEnumCode = @"
public enum TestEnum
{
    Value1,
    Value2,
    Value3
}";

            // Act
            var result = await _validator.ValidateAsync(validEnumCode);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task ValidateAsync_WithNullInput_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _validator.ValidateAsync(null!));
        }

        [Fact]
        public async Task ValidateAsync_WithComplexValidCode_ShouldReturnSuccess()
        {
            // Arrange
            var complexCode = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestNamespace
{
    public class ComplexClass
    {
        private readonly string _name;
        private readonly List<string> _items;

        public ComplexClass(string name)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _items = new List<string>();
        }

        public string Name => _name;

        public async Task<string> ProcessAsync()
        {
            await Task.Delay(100);
            return _name.ToUpper();
        }

        public void AddItem(string item)
        {
            if (string.IsNullOrEmpty(item))
                throw new ArgumentException(""Item cannot be null or empty"", nameof(item));
            
            _items.Add(item);
        }
    }
}";

            // Act
            var result = await _validator.ValidateAsync(complexCode);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }
    }
}
