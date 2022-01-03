using System;
using System.Threading;
using Serilog;

namespace Avira.VPN.Core.Win
{
    public class SharedStartEvent
    {
        private const string DefaultEventName = "Global\\432B7734-943E-4729-A5B1-F234F986A1D3";

        private readonly string sharedEventName;

        public EventWaitHandle Handle { get; private set; }

        public SharedStartEvent()
            : this("Global\\432B7734-943E-4729-A5B1-F234F986A1D3")
        {
        }

        public SharedStartEvent(string eventName)
        {
            sharedEventName = eventName;
        }

        public void Create()
        {
            Handle = new EventWaitHandle(initialState: false, EventResetMode.AutoReset, sharedEventName);
        }

        public bool Exists()
        {
            bool result = false;
            try
            {
                Handle = EventWaitHandle.OpenExisting(sharedEventName);
                result = true;
                return result;
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                return result;
            }
            catch (Exception exception)
            {
                Log.Error(exception, "Failed to open shared event " + sharedEventName + ".");
                return result;
            }
        }

        public void Signal()
        {
            try
            {
                Handle?.Set();
            }
            catch (Exception exception)
            {
                Log.Warning(exception, "Failed to signal shared event " + sharedEventName + ".");
            }
        }
    }
}