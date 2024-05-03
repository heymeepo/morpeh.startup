#if ENABLE_MONO || ENABLE_IL2CPP
#define MORPEH_UNITY
#endif

#if VCONTAINER && !MORPEH_UNITY
#undef VCONTAINER
#endif

#if VCONTAINER
#endif

using static Scellecs.Morpeh.Elysium.EcsStartup;

namespace Scellecs.Morpeh.Elysium
{
    public interface IEcsFeature
    {
        public void Configure(FeatureBuilder builder);
    }
}
