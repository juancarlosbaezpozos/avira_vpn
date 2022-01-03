using System;
using System.Globalization;
using System.Threading.Tasks;
using Serilog;

namespace Avira.VPN.Core
{
    public class PersistentTimer
    {
        private int initialDelay;

        private int delay;

        private string key;

        private ISettings settings;

        private Action action;

        private DateTime lastRun;

        private bool done;

        private string PersistentKey => "Last" + key;

        public PersistentTimer(Action action, int initialDelay, int delay, string key)
            : this(action, initialDelay, delay, key, DiContainer.Resolve<ISettings>(),
                DiContainer.Resolve<IAppStateNotifier>())
        {
        }

        public PersistentTimer(Action action, int initialDelay, int delay, string key, ISettings settings,
            IAppStateNotifier appStateNotifier)
        {
            PersistentTimer persistentTimer = this;
            this.key = key;
            this.settings = settings;
            this.initialDelay = GetIntFromSettins(key + "InitialDelay", initialDelay);
            this.delay = GetIntFromSettins(key + "Delay", delay);
            this.action = action;
            lastRun = ParseStoredValue();
            if (appStateNotifier != null)
            {
                appStateNotifier.ResumingWithInternetAccess += delegate
                {
                    Log.Debug("PersistentTimer: AppIsResuming. Running action for key <" + key + ">.");
                    persistentTimer.RunAction();
                };
            }

            Run().CatchAll();
        }

        private int GetIntFromSettins(string key, int defaultValue)
        {
            if (settings == null)
            {
                return defaultValue;
            }

            if (!int.TryParse(settings.Get(key, defaultValue.ToString()), NumberStyles.Integer, null, out var result))
            {
                return defaultValue;
            }

            return result;
        }

        private DateTime ParseStoredValue()
        {
            if (settings == null)
            {
                return new DateTime(2000, 1, 1);
            }

            string text = settings.Get(PersistentKey);
            if (string.IsNullOrEmpty(text))
            {
                return new DateTime(2000, 1, 1);
            }

            try
            {
                return DateTime.Parse(text, null, DateTimeStyles.RoundtripKind);
            }
            catch (Exception)
            {
                return new DateTime(2000, 1, 1);
            }
        }

        internal async Task Run()
        {
            await Task.Delay(initialDelay);
            _ = DateTime.UtcNow;
            int millisecondsDelay = delay;
            if (DateTime.UtcNow > lastRun + TimeSpan.FromMilliseconds(delay))
            {
                RunAction();
            }
            else
            {
                millisecondsDelay =
                    (int)(lastRun + TimeSpan.FromMilliseconds(delay) - DateTime.UtcNow).TotalMilliseconds;
            }

            while (!done)
            {
                await Task.Delay(millisecondsDelay);
                millisecondsDelay = delay;
                if (!done)
                {
                    RunAction();
                }
            }
        }

        public void RunAction()
        {
            action();
            lastRun = DateTime.UtcNow;
            settings?.Set(PersistentKey, lastRun.ToString("o"));
        }

        internal void Stop()
        {
            done = true;
        }
    }
}