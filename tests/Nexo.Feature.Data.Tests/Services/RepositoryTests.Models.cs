using System;

namespace Nexo.Feature.Data.Tests.Services
{
    /// <summary>
    /// Test entity for repository tests
    /// </summary>
    public partial class TestEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
