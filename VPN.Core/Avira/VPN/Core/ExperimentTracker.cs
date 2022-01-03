using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Avira.VPN.Core
{
    public class ExperimentTracker
    {
        private static readonly string LastBucketsName = "last_buckets";

        private readonly IRemoteConfiguration remoteConfiguration;

        private readonly ISettings settings;

        public ExperimentTracker()
            : this(DiContainer.Resolve<ISettings>(), DiContainer.Resolve<IRemoteConfiguration>())
        {
        }

        public ExperimentTracker(ISettings settings, IRemoteConfiguration remoteConfiguration)
        {
            this.settings = settings;
            this.remoteConfiguration = remoteConfiguration;
            Init();
        }

        private void Init()
        {
            remoteConfiguration.ConfigurationChanged += delegate { FeaturesChangedHandler(); };
            FeaturesChangedHandler();
        }

        private void FeaturesChangedHandler()
        {
            List<string> list = JsonConvert.DeserializeObject<List<string>>(settings.Get(LastBucketsName, "[]"));
            IEnumerable<string> enumerable = remoteConfiguration.Buckets.Except(list);
            TrackExperiments(enumerable, Tracker.Events.ExperimentStarted);
            IEnumerable<string> enumerable2 = list.Except(remoteConfiguration.Buckets);
            TrackExperiments(enumerable2, Tracker.Events.ExperimentStopped);
            if (enumerable.Count() > 0 || enumerable2.Count() > 0)
            {
                string value = JsonConvert.SerializeObject(remoteConfiguration.Buckets);
                settings.Set(LastBucketsName, value);
            }
        }

        private void TrackExperiments(IEnumerable<string> buckets, string eventName)
        {
            foreach (string bucket in buckets)
            {
                Dictionary<string, string> properties = new Dictionary<string, string>
                {
                    {
                        Tracker.EventProperties.Bucket,
                        bucket
                    }
                };
                Tracker.TrackEvent(eventName, properties);
            }
        }
    }
}