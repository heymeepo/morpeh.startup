#if ENABLE_MONO || ENABLE_IL2CPP
#define MORPEH_UNITY
#endif

#if VCONTAINER && !MORPEH_UNITY
#undef VCONTAINER
#endif

#if VCONTAINER
using VContainer;
using VContainer.Unity;
#endif

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using static Scellecs.Morpeh.Elysium.EcsStartup;

namespace Scellecs.Morpeh.Elysium
{
    public sealed class EcsStartup : IDisposable
    {
        public World World { get; private set; }

#if VCONTAINER && MORPEH_UNITY
        private LifetimeScope scope;
        private LifetimeScope featuresScope;
        private LifetimeScope systemsScope;

        private Action<IContainerBuilder> registerFeatures;
        private Action<IContainerBuilder> registerSystems;
#endif
        private Action buildSetupInOrder;
        private Action setupSystemsGroups;

        private int currentOrder;
        private Dictionary<int, SystemsGroup> systemsGroups;

        private bool initialized;
        private bool disposed;

#if VCONTAINER
        public EcsStartup(LifetimeScope scope, World world = null)
        {
            this.scope = scope;
            currentOrder = 0;
            systemsGroups = new Dictionary<int, SystemsGroup>();
            World = world.IsNullOrDisposed() ? World.Default : world;
            initialized = false;
            disposed = false;
        }
#else
        public EcsStartup(World world = null)
        {
            currentOrder = 0;
            systemsGroups = new Dictionary<int, SystemsGroup>();
            World = world.IsNullOrDisposed() ? World.Default : world;
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
                    LogWarning("The EcsStartup has already been disposed. Create a new one to use it.");
                }
                else
                {
                    LogWarning($"EcsStartup with {World.GetFriendlyName()} has already been initialized.");
                }

                return;
            }

            if (World.IsNullOrDisposed())
            {
                World = World.Create();
            }

            World.UpdateByUnity = updateByUnity;

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
            if (World.UpdateByUnity == false)
            {
                World.Update(deltaTime);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FixedUpdate(float fixedDeltaTime)
        {
            if (World.UpdateByUnity == false)
            {
                World.FixedUpdate(fixedDeltaTime);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LateUpdate(float deltaTime)
        {
            if (World.UpdateByUnity == false)
            {
                World.LateUpdate(deltaTime);
                World.CleanupUpdate(deltaTime);
            }
        }

        public void Dispose()
        {
            if (initialized && disposed == false)
            {
                systemsGroups.Clear();
                World.Dispose();
#if VCONTAINER
                featuresScope.Dispose();
                systemsScope.Dispose();
#endif
                World = null;
                disposed = true;
            }
        }

        private static void LogWarning(string message)
        {
#if MORPEH_UNITY
            UnityEngine.Debug.LogWarning(message);
#else
            Console.WriteLine(message);
#endif
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
                systemsGroup = systemsGroups[order] = World.CreateSystemsGroup();
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
                World.AddSystemsGroup(group.Key, group.Value);
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
