using System;

namespace Nexo.Shared.Factories
{
    /// <summary>
    /// Base factory interface
    /// </summary>
    public interface IFactory<T>
    {
        T Create();
        T Create(params object[] parameters);
    }
}
