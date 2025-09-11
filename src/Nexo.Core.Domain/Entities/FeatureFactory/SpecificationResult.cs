using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Entities.FeatureFactory
{
    public class SpecificationResult
    {
        public List<Specification> Specifications { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }
}
