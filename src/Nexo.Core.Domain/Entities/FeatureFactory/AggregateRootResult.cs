using System;
using System.Collections.Generic;
using Nexo.Core.Domain.Entities.Domain;

namespace Nexo.Core.Domain.Entities.FeatureFactory
{
    public partial class AggregateRootResult
    {
        public List<AggregateRoot> AggregateRoots { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }
}
