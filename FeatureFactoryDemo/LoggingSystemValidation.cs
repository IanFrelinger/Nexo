using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace FeatureFactoryDemo
{
    /// <summary>
    /// Comprehensive logging system validation for dependency injection wrapped logging.
    /// This class is split into partial files for better organization:
    /// - LoggingSystemValidation.Core.cs - Main validation orchestration
    /// - LoggingSystemValidation.DependencyInjection.cs - DI and type safety tests
    /// - LoggingSystemValidation.Logging.cs - Log levels, structured logging, exception logging
    /// - LoggingSystemValidation.Performance.cs - Performance, concurrency, memory tests
    /// - LoggingSystemValidation.TestServices.cs - Test service implementations
    /// - LoggingSystemValidation.TestInfrastructure.cs - Test logger provider and models
    /// </summary>
    public partial class LoggingSystemValidation : IDisposable
    {
        // This class is split into partial files for better organization
        // All functionality has been moved to the respective partial files listed above
    }
}
