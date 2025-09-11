using System;
using System.Collections.Generic;
using Nexo.Core.Domain.Entities.Domain;

namespace Nexo.Core.Domain.Entities.FeatureFactory
{
    public class ValueObjectResult
    {
        public List<ValueObject> ValueObjects { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }
}
