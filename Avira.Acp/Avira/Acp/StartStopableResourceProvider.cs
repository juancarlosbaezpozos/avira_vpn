using System;

namespace Avira.Acp
{
    public class StartStopableResourceProvider<T> : ResourceProvider<T>, IStartStopable where T : class
    {
        private readonly IStartStopable startStopable;

        public StartStopableResourceProvider(IResourceRepository<T> repository, ResourceLocation resourceLocation,
            IAcpMessageBroker messageBroker)
            : base(repository, resourceLocation, messageBroker)
        {
            startStopable = repository as IStartStopable;
            if (startStopable == null)
            {
                throw new ArgumentException("Repository must be of type IStartStopable");
            }
        }

        public void Start()
        {
            startStopable.Start();
        }

        public void Stop()
        {
            startStopable.Stop();
        }
    }
}