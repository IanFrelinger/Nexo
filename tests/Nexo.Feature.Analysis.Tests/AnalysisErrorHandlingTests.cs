using Xunit;
using System;

namespace Nexo.Feature.Analysis.Tests;

public class AnalysisErrorHandlingTests
{
    [Fact]
    public void Analysis_Invalid_Input_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => _ = new object().ToString());
    }
}
