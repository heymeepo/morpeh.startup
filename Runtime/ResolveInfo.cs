using System;

namespace Scellecs.Morpeh.Elysium
{
    internal struct ResolveInfo
    {
        public RegistrationDefinition definition;
        public bool injected;
        public Type type;
        public int order;
    }
}
