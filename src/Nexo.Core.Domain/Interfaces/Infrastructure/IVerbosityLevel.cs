using System;

namespace Nexo.Core.Domain.Entities.Infrastructure;

/// <summary>
/// Verbosity levels for logging and output
/// </summary>
public enum VerbosityLevel
{
    /// <summary>
    /// Silent - no output
    /// </summary>
    Silent = 0,
    
    /// <summary>
    /// Minimal - only essential output
    /// </summary>
    Minimal = 1,
    
    /// <summary>
    /// Normal - standard output level
    /// </summary>
    Normal = 2,
    
    /// <summary>
    /// Detailed - additional details
    /// </summary>
    Detailed = 3,
    
    /// <summary>
    /// Diagnostic - diagnostic information
    /// </summary>
    Diagnostic = 4,
    
    /// <summary>
    /// Verbose - maximum verbosity
    /// </summary>
    Verbose = 5
}