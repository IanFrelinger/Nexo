using Xunit;

namespace Nexo.Tests.Standalone;

/// <summary>
/// Standalone tests that don't depend on any external projects
/// </summary>
public class StandaloneTests
{
    [Fact]
    public void Basic_Test_Should_Pass()
    {
        // This is a basic test to verify the test framework is working
        Assert.True(true);
    }

    [Fact]
    public void String_Operations_Should_Work()
    {
        var input = "Hello, World!";
        var result = input.ToUpper();
        
        Assert.Equal("HELLO, WORLD!", result);
    }

    [Fact]
    public void Math_Operations_Should_Work()
    {
        var a = 5;
        var b = 3;
        var sum = a + b;
        
        Assert.Equal(8, sum);
    }

    [Fact]
    public void Collection_Operations_Should_Work()
    {
        var numbers = new[] { 1, 2, 3, 4, 5 };
        var evenNumbers = numbers.Where(n => n % 2 == 0).ToArray();
        
        Assert.Equal(2, evenNumbers.Length);
        Assert.Contains(2, evenNumbers);
        Assert.Contains(4, evenNumbers);
    }
}
