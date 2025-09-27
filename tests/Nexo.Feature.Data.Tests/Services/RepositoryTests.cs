using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Feature.Data.Interfaces;
using Nexo.Feature.Data.Services;
using Xunit;

namespace Nexo.Feature.Data.Tests.Services
{
    /// <summary>
    /// Tests for repository functionality.
    /// This class acts as an orchestrator, delegating specific functionalities to partial class implementations.
    /// </summary>
    public partial class RepositoryTests
    {
        private readonly Mock<IDatabaseProvider> _mockDatabaseProvider;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly Mock<QueryBuilder> _mockQueryBuilder;
        private readonly Mock<ILogger<Repository<TestEntity, Guid>>> _mockLogger;
        private readonly Repository<TestEntity, Guid> _repository;

        public RepositoryTests()
        {
            _mockDatabaseProvider = new Mock<IDatabaseProvider>();
            _mockCacheService = new Mock<ICacheService>();
            _mockQueryBuilder = new Mock<QueryBuilder>(Mock.Of<ILogger<QueryBuilder>>());
            _mockLogger = new Mock<ILogger<Repository<TestEntity, Guid>>>();
            _repository = new Repository<TestEntity, Guid>(
                _mockDatabaseProvider.Object, 
                _mockCacheService.Object,
                _mockQueryBuilder.Object,
                _mockLogger.Object);
        }
        // This class acts as an orchestrator for various test categories,
        // with specific categories defined in partial classes.
    }
} 