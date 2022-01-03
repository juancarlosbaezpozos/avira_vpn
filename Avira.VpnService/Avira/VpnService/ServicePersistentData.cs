using System;
using System.Collections.Generic;
using Avira.VPN.Core.Win;

namespace Avira.VpnService
{
    internal class ServicePersistentData : IServicePersistentData
    {
        private readonly IStorage storage;

        public string Regions
        {
            get { return storage.Get("CachedRegions"); }
            set { storage.Set("CachedRegions", value); }
        }

        public DateTime LastActivityNotification
        {
            get { return GenericAccessor.Get(storage, "LastActivityNotification", default(DateTime)); }
            set { GenericAccessor.Set(storage, "LastActivityNotification", value); }
        }

        public List<string> TrustedWifis
        {
            get { return GenericAccessor.Get<List<string>>(storage, "TrustedWifis"); }
            set { GenericAccessor.Set(storage, "TrustedWifis", value); }
        }

        public KnownWifis KnownWiFis
        {
            get { return GenericAccessor.Get<KnownWifis>(storage, "KnownWiFis"); }
            set { GenericAccessor.Set(storage, "KnownWiFis", value); }
        }

        public int CurrentEducationMessage
        {
            get { return GenericAccessor.Get<int>(storage, "CurrentEducationMessage", 0); }
            set { GenericAccessor.Set<int>(storage, "CurrentEducationMessage", value); }
        }

        public ServicePersistentData()
            : this(ProductSettings.SecureStorage)
        {
        }

        internal ServicePersistentData(IStorage xmlStorage)
        {
            storage = xmlStorage;
        }
    }
}