using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.Data.Interfaces
{
    public interface IRepository<T, TId> where T : class
{
    // Orchestration methods will be added here
}
}