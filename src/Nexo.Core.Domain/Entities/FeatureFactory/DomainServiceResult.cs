using System;
using System.Collections.Generic;
using Nexo.Core.Domain.Entities.Domain;

namespace Nexo.Core.Domain.Entities.FeatureFactory
{
    public class DomainServiceResult
    {
        public List<DomainService> Services { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }
}
