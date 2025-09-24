using System;
using System.Collections.Generic;
using Nexo.Core.Domain.Interfaces.Infrastructure;
using Nexo.Core.Domain.Entities.Infrastructure.DataStore;

namespace Nexo.Core.Domain.Entities.Infrastructure;

/// <summary>
/// Orchestrator for data store types that delegates to specialized data store type classes.
/// </summary>
public static class DataStoreTypes
{
    // This class now serves as a namespace organizer and factory for data store types
    // All specific data store types have been moved to focused classes in the DataStore folder
}
