#if ENABLE_MONO || ENABLE_IL2CPP
#define MORPEH_UNITY
#endif

#if VCONTAINER && !MORPEH_UNITY
#undef VCONTAINER
#endif

#if VCONTAINER
using System;
using VContainer.Unity;
using VContainer;
using System.Collections.Generic;

namespace Scellecs.Morpeh.Elysium
{
    public sealed class VContainerResolver : IStartupContainer
    {
        private LifetimeScope scope;
        private LifetimeScope featuresScope;
        private LifetimeScope systemsScope;

        private HashSet<Type> featuresTypes;
        private HashSet<Type> systemsTypes;

        public VContainerResolver(LifetimeScope currentScope)
        {
            scope = currentScope;
            featuresTypes = new HashSet<Type>(64);
            systemsTypes = new HashSet<Type>(64);
        }

        public void BuildFeaturesContainer() => featuresScope = scope.CreateChild(builder => RegisterTypesInContainer(builder, featuresTypes));

        public void BuildSystemsContainer() => systemsScope = scope.CreateChild(builder => RegisterTypesInContainer(builder, systemsTypes));

        public void Register(Type type, RegistrationDefinition definition)
        {
            if (definition == RegistrationDefinition.Feature)
            {
                featuresTypes.Add(type);
            }
            else
            {
                systemsTypes.Add(type);
            }
        }

        public object Resolve(Type type, RegistrationDefinition definition)
        {
            if (definition == RegistrationDefinition.Feature)
            {
                return featuresScope.Container.Resolve(type);
            }
            else
            {
                return systemsScope.Container.Resolve(type);
            }
        }

        public void Dispose()
        {
            if (featuresScope != null)
            {
                featuresScope.Dispose();
            }
            if (systemsScope != null)
            {
                systemsScope.Dispose();
            }
        }

        private static void RegisterTypesInContainer(IContainerBuilder builder, HashSet<Type> types)
        { 
            foreach (var type in types) 
            {
                builder.Register(type, Lifetime.Transient);
            }
        }
    }
}
#endif
