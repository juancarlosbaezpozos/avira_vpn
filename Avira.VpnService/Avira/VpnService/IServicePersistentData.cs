using System;
using System.Collections.Generic;

namespace Avira.VpnService
{
    public interface IServicePersistentData
    {
        string Regions { get; set; }

        List<string> TrustedWifis { get; set; }

        KnownWifis KnownWiFis { get; set; }

        DateTime LastActivityNotification { get; set; }

        int CurrentEducationMessage { get; set; }
    }
}