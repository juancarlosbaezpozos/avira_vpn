using System;
using System.Collections.Generic;
using Serilog.Core;
using Serilog.Events;
using SharpRaven;
using SharpRaven.Data;

namespace Avira.VPN.Core
{
    public class SentrySerilogSink : ILogEventSink
    {
        public class UserFactory : ISentryUserFactory
        {
            private string userName;

            private string deviceId;

            public UserFactory(string userName, string deviceId)
            {
                this.userName = userName;
                this.deviceId = deviceId;
            }

            public SentryUser Create()
            {
                return new SentryUser(userName)
                {
                    Id = deviceId
                };
            }
        }

        private readonly IFormatProvider formatProvider;

        private readonly IRavenClient ravenClient;

        private readonly Dictionary<string, string> eventTags;

        private readonly ISentryUserFactory userFactory;

        private readonly Func<bool> evaluateTracking;

        public SentrySerilogSink(IFormatProvider formatProvider, string sentryUrl, string sentryUserName,
            string serverName, string productVersion, string language, bool isStoreApplication, string deviceId,
            Func<bool> evaluateTracking)
        {
            this.formatProvider = formatProvider;
            userFactory = new UserFactory(sentryUserName, deviceId);
            this.evaluateTracking = evaluateTracking;
            JsonPacketFactory jsonPacketFactory = new JsonPacketFactory
            {
                ServerName = serverName
            };
            ravenClient = new RavenClient(sentryUrl, jsonPacketFactory, null, userFactory);
            ravenClient.Release = productVersion;
            eventTags = new Dictionary<string, string>
            {
                { "version", productVersion },
                { "language", language },
                {
                    "store_application",
                    isStoreApplication.ToString()
                }
            };
        }

        public void Emit(LogEvent logEvent)
        {
            if (logEvent.Level >= LogEventLevel.Error && evaluateTracking())
            {
                SentryEvent @event = new SentryEvent(logEvent.Exception)
                {
                    Level = ((logEvent.Level == LogEventLevel.Error) ? ErrorLevel.Error : ErrorLevel.Fatal),
                    Message = logEvent.RenderMessage(formatProvider),
                    Tags = eventTags
                };
                ravenClient.Capture(@event);
            }
        }
    }
}