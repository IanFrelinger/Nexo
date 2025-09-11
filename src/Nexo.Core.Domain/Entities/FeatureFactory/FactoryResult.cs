using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Entities.FeatureFactory
{
    public class FactoryResult
    {
        public List<Factory> Factories { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }
}
