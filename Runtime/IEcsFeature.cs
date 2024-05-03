namespace Scellecs.Morpeh.Elysium
{
    public interface IEcsFeature
    {
        public void Configure(EcsStartup.FeatureBuilder builder);
    }
}
