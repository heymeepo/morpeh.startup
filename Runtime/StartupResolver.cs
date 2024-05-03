using System;
using System.Collections.Generic;

namespace Scellecs.Morpeh.Elysium
{
    internal sealed class StartupResolver : IDisposable
    {
        private readonly IStartupContainer container;
        private Dictionary<Type, object> registrationMap;

        public StartupResolver(IStartupContainer container)
        {
            registrationMap = new Dictionary<Type, object>();
            this.container = container;
        }

        public void Register(Type type, RegistrationDefinition definition, bool injected, object instance)
        {
            if (injected && container == null)
            {
                throw new ArgumentException("You haven't passed the implementation of the DI container into the constructor arguments of the startup, but you are attempting to use injection methods.");
            }

            if (injected)
            {
                container.Register(type, definition);
            }
            else if (registrationMap.ContainsKey(type) == false)
            {
                registrationMap.Add(type, instance);
            }
        }

        public object Resolve(Type type, RegistrationDefinition definition, bool injected) => injected ? container.Resolve(type, definition) : registrationMap[type];

        public void BuildFeaturesContainer() => container?.BuildFeaturesContainer();

        public void BuildSystemsContainer() => container?.BuildSystemsContainer();

        public void Cleanup() => registrationMap = null;

        public void Dispose() => container?.Dispose();
    }
}
