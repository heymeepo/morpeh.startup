using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
#if VCONTAINER
using VContainer;
using VContainer.Unity;
#endif
using static Scellecs.Morpeh.Elysium.EcsStartup;

namespace Scellecs.Morpeh.Elysium
{
    public sealed class EcsStartup : IDisposable
    {
        private int currentOrder;

        private readonly Dictionary<int, SystemsGroup> systemsGroups;
#if VCONTAINER
        private readonly LifetimeScope scope;

        private LifetimeScope featuresScope;
        private LifetimeScope systemsScope;

        private Action<IContainerBuilder> registerFeatures;
        private Action<IContainerBuilder> registerSystems;
#endif
        private Action buildSetupInOrder;
        private Action setupSystemsGroups;

        private World world;

        private bool initialized;
        private bool disposed;

#if VCONTAINER
        public EcsStartup(LifetimeScope scope)
        {
            this.scope = scope;
            currentOrder = 0;
            systemsGroups = new Dictionary<int, SystemsGroup>();
            world = World.Default;
            initialized = false;
            disposed = false;
        }
#else
		public EcsStartup() 
		{ 
            currentOrder = 0;
			systemsGroups = new Dictionary<int, SystemsGroup>();
			world = World.Default;
			initialized = false;
			disposed = false;
		}
#endif
        public void Initialize(bool updateByUnity)
        {
            if (initialized)
            {
                if (disposed)
                {
                    Debug.LogError("The EcsStartup has already been disposed. Create a new one to use it.");
                }
                else
                {
                    Debug.LogWarning($"EcsStartup with {world.GetFriendlyName()} has already been initialized.");
                }

                return;
            }

            world ??= World.Create();
            world.UpdateByUnity = updateByUnity;

            RegisterFeatures();
            BuildSystemsSetupOrder();
            RegisterSystems();
            SetupSystemsGroups();
            CleanupActions();
            initialized = true;
        }

        public StartupBuilder AddSystemsGroup() => new StartupBuilder(this, currentOrder++);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update(float deltaTime)
        {
            if (world.UpdateByUnity == false)
            {
                world.Update(deltaTime);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FixedUpdate(float fixedDeltaTime)
        {
            if (world.UpdateByUnity == false)
            {
                world.FixedUpdate(fixedDeltaTime);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LateUpdate(float deltaTime)
        {
            if (world.UpdateByUnity == false)
            {
                world.LateUpdate(deltaTime);
                world.CleanupUpdate(deltaTime);
            }
        }

        public void Dispose()
        {
            if (initialized && disposed == false)
            {
                systemsGroups.Clear();
                world.Dispose();
#if VCONTAINER
                featuresScope.Dispose();
                systemsScope.Dispose();
#endif
                world = null;
                disposed = true;
            }
        }
#if VCONTAINER
        private void AddSystemInjectedDefferedSetup<T>(int order) where T : class, ISystem
        {
            buildSetupInOrder += () => AddSystemInjected<T>(order);
        }

        private void AddSystemInjected<T>(int order) where T : class, ISystem
        {
            registerSystems += (builder) => builder.Register<T>(Lifetime.Transient);

            setupSystemsGroups += () =>
            {
                var system = systemsScope.Container.Resolve<T>();
                var systemsGroup = GetOrCreateSystemsGroup(order);
                systemsGroup.AddSystem(system);
            };
        }

        private void AddInitializerInjectedDefferedSetup<T>(int order) where T : class, IInitializer
        {
            buildSetupInOrder += () => AddInitializerInjected<T>(order);
        }

        private void AddInitializerInjected<T>(int order) where T : class, IInitializer
        {
            registerSystems += (builder) => builder.Register<T>(Lifetime.Transient);

            setupSystemsGroups += () =>
            {
                var intitializer = systemsScope.Container.Resolve<T>();
                var systemsGroup = GetOrCreateSystemsGroup(order);
                systemsGroup.AddInitializer(intitializer);
            };
        }

        private void AddFeatureInjectedDefferedSetup<T>(int order) where T : class, IEcsFeature
        {
            registerFeatures += (builder) => builder.Register<T>(Lifetime.Transient);

            buildSetupInOrder += () =>
            {
                var feature = featuresScope.Container.Resolve<T>();
                feature.Configure(new FeatureBuilder(this, order));
            };
        }
#endif
        private void AddSystemDefferedSetup<T>(int order, T system) where T : class, ISystem
        {
            buildSetupInOrder += () => AddSystem(order, system);
        }

        private void AddSystem<T>(int order, T system) where T : class, ISystem
        {
            setupSystemsGroups += () =>
            {
                var systemsGroup = GetOrCreateSystemsGroup(order);
                systemsGroup.AddSystem(system);
            };
        }

        private void AddInitializerDefferedSetup<T>(int order, T initializer) where T : class, IInitializer
        {
            buildSetupInOrder += () => AddInitializer(order, initializer);
        }

        private void AddInitializer<T>(int order, T initializer) where T : class, IInitializer
        {
            setupSystemsGroups += () =>
            {
                var systemsGroup = GetOrCreateSystemsGroup(order);
                systemsGroup.AddInitializer(initializer);
            };
        }

        private void AddFeatureDefferedSetup<T>(int order, T feature) where T : class, IEcsFeature
        {
            buildSetupInOrder += () =>
            {
                feature.Configure(new FeatureBuilder(this, order));
            };
        }

        private SystemsGroup GetOrCreateSystemsGroup(int order)
        {
            if (systemsGroups.TryGetValue(order, out SystemsGroup systemsGroup) == false)
            {
                systemsGroup = systemsGroups[order] = world.CreateSystemsGroup();
            }

            return systemsGroup;
        }

        [System.Diagnostics.Conditional("VCONTAINER")]
        private void RegisterFeatures()
        {
#if VCONTAINER
            featuresScope = scope.CreateChild(builder => registerFeatures?.Invoke(builder));
#endif
        }

        private void BuildSystemsSetupOrder()
        {
            buildSetupInOrder?.Invoke();
        }

        [System.Diagnostics.Conditional("VCONTAINER")]
        private void RegisterSystems()
        {
#if VCONTAINER
            systemsScope = scope.CreateChild(builder => registerSystems?.Invoke(builder));
#endif
        }

        private void SetupSystemsGroups()
        {
            setupSystemsGroups?.Invoke();

            foreach (var group in systemsGroups)
            {
                world.AddSystemsGroup(group.Key, group.Value);
            }
        }

        private void CleanupActions()
        {
#if VCONTAINER
            registerSystems = null;
            registerFeatures = null;
#endif
            buildSetupInOrder = null;
            setupSystemsGroups = null;
        }

        public readonly struct StartupBuilder
        {
            private readonly EcsStartup ecsStartup;
            private readonly int order;

            public StartupBuilder(EcsStartup ecsStartup, int order)
            {
                this.ecsStartup = ecsStartup;
                this.order = order;
            }
#if VCONTAINER
            public StartupBuilder AddInitializerInjected<T>() where T : class, IInitializer
            {
                ecsStartup.AddInitializerInjectedDefferedSetup<T>(order);
                return this;
            }

            public StartupBuilder AddUpdateSystemInjected<T>() where T : class, IUpdateSystem
            {
                ecsStartup.AddSystemInjectedDefferedSetup<T>(order);
                return this;
            }

            public StartupBuilder AddFixedSystemInjected<T>() where T : class, IFixedSystem
            {
                ecsStartup.AddSystemInjectedDefferedSetup<T>(order);
                return this;
            }

            public StartupBuilder AddLateSystemInjected<T>() where T : class, ILateSystem
            {
                ecsStartup.AddSystemInjectedDefferedSetup<T>(order);
                return this;
            }

            public StartupBuilder AddCleanupSystemInjected<T>() where T : class, ICleanupSystem
            {
                ecsStartup.AddSystemInjectedDefferedSetup<T>(order);
                return this;
            }

            public StartupBuilder AddFeatureInjected<T>() where T : class, IEcsFeature
            {
                ecsStartup.AddFeatureInjectedDefferedSetup<T>(order);
                return this;
            }
#endif
            public StartupBuilder AddInitializer<T>(T initializer) where T : class, IInitializer
            {
                ecsStartup.AddInitializerDefferedSetup(order, initializer);
                return this;
            }

            public StartupBuilder AddUpdateSystem<T>(T system) where T : class, IUpdateSystem
            {
                ecsStartup.AddSystemDefferedSetup(order, system);
                return this;
            }

            public StartupBuilder AddFixedSystem<T>(T system) where T : class, IFixedSystem
            {
                ecsStartup.AddSystemDefferedSetup(order, system);
                return this;
            }

            public StartupBuilder AddLateSystem<T>(T system) where T : class, ILateSystem
            {
                ecsStartup.AddSystemDefferedSetup(order, system);
                return this;
            }

            public StartupBuilder AddCleanupSystem<T>(T system) where T : class, ICleanupSystem
            {
                ecsStartup.AddSystemDefferedSetup(order, system);
                return this;
            }

            public StartupBuilder AddFeature<T>(T feature) where T : class, IEcsFeature
            {
                ecsStartup.AddFeatureDefferedSetup(order, feature);
                return this;
            }
        }

        public readonly struct FeatureBuilder
        {
            private readonly EcsStartup ecsStartup;
            private readonly int order;

            public FeatureBuilder(EcsStartup ecsStartup, int order)
            {
                this.ecsStartup = ecsStartup;
                this.order = order;
            }
#if VCONTAINER
            public FeatureBuilder AddInitializerInjected<T>() where T : class, IInitializer
            {
                ecsStartup.AddInitializerInjected<T>(order);
                return this;
            }

            public FeatureBuilder AddUpdateSystemInjected<T>() where T : class, IUpdateSystem
            {
                ecsStartup.AddSystemInjected<T>(order);
                return this;
            }

            public FeatureBuilder AddFixedSystemInjected<T>() where T : class, IFixedSystem
            {
                ecsStartup.AddSystemInjected<T>(order);
                return this;
            }

            public FeatureBuilder AddLateSystemInjected<T>() where T : class, ILateSystem
            {
                ecsStartup.AddSystemInjected<T>(order);
                return this;
            }

            public FeatureBuilder AddCleanupSystemInjected<T>() where T : class, ICleanupSystem
            {
                ecsStartup.AddSystemInjected<T>(order);
                return this;
            }
#endif
            public FeatureBuilder AddInitializer<T>(T initializer) where T : class, IInitializer
            {
                ecsStartup.AddInitializer(order, initializer);
                return this;
            }

            public FeatureBuilder AddUpdateSystem<T>(T system) where T : class, IUpdateSystem
            {
                ecsStartup.AddSystem(order, system);
                return this;
            }

            public FeatureBuilder AddFixedSystem<T>(T system) where T : class, IFixedSystem
            {
                ecsStartup.AddSystem(order, system);
                return this;
            }

            public FeatureBuilder AddLateSystem<T>(T system) where T : class, ILateSystem
            {
                ecsStartup.AddSystem(order, system);
                return this;
            }

            public FeatureBuilder AddCleanupSystem<T>(T system) where T : class, ICleanupSystem
            {
                ecsStartup.AddSystem(order, system);
                return this;
            }
        }
    }

    public interface IEcsFeature
    {
        public void Configure(FeatureBuilder builder);
    }

    public interface IUpdateSystem : ISystem { }
}
