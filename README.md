# Morpeh Startup

Simple startup with DI integration for [Morpeh ECS](https://github.com/scellecs/morpeh) 

## Installation

Install via git URL

```bash
https://github.com/heymeepo/morpeh.startup.git
```

## Usage

*Attention: All your update systems should be inherited from the IUpdateSystem interface instead of ISystem.*

```csharp
public class Game : MonoBehaviour
{
    private EcsStartup startup;

    private void Awake()
    {
        startup = new EcsStartup();

        startup
            .AddSystemsGroup()
            .AddInitializer(new GameInitializer())
            .AddUpdateSystem(new InputSystem())
            .AddLateSystem(new GameOverSystem());

        startup
            .AddSystemsGroup()
            .AddCleanupSystem(new DestroyEntitySystem());

        startup.Initialize(updateByUnity: true);
    }

    private void OnDestroy()
    {
        startup?.Dispose();
    }
}
```

All added systems after ```AddSystemsGroup()``` are placed in one systems group. If you need to add systems to different systems groups, you should call ```startup.AddSystemsGroup()``` again, as in the example above.

You can manually update the startup by passing 'updateByUnity: false' and calling the methods ```startup.Update()```, ```startup.FixedUpdate()```, and ```startup.LateUpdate()``` as needed.

You can also pass an instance of the ```World``` into the constructor, otherwise, ```World.Default``` will be used.

## Features
A feature is a wrapper around a set of systems responsible for some specific functionality. To create a feature, declare a new class and inherit it from the ```IEcsFeature``` interface. Inside the feature, all functionality for adding systems is available, except for ```AddSystemsGroup``` and ```AddFeature```

```csharp
public class Game : MonoBehaviour
{
    private EcsStartup startup;

    private void Awake()
    {
        startup = new EcsStartup();

        startup
            .AddSystemsGroup()
            .AddInitializer(new GameInitializer())
            .AddFeature(new AnimationFeature());

        startup.Initialize(updateByUnity: true);
    }
}

public sealed class AnimationFeature : IEcsFeature
{
    public void Configure(EcsStartup.FeatureBuilder builder)
    {
        builder
            .AddUpdateSystem(new AnimatorInitializeSystem())
            .AddUpdateSystem(new IdleAnimationSystem())
            .AddUpdateSystem(new MovementAnimationSystem())
            .AddUpdateSystem(new DieAnimationSystem())
            .AddUpdateSystem(new AnimatorSystem());
    }
}
```

## VContainer
Ensure that you have imported the [VContainer](https://github.com/hadashiA/VContainer) package and define ```VCONTAINER``` in 
*Project Settings -> Player -> Scripting Define Symbols*

Now, to create an EcsStartup, you need to pass ```VContainerResolver``` with the current ```LifetimeScope``` to its constructor. Specify all the necessary dependencies in the constructors of your systems and features.

You can use the methods with the 'Injected' postfix to add your systems using the container. However, you still have the option to add systems manually.

```csharp
public class EcsModule : IStartable, IDisposable
{
    private readonly LifetimeScope scope;
    private EcsStartup startup;

    [Inject]
    public EcsModule(LifetimeScope scope)
    {
        this.scope = scope;
    }

    public void Start()
    {
        startup = new EcsStartup(new VContainerResolver(scope));

        startup
            .AddSystemsGroup()
            .AddInitializerInjected<GameInitializer>()
            .AddUpdateSystemInjected<InputSystem>();

        startup
            .AddSystemsGroup()
            .AddFeatureInjected<AnimationFeature>()
            .AddFeatureInjected<RenderFeature>();

        startup
            .AddSystemsGroup()
            .AddCleanupSystem(new DestroyEntitySystem());

        startup.Initialize(updateByUnity: true);
    }

    public void Dispose()
    {
        startup?.Dispose();
    }
}

public sealed class AnimationFeature : IEcsFeature
{
    private readonly IGameSettingsService gameSettings;

    [Inject]
    public AnimationFeature(IGameSettingsService gameSettings)
    {
        this.gameSettings = gameSettings;
    }

    public void Configure(EcsStartup.FeatureBuilder builder)
    {
        builder
            .AddUpdateSystemInjected<AnimatorInitializeSystem>()
            .AddUpdateSystemInjected<IdleAnimationSystem>()
            .AddUpdateSystemInjected<MovementAnimationSystem>()
            .AddUpdateSystemInjected<DieAnimationSystem>();

        if (gameSettings.Graphics.EnableExperimentalAnimations)
        {
            builder.AddUpdateSystem(new ExperimentalAnimatorSystem(...));
        }
        else
        { 
            builder.AddUpdateSystemInjected<AnimatorSystem>();
        }
    }
}
```

*Clarification: You don't need to register your systems or features in the LifetimeScope, the startup does it automatically.*

## Custom DI container (Advanced)

To add support for another DI solution:

- Define the ```STARTUP_DI``` directive.
- Create a class that implements ```IStartupContainer```.
- Implement all necessary methods similar to how it's done in the ```VContainerResolver``` class.
- Pass an instance of this class to the constructor of EcsStartup.

Here's a brief explanation:

Since we support injection both into features and systems, we need to use two different containers.

Why is that?

The issue arises because, besides directly added systems and features, we also have a ```Configure``` method inside features. These methods additionally want to register their systems in the container. Hence, we cannot build the systems' container until all systems are registered. However, to invoke the ```Configure``` method of features, we need to resolve them. Therefore, features cannot reside in the same container as systems.

Due to this, we first build the container with features. Afterward, we call the Configure methods. Only then can we build the container with systems and create systems groups.

## License

[MIT](https://choosealicense.com/licenses/mit/)