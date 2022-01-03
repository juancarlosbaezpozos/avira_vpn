using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Avira.VPN.Core
{
    [DiContainer.Export(typeof(IOeStatusReporter))]
    public sealed class OeStatusReporter : IOeStatusReporter, IDisposable
    {
        private Task oeReportTask;

        private CancellationTokenSource cts = new CancellationTokenSource();

        private IOeApi OeApi;

        public OeStatusReporter()
            : this(DiContainer.Resolve<IOeApi>())
        {
        }

        public OeStatusReporter(IOeApi OeApi)
        {
            this.OeApi = OeApi;
        }

        public void Start(TimeSpan initialDelay, TimeSpan repeatInterval)
        {
            oeReportTask = ReportOnInterval(initialDelay, repeatInterval).Catch<TaskCanceledException>(delegate { });
        }

        private async Task ReportOnInterval(TimeSpan initialDelay, TimeSpan repeatInterval)
        {
            await Task.Delay(initialDelay, cts.Token);
            if (repeatInterval.TotalMilliseconds == 0.0)
            {
                repeatInterval = TimeSpan.FromDays(1.0);
            }

            while (!cts.IsCancellationRequested)
            {
                TimeSpan delay = OeReportSettings.LastOeReport.Add(repeatInterval) - DateTime.Now;
                if (delay.Ticks < 0)
                {
                    delay = TimeSpan.Zero;
                }

                await Task.Delay(delay, cts.Token);
                if (cts.IsCancellationRequested)
                {
                    break;
                }

                await SendStatus().Catch(delegate(Exception e) { Log.Warning(e, "Failed to send OE status."); });
                OeReportSettings.LastOeReport = DateTime.Now;
            }
        }

        public async Task SendStatus()
        {
            if (!DiContainer.Resolve<IAppSettings>().Get().AppImprovement)
            {
                return;
            }

            if ((DiContainer.Resolve<IAuthenticator>()?.AccessToken ?? string.Empty) != string.Empty)
            {
                IOeStatusProvider oeStatusProvider;
                IOeStatusProvider provider = (oeStatusProvider = DiContainer.Resolve<IOeStatusProvider>());
                if (oeStatusProvider != null)
                {
                    Log.Debug("Sending Connect event Heartbeat. CustomData: " +
                              provider.HeartbeatCustomData.ToString());
                    await OeApi.SendHeartbeat(provider.HeartbeatCustomData);
                    if (provider.WasConnectFeatureUsed())
                    {
                        Log.Debug("Sending Connect event FeatureUsed : Vpn. CustomData: " +
                                  provider.EventsCustomData.ToString());
                        await OeApi.SendFeatureUsed("Vpn", provider.EventsCustomData);
                        Tracker.TrackEvent(Tracker.Events.AARRR_FeatureUsed);
                    }

                    if (provider.WasAppOpened())
                    {
                        Log.Debug("Sending Connect event AppOpen : Main. CustomData: " +
                                  provider.EventsCustomData.ToString());
                        await OeApi.SendAppOpen("Main", provider.EventsCustomData);
                        Tracker.TrackEvent(Tracker.Events.AARRR_AppOpen);
                    }
                }
            }
            else
            {
                Log.Debug("OeStatusReporter SendStatus not sent - IAuthenticator accessToken not available.");
            }
        }

        private static AppInstance FindAppInstance(JArray instances, long deviceId)
        {
            return new AppInstance((long)((JObject)instances
                .Where((JToken i) => (long)i.SelectToken("relationships.device.data.id") == deviceId)
                .DefaultIfEmpty<JToken>(instances.First()).First())["id"]);
        }

        public void Dispose()
        {
            if (cts != null)
            {
                cts.Cancel();
                oeReportTask.Wait();
                cts.Dispose();
                cts = null;
            }
        }
    }
}