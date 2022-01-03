using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avira.Acp.Messages.JsonApi;

namespace Avira.Acp
{
    internal class ResourceHandlersMap : BaseResourceRepository<ResourceLocation>, IResourceHandlersMap,
        IResourceRepository<ResourceLocation>
    {
        private const string ResourceType = "resources";

        private readonly List<ResourceHandlerMapEntry> resourcesHandlers = new List<ResourceHandlerMapEntry>();

        private readonly List<SubstituteRegistration> substituteEntries = new List<SubstituteRegistration>();

        public string Add(ResourceLocation resourceLocation, RequestHandler handler, string owner)
        {
            if (!resourceLocation.IsValid())
            {
                throw new ArgumentException("Resource location is not valid");
            }

            DisableSubstitute(resourceLocation, owner);
            string text = AddInternal(resourceLocation, handler);
            if (text != null)
            {
                OnCreated(new CreatedEventArgs<ResourceLocation>(owner, resourceLocation, text, "resources"));
            }

            return text;
        }

        public string AddSubstitute(ResourceLocation resourceLocation, IResourceProvider providerSubstitute)
        {
            if (substituteEntries.Any((SubstituteRegistration s) => s.ResourceLocation.Equals(resourceLocation)))
            {
                return null;
            }

            SubstituteRegistration item = new SubstituteRegistration(resourceLocation, providerSubstitute);
            substituteEntries.Add(item);
            return EnableSubstitute(resourceLocation);
        }

        public bool RemoveSubstitute(ResourceLocation resourceLocation)
        {
            SubstituteRegistration substituteRegistration =
                substituteEntries.FirstOrDefault((SubstituteRegistration s) =>
                    s.ResourceLocation.Equals(resourceLocation));
            if (substituteRegistration == null)
            {
                return false;
            }

            substituteEntries.Remove(substituteRegistration);
            DisableSubstitute(substituteRegistration, string.Empty);
            return true;
        }

        public RequestHandler Get(ResourceLocation resourceLocation)
        {
            ResourceHandlerMapEntry resourceHandlerMapEntry;
            lock (resourcesHandlers)
            {
                resourceHandlerMapEntry = resourcesHandlers.Find((ResourceHandlerMapEntry mapEntry) =>
                    mapEntry.ResourceLocation.CheckMatch(resourceLocation));
            }

            return resourceHandlerMapEntry?.Handler;
        }

        public ICollection<ResourceLocation> GetAllResourceLocations()
        {
            lock (resourcesHandlers)
            {
                return resourcesHandlers.Select((ResourceHandlerMapEntry entry) => entry.ResourceLocation).ToList();
            }
        }

        public bool Remove(string resourceId, string owner)
        {
            ResourceHandlerMapEntry resourceHandlerMapEntry;
            lock (resourcesHandlers)
            {
                resourceHandlerMapEntry = resourcesHandlers.FirstOrDefault((ResourceHandlerMapEntry mapEntry) =>
                    mapEntry.Id.Equals(resourceId));
            }

            if (resourceHandlerMapEntry == null)
            {
                return false;
            }

            bool num = RemoveInternal(resourceHandlerMapEntry, owner);
            if (num)
            {
                EnableSubstitute(resourceHandlerMapEntry.ResourceLocation);
            }

            return num;
        }

        public bool IsResourceRegistered(ResourceLocation resourceLocation)
        {
            lock (resourcesHandlers)
            {
                return resourcesHandlers.Any((ResourceHandlerMapEntry r) =>
                    r.ResourceLocation.Equals(resourceLocation));
            }
        }

        public override List<Resource<ResourceLocation>> ReadAll(string filter)
        {
            lock (resourcesHandlers)
            {
                return resourcesHandlers.Select((ResourceHandlerMapEntry resourceHandler) =>
                    new Resource<ResourceLocation>
                    {
                        Attributes = resourceHandler.ResourceLocation,
                        Id = resourceHandler.Id.ToString(CultureInfo.InvariantCulture),
                        Type = "resources"
                    }).ToList();
            }
        }

        public override Resource<ResourceLocation> Read(string id)
        {
            ResourceHandlerMapEntry resourceHandlerMapEntry;
            lock (resourcesHandlers)
            {
                resourceHandlerMapEntry = resourcesHandlers.SingleOrDefault((ResourceHandlerMapEntry r) => r.Id == id);
            }

            if (resourceHandlerMapEntry == null)
            {
                return null;
            }

            return new Resource<ResourceLocation>
            {
                Attributes = resourceHandlerMapEntry.ResourceLocation,
                Id = resourceHandlerMapEntry.Id.ToString(CultureInfo.InvariantCulture),
                Type = "resources"
            };
        }

        private string AddInternal(ResourceLocation resourceLocation, RequestHandler handler)
        {
            lock (resourcesHandlers)
            {
                if (resourcesHandlers.Any((ResourceHandlerMapEntry mapEntry) =>
                        mapEntry.ResourceLocation.CheckMatch(resourceLocation)))
                {
                    return null;
                }

                ResourceHandlerMapEntry resourceHandlerMapEntry =
                    new ResourceHandlerMapEntry(resourceLocation, handler);
                resourcesHandlers.Add(resourceHandlerMapEntry);
                return resourceHandlerMapEntry.Id;
            }
        }

        private void DisableSubstitute(ResourceLocation resourceLocation, string owner)
        {
            SubstituteRegistration substituteRegistration =
                substituteEntries.FirstOrDefault((SubstituteRegistration s) =>
                    s.ResourceLocation.Equals(resourceLocation));
            if (substituteRegistration != null)
            {
                DisableSubstitute(substituteRegistration, owner);
            }
        }

        private void DisableSubstitute(SubstituteRegistration substituteRegistration, string owner)
        {
            if (substituteRegistration.RegistrationId != null)
            {
                RemoveInternal(substituteRegistration.ResourceLocation, owner);
                substituteRegistration.RegistrationId = null;
                StopSubstitute(substituteRegistration.ResourceProvider);
            }
        }

        private string EnableSubstitute(ResourceLocation resourceLocation)
        {
            SubstituteRegistration substituteRegistration =
                substituteEntries.FirstOrDefault((SubstituteRegistration s) =>
                    s.ResourceLocation.Equals(resourceLocation));
            if (substituteRegistration == null)
            {
                return null;
            }

            string text = AddInternal(resourceLocation, substituteRegistration.ResourceProvider.HandleMessage);
            if (text != null)
            {
                substituteRegistration.RegistrationId = text;
                StartSubstitute(substituteRegistration.ResourceProvider);
                OnCreated(new CreatedEventArgs<ResourceLocation>(resourceLocation, text, "resources"));
            }

            return text;
        }

        private void RemoveInternal(ResourceLocation resourceLocation, string owner)
        {
            ResourceHandlerMapEntry resourceHandlerMapEntry;
            lock (resourcesHandlers)
            {
                resourceHandlerMapEntry = resourcesHandlers.FirstOrDefault((ResourceHandlerMapEntry h) =>
                    h.ResourceLocation.CheckMatch(resourceLocation));
            }

            if (resourceHandlerMapEntry != null)
            {
                RemoveInternal(resourceHandlerMapEntry, owner);
            }
        }

        private bool RemoveInternal(ResourceHandlerMapEntry entry, string owner)
        {
            bool flag;
            lock (resourcesHandlers)
            {
                flag = resourcesHandlers.Remove(entry);
            }

            if (flag)
            {
                OnDeleted(new DeletedEventArgs(owner, entry.Id, "resources"));
            }

            return flag;
        }

        private void StartSubstitute(IResourceProvider substitute)
        {
            (substitute as IStartStopable)?.Start();
        }

        private void StopSubstitute(IResourceProvider substitute)
        {
            (substitute as IStartStopable)?.Stop();
        }
    }
}