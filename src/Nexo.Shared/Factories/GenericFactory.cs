using System;

namespace Nexo.Shared.Factories
{
    /// <summary>
    /// Generic factory implementation
    /// </summary>
    public class GenericFactory<T> : IFactory<T>
    {
        private readonly Func<T> _factory;

        public GenericFactory(Func<T> factory)
        {
            _factory = factory;
        }

        public T Create()
        {
            return _factory();
        }

        public T Create(params object[] parameters)
        {
            return FactoryManager.Create<T>(parameters);
        }
    }
}
