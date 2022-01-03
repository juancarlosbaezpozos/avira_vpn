using System;
using System.Threading.Tasks;
using Avira.Messaging;

namespace Avira.VPN.Core
{
    public interface IUserManagement
    {
        string Token { get; }

        string FtuUrl { get; }

        event EventHandler<EventArgs<string>> TokenChanged;

        Task StartLogin();

        Task OpenDashboard();

        Task StartUpgrade();

        void Clear();
    }
}