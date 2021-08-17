using System.Reflection;

namespace SCMM.Azure.ServiceBus
{
    public class MessageHandlerTypeCollection
    {
        private readonly Assembly[] _assemblies;

        public MessageHandlerTypeCollection(params Assembly[] assemblies)
        {
            _assemblies = assemblies;
        }

        public IEnumerable<Assembly> Assemblies => _assemblies;
    }
}
