using System;
using System.IO;

namespace Nexo.Infrastructure.Tests.Compilation
{
    /// <summary>
    /// Utility methods for Roslyn compilation tests
    /// </summary>
    public partial class RoslynCompilationIdempotentTests
    {
        #region Helper Methods

        private string GenerateLargeCode(int methodCount)
        {
            var code = @"
                using System;
                public class LargeClass
                {
            ";

            for (int i = 0; i < methodCount; i++)
            {
                code += $@"
                    public string GetMethod{i}() => ""Method {i}"";
                ";
            }

            code += @"
                }
            ";

            return code;
        }

        #endregion

        #region Cleanup

        public void Dispose()
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }

        #endregion
    }
}
