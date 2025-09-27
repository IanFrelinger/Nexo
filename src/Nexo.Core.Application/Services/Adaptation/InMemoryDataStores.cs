using Nexo.Core.Domain.Interfaces.Infrastructure;
using Nexo.Core.Domain.Entities.Infrastructure;
using Nexo.Core.Application.Services.Environment;
using Nexo.Core.Application.Services.Learning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.Adaptation
{
    /// <summary>
    /// In-memory data stores for adaptation, performance, feedback, and environment data.
    /// This class is split into partial files for better organization:
    /// - InMemoryDataStores.Adaptation.cs - In-memory adaptation data management
    /// - InMemoryDataStores.Performance.cs - In-memory performance data management
    /// - InMemoryDataStores.Feedback.cs - In-memory feedback data management
    /// - InMemoryDataStores.Environment.cs - In-memory environment data management
    /// - InMemoryDataStores.Models.cs - Data models and summaries
    /// </summary>
    public partial class InMemoryDataStores
    {
        // This class is split into partial files for better organization
        // All functionality has been moved to the respective partial files listed above
    }

    /// <summary>
    /// In-memory implementation of adaptation data store
    /// </summary>
    public partial class InMemoryAdaptationDataStore : IAdaptationDataStore
    {
        // Implementation moved to InMemoryDataStores.Adaptation.cs
    }

    /// <summary>
    /// In-memory implementation of performance data store
    /// </summary>
    public partial class InMemoryPerformanceDataStore : IPerformanceDataStore
    {
        // Implementation moved to InMemoryDataStores.Performance.cs
    }

    /// <summary>
    /// In-memory implementation of feedback store
    /// </summary>
    public partial class InMemoryFeedbackStore : IFeedbackStore
    {
        // Implementation moved to InMemoryDataStores.Feedback.cs
    }

    /// <summary>
    /// In-memory implementation of environment data store
    /// </summary>
    public partial class InMemoryEnvironmentDataStore : IEnvironmentDataStore
    {
        // Implementation moved to InMemoryDataStores.Environment.cs
    }
}
