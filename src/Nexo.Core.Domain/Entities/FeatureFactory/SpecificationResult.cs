using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Entities.FeatureFactory
{
    public partial class SpecificationResult
    {
        public List<Specification> Specifications { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }
}
