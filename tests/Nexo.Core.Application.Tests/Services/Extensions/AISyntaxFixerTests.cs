using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Nexo.Core.Application.Services.Extensions;
using Nexo.Core.Application.Interfaces.Extensions;
using Nexo.Core.Domain.Composition;

namespace Nexo.Core.Application.Tests.Services.Extensions
{
    /// <summary>
    /// Tests for the AISyntaxFixer service
    /// </summary>
    public class AISyntaxFixerTests
    {
        private readonly Mock<ILogger<AISyntaxFixer>> _mockLogger;
        private readonly Mock<ICSharpSyntaxValidator> _mockSyntaxValidator;
        private readonly AISyntaxFixer _fixer;

        public AISyntaxFixerTests()
        {
            _mockLogger = new Mock<ILogger<AISyntaxFixer>>();
            _mockSyntaxValidator = new Mock<ICSharpSyntaxValidator>();
            _fixer = new AISyntaxFixer(_mockLogger.Object, _mockSyntaxValidator.Object);
        }

        [Fact]
        public async Task FixSyntaxAsync_WithValidCode_ShouldReturnOriginalCode()
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

            var validationResult = ValidationResult.Success();
            _mockSyntaxValidator.Setup(x => x.ValidateAsync(validCode))
                .ReturnsAsync(validationResult);

            // Act
            var result = await _fixer.FixSyntaxAsync(validCode);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(validCode, result.FixedCode);
            Assert.Empty(result.FixesApplied);
        }

        [Fact]
        public async Task FixSyntaxAsync_WithSyntaxErrors_ShouldAttemptToFix()
        {
            // Arrange
            var invalidCode = @"
using System

public class TestClass
{
    public string Name { get; set; }
    
    public void DoSomething()
    {
        Console.WriteLine(""Hello World""
        // Missing closing parenthesis
    }
}";

            var validationResult = new ValidationResult();
            validationResult.AddError("; expected", "syntax", "CS1002");
            validationResult.AddError(") expected", "syntax", "CS1026");
            
            _mockSyntaxValidator.Setup(x => x.ValidateAsync(It.IsAny<string>()))
                .ReturnsAsync(validationResult);

            // Act
            var result = await _fixer.FixSyntaxAsync(invalidCode);

            // Assert
            Assert.False(result.IsSuccess); // Should fail since we can't actually fix complex syntax errors
            Assert.NotEmpty(result.FixesApplied);
            Assert.Contains(result.FixesApplied, f => f.Contains("semicolon"));
        }

        [Fact]
        public async Task FixSyntaxAsync_WithEmptyCode_ShouldReturnFailure()
        {
            // Arrange
            var emptyCode = "";
            var validationResult = ValidationResult.Failure("Code is empty");
            _mockSyntaxValidator.Setup(x => x.ValidateAsync(emptyCode))
                .ReturnsAsync(validationResult);

            // Act
            var result = await _fixer.FixSyntaxAsync(emptyCode);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Empty(result.FixedCode);
            Assert.Contains(result.FixesApplied, f => f.Contains("empty"));
        }

        [Fact]
        public async Task FixSyntaxAsync_WithWhitespaceOnly_ShouldReturnFailure()
        {
            // Arrange
            var whitespaceCode = "   \n\t   ";
            var validationResult = ValidationResult.Failure("Code is empty or contains only whitespace");
            _mockSyntaxValidator.Setup(x => x.ValidateAsync(whitespaceCode))
                .ReturnsAsync(validationResult);

            // Act
            var result = await _fixer.FixSyntaxAsync(whitespaceCode);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(whitespaceCode, result.FixedCode); // Should return original code
            Assert.Contains(result.FixesApplied, f => f.Contains("whitespace"));
        }

        [Fact]
        public async Task FixSyntaxAsync_WithMissingSemicolon_ShouldAttemptToFix()
        {
            // Arrange
            var codeWithMissingSemicolon = @"
using System

public class TestClass
{
    public string Name { get; set; }
}";

            var validationResult = new ValidationResult();
            validationResult.AddError("; expected", "syntax", "CS1002");
            
            _mockSyntaxValidator.Setup(x => x.ValidateAsync(codeWithMissingSemicolon))
                .ReturnsAsync(validationResult);
            
            // Setup for the fixed code validation
            _mockSyntaxValidator.Setup(x => x.ValidateAsync(It.Is<string>(s => s != codeWithMissingSemicolon)))
                .ReturnsAsync(ValidationResult.Success());

            // Act
            var result = await _fixer.FixSyntaxAsync(codeWithMissingSemicolon);

            // Assert
            Assert.True(result.IsSuccess); // Should succeed since we can fix missing semicolons
            Assert.NotEmpty(result.FixesApplied);
            Assert.Contains(result.FixesApplied, f => f.Contains("semicolon"));
        }

        [Fact]
        public async Task FixSyntaxAsync_WithMismatchedBraces_ShouldAttemptToFix()
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

            var validationResult = new ValidationResult();
            validationResult.AddError("} expected", "syntax", "CS1022");
            
            _mockSyntaxValidator.Setup(x => x.ValidateAsync(It.IsAny<string>()))
                .ReturnsAsync(validationResult);

            // Act
            var result = await _fixer.FixSyntaxAsync(codeWithMismatchedBraces);

            // Assert
            Assert.False(result.IsSuccess); // Should fail since we can't actually fix complex syntax errors
            Assert.NotEmpty(result.FixesApplied);
            Assert.Contains(result.FixesApplied, f => f.Contains("brace"));
        }

        [Fact]
        public async Task FixSyntaxAsync_WithNullInput_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _fixer.FixSyntaxAsync(null!));
        }

        [Fact]
        public async Task FixSyntaxAsync_WithValidationException_ShouldReturnFailure()
        {
            // Arrange
            var code = "some code";
            _mockSyntaxValidator.Setup(x => x.ValidateAsync(code))
                .ThrowsAsync(new Exception("Validation failed"));

            // Act
            var result = await _fixer.FixSyntaxAsync(code);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains(result.FixesApplied, f => f.Contains("Exception occurred"));
        }

        [Fact]
        public async Task FixSyntaxAsync_WithMultipleErrors_ShouldAttemptMultipleFixes()
        {
            // Arrange
            var codeWithMultipleErrors = @"
using System

public class TestClass
{
    public string Name { get; set; }
    
    public void DoSomething()
    {
        Console.WriteLine(""Hello World""
        // Missing semicolon and closing parenthesis
    }
}";

            var validationResult = new ValidationResult();
            validationResult.AddError("; expected", "syntax", "CS1002");
            validationResult.AddError(") expected", "syntax", "CS1026");
            
            _mockSyntaxValidator.Setup(x => x.ValidateAsync(It.IsAny<string>()))
                .ReturnsAsync(validationResult);

            // Act
            var result = await _fixer.FixSyntaxAsync(codeWithMultipleErrors);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.NotEmpty(result.FixesApplied);
            Assert.True(result.FixesApplied.Count >= 2);
        }

        [Fact]
        public async Task FixSyntaxAsync_WithUnfixableErrors_ShouldReturnFailure()
        {
            // Arrange
            var unfixableCode = @"
public class TestClass
{
    public void DoSomething()
    {
        // This is completely broken syntax
        if (true
        {
            Console.WriteLine(""Hello""
        }
    }
}";

            var validationResult = new ValidationResult();
            validationResult.AddError(") expected", "syntax", "CS1026");
            validationResult.AddError("; expected", "syntax", "CS1002");
            validationResult.AddError("} expected", "syntax", "CS1022");
            
            _mockSyntaxValidator.Setup(x => x.ValidateAsync(It.IsAny<string>()))
                .ReturnsAsync(validationResult);

            // Act
            var result = await _fixer.FixSyntaxAsync(unfixableCode);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.NotEmpty(result.FixesApplied);
            Assert.Contains(result.FixesApplied, f => f.Contains("Applied general syntax fixes"));
        }
    }
}
