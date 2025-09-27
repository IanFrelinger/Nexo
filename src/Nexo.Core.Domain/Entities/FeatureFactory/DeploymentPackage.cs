using System;
using Nexo.Core.Domain.Enums.FeatureFactory;

namespace Nexo.Core.Domain.Entities.FeatureFactory
{
    public partial class DeploymentPackage
    {
        public string Id { get; set; } = string.Empty;
        public PackageType PackageType { get; set; }
        public string FilePath { get; set; } = string.Empty;
    }
}
