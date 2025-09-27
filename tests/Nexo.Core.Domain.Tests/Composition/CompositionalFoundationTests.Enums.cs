using System;
using Xunit;
using Nexo.Core.Domain.Composition;

namespace Nexo.Core.Domain.Tests.Composition
{
    /// <summary>
    /// Enum tests for compositional foundation.
    /// </summary>
    public partial class CompositionalFoundationTests
    {
        [Fact]
        public void ValidationSeverity_EnumValues_AreDefined()
        {
            // Assert
            Assert.True(Enum.IsDefined(typeof(ValidationSeverity), ValidationSeverity.Info));
            Assert.True(Enum.IsDefined(typeof(ValidationSeverity), ValidationSeverity.Warning));
            Assert.True(Enum.IsDefined(typeof(ValidationSeverity), ValidationSeverity.Error));
            Assert.True(Enum.IsDefined(typeof(ValidationSeverity), ValidationSeverity.Critical));
        }
        
        [Fact]
        public void ValidationType_EnumValues_AreDefined()
        {
            // Assert
            Assert.True(Enum.IsDefined(typeof(ValidationType), ValidationType.Required));
            Assert.True(Enum.IsDefined(typeof(ValidationType), ValidationType.Length));
            Assert.True(Enum.IsDefined(typeof(ValidationType), ValidationType.Pattern));
            Assert.True(Enum.IsDefined(typeof(ValidationType), ValidationType.Custom));
            Assert.True(Enum.IsDefined(typeof(ValidationType), ValidationType.Composite));
        }
        
        [Fact]
        public void WarningSeverity_EnumValues_AreDefined()
        {
            // Assert
            Assert.True(Enum.IsDefined(typeof(WarningSeverity), WarningSeverity.Low));
            Assert.True(Enum.IsDefined(typeof(WarningSeverity), WarningSeverity.Medium));
            Assert.True(Enum.IsDefined(typeof(WarningSeverity), WarningSeverity.High));
        }
    }
}
