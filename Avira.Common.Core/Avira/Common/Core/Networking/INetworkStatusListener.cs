using System;

namespace Avira.Common.Core.Networking
{
    public interface INetworkStatusListener
    {
        event EventHandler StatusChanged;
    }
}