using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Entities.FeatureFactory
{
    public class RepositoryResult
    {
        public List<Repository> Repositories { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }
}
