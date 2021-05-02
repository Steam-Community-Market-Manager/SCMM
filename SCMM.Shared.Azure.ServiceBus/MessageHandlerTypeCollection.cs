using System;
using System.Collections.Generic;
using System.Reflection;

namespace SCMM.Shared.Azure.ServiceBus
{
    public class MessageHandlerTypeCollection
    {
        private Assembly[] _assemblies;

        public MessageHandlerTypeCollection(params Assembly[] assemblies)
        {
            _assemblies = assemblies;
        }

        public IEnumerable<Assembly> Assemblies => _assemblies;
    }
}
