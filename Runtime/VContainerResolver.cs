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

namespace Scellecs.Morpeh.Elysium
{
    public sealed class VContainerResolver : IStartupContainer
    {
        private LifetimeScope scope;
        private LifetimeScope featuresScope;
        private LifetimeScope systemsScope;

        private Action<IContainerBuilder> registerFeatures;
        private Action<IContainerBuilder> registerSystems;

        public VContainerResolver(LifetimeScope currentScope) => scope = currentScope;

        public void BuildFeaturesContainer() => featuresScope = scope.CreateChild(builder => registerFeatures?.Invoke(builder));

        public void BuildSystemsContainer() => systemsScope = scope.CreateChild(builder => registerSystems?.Invoke(builder));

        public void Register(Type type, RegistrationDefinition definition)
        {
            if (definition == RegistrationDefinition.Feature)
            {
                registerFeatures += (builder) => builder.Register(type, Lifetime.Transient);
            }
            else
            {
                registerSystems += (builder) => builder.Register(type, Lifetime.Transient);
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
    }
}
#endif
