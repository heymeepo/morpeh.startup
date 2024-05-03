using System;

namespace Scellecs.Morpeh.Elysium
{
    public interface IStartupContainer : IDisposable
    {
        public void BuildFeaturesContainer();
        public void BuildSystemsContainer();
        public void Register(Type type, RegistrationDefinition definition);
        public object Resolve(Type type, RegistrationDefinition definition);
    }
}