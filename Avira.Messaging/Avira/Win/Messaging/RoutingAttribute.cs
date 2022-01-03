using System;

namespace Avira.Win.Messaging
{
    public class RoutingAttribute : Attribute
    {
        public string Route { get; }

        public bool AutoPublishOnSubscribe { get; set; }

        public RoutingAttribute(string route)
        {
            Route = route;
        }

        public RoutingAttribute(string route, bool autoPublishOnSubscribe)
        {
            Route = route;
            AutoPublishOnSubscribe = autoPublishOnSubscribe;
        }
    }
}